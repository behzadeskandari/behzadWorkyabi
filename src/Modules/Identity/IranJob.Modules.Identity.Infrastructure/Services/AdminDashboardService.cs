using IranJob.Modules.Identity.Application.Abstractions;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.Identity.Domain.Entities;
using IranJob.Modules.Identity.Infrastructure.Persistence;
using IranJob.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IAdminDashboardService"/> that queries
/// the Identity database for real statistics and user listings.
/// No business logic or filesystem operations -- pure data projection.
/// </summary>
public sealed class AdminDashboardService(
    UserManager<ApplicationUser> userManager,
    IdentityDbContext dbContext) : IAdminDashboardService
{
    private const int RecentRegistrationDays = 7;

    public async Task<SystemStatsResult> GetSystemStatsAsync(CancellationToken cancellationToken = default)
    {
        var recentThreshold = DateTimeOffset.UtcNow.AddDays(-RecentRegistrationDays);
        var users = userManager.Users.AsNoTracking();
        var totalUsers = await users.CountAsync(cancellationToken);
        var activeUsers = await users.CountAsync(user => user.IsActive, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        int lockedOutUsers;
        int recentRegistrations;
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            // SQLite cannot translate DateTimeOffset comparisons for this model.
            var dateValues = await users.Select(user => new { user.LockoutEnd, user.CreatedAt }).ToListAsync(cancellationToken);
            lockedOutUsers = dateValues.Count(user => user.LockoutEnd.HasValue && user.LockoutEnd > now);
            recentRegistrations = dateValues.Count(user => user.CreatedAt >= recentThreshold);
        }
        else
        {
            lockedOutUsers = await users.CountAsync(user => user.LockoutEnd.HasValue && user.LockoutEnd > now, cancellationToken);
            recentRegistrations = await users.CountAsync(user => user.CreatedAt >= recentThreshold, cancellationToken);
        }

        var roleCounts = await (from userRole in dbContext.UserRoles.AsNoTracking()
                                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                                group userRole by role.Name into roleGroup
                                select new { Role = roleGroup.Key, Count = roleGroup.Select(x => x.UserId).Distinct().Count() })
            .ToDictionaryAsync(item => item.Role!, item => item.Count, cancellationToken);
        var usersByRole = IdentityRoles.All.ToDictionary(role => role, role => roleCounts.GetValueOrDefault(role));

        return new SystemStatsResult(
            TotalUsers: totalUsers,
            ActiveUsers: activeUsers,
            LockedOutUsers: lockedOutUsers,
            RecentRegistrations: recentRegistrations,
            UsersByRole: usersByRole,
            GeneratedAt: DateTimeOffset.UtcNow);
    }

    public async Task<UserDetailResult> GetUserByIdAsync(
        Guid userId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            throw new NotFoundException($"User with ID '{userId}' was not found.");

        var roles = await userManager.GetRolesAsync(user);

        return new UserDetailResult(
            Id: user.Id,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Email: user.Email ?? string.Empty,
            PhoneNumber: user.PhoneNumber,
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            LockoutEnd: user.LockoutEnd,
            UpdatedAt: user.UpdatedAt,
            Roles: roles.ToList(),
            CandidateProfile: null,
            EmployerProfile: null);
    }

    public async Task<UserListResult> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        return await GetUsersAsync(page, pageSize, cancellationToken: cancellationToken);
    }

    public async Task<UserListResult> GetUsersAsync(
        int page,
        int pageSize,
        string? search = null,
        string? role = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<ApplicationUser> query = userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(searchTerm)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(searchTerm)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleEntity = await dbContext.Roles.AsNoTracking()
                .Where(item => item.NormalizedName == role.ToUpperInvariant())
                .Select(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (roleEntity == Guid.Empty)
                return new UserListResult([], 0, page, pageSize);
            query = query.Where(user => dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RoleId == roleEntity));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var offset = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
        List<ApplicationUser> pagedUsers;
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            // SQLite cannot order DateTimeOffset values in this model.
            pagedUsers = (await query.ToListAsync(cancellationToken))
                .OrderByDescending(user => user.CreatedAt)
                .Skip(offset).Take(pageSize).ToList();
        }
        else
        {
            pagedUsers = await query.OrderByDescending(user => user.CreatedAt).ThenBy(user => user.Id)
                .Skip(offset).Take(pageSize).ToListAsync(cancellationToken);
        }

        var pageIds = pagedUsers.Select(user => user.Id).ToArray();
        var rolesByUser = await (from userRole in dbContext.UserRoles.AsNoTracking()
                                 join roleEntity in dbContext.Roles.AsNoTracking() on userRole.RoleId equals roleEntity.Id
                                 where pageIds.Contains(userRole.UserId)
                                 select new { userRole.UserId, Role = roleEntity.Name! })
            .ToListAsync(cancellationToken);
        var rolesLookup = rolesByUser.GroupBy(item => item.UserId).ToDictionary(group => group.Key, group => group.Select(item => item.Role).ToList());
        var items = pagedUsers.Select(user => new AdminUserDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email ?? string.Empty,
                user.PhoneNumber,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                user.LockoutEnd,
                rolesLookup.GetValueOrDefault(user.Id, []))).ToList();

        return new UserListResult(items, totalCount, page, pageSize);
    }

        public async Task<PasswordResetInitiationResult> InitiatePasswordResetAsync(
     Guid targetUserId,
     Guid actorUserId,
     CancellationToken cancellationToken = default)
    {
        // Prevent self password reset
        if (targetUserId == actorUserId)
            throw new ForbiddenException("You cannot reset your own password.");

        var user = await userManager.FindByIdAsync(targetUserId.ToString());

        if (user is null)
        {
            return new PasswordResetInitiationResult(
                UserId: targetUserId,
                Email: string.Empty,
                Initiated: false,
                ErrorMessage: $"User with ID '{targetUserId}' was not found.",
                Token: null);
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return new PasswordResetInitiationResult(
                UserId: user.Id,
                Email: string.Empty,
                Initiated: false,
                ErrorMessage: "User does not have an email address.",
                Token: null);
        }

        // Generate password reset token (in a real system this would be sent
        // via email/SMS — the token is never exposed through the API).
        _ = await userManager.GeneratePasswordResetTokenAsync(user);

        return new PasswordResetInitiationResult(
            UserId: user.Id,
            Email: user.Email,
            Initiated: false,
            ErrorMessage: null,
            Token: null);
    }

        public async Task SetUserActiveStatusAsync(
        Guid targetUserId,
        bool isActive,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(targetUserId.ToString());
        if (user is null)
            throw new NotFoundException($"User with ID '{targetUserId}' was not found.");

        // Prevent self-deactivation
        if (targetUserId == actorUserId && !isActive)
            throw new ForbiddenException("You cannot deactivate your own account.");

        // Admin (not SuperAdmin) cannot modify Admin or SuperAdmin accounts
        var actor = await userManager.FindByIdAsync(actorUserId.ToString());
        if (actor is not null)
        {
            var actorRoles = await userManager.GetRolesAsync(actor);
            if (!actorRoles.Contains(IdentityRoles.SuperAdmin))
            {
                var targetRoles = await userManager.GetRolesAsync(user);
                if (targetRoles.Contains(IdentityRoles.Admin) || targetRoles.Contains(IdentityRoles.SuperAdmin))
                    throw new ForbiddenException("Admin role cannot modify Admin or SuperAdmin accounts.");
            }
        }

        user.IsActive = isActive;
        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user status: {errors}");
        }
    }

        public async Task SetUserLockoutAsync(
        Guid targetUserId,
        bool lockOut,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(targetUserId.ToString());
        if (user is null)
            throw new NotFoundException($"User with ID '{targetUserId}' was not found.");

        // Prevent self-lockout
        if (targetUserId == actorUserId && lockOut)
            throw new ForbiddenException("You cannot lock out your own account.");

        // Admin (not SuperAdmin) cannot lock or unlock Admin or SuperAdmin accounts
        var actor = await userManager.FindByIdAsync(actorUserId.ToString());
        if (actor is not null)
        {
            var actorRoles = await userManager.GetRolesAsync(actor);
            if (!actorRoles.Contains(IdentityRoles.SuperAdmin))
            {
                var targetRoles = await userManager.GetRolesAsync(user);
                if (targetRoles.Contains(IdentityRoles.Admin) || targetRoles.Contains(IdentityRoles.SuperAdmin))
                    throw new ForbiddenException("Admin role cannot lock or unlock Admin or SuperAdmin accounts.");
            }
        }

        if (lockOut)
        {
            // Lock for a long period (e.g. 100 years) or until manually unlocked
            var lockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            var result = await userManager.SetLockoutEndDateAsync(user, lockoutEnd);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to lock user: {errors}");
            }
        }
        else
        {
                        // Unlock
            var result = await userManager.SetLockoutEndDateAsync(user, null);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to unlock user: {errors}");
            }
        }
    }

    public async Task UpdateUserRolesAsync(
        Guid targetUserId,
        IReadOnlyList<string> roles,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(targetUserId.ToString());
        if (user is null)
            throw new NotFoundException($"User with ID '{targetUserId}' was not found.");

        // Validate roles
        var invalidRoles = roles.Where(r => !IdentityRoles.All.Contains(r)).ToList();
        if (invalidRoles.Count > 0)
            throw new ValidationException($"Invalid roles: {string.Join(", ", invalidRoles)}");

        // Check for duplicate roles
        var duplicateRoles = roles.GroupBy(r => r).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateRoles.Count > 0)
            throw new ValidationException($"Duplicate roles: {string.Join(", ", duplicateRoles)}");

        // Determine actor's roles
        var actor = await userManager.FindByIdAsync(actorUserId.ToString());
        var actorRoles = actor is not null ? await userManager.GetRolesAsync(actor) : [];
        var isActorSuperAdmin = actorRoles.Contains(IdentityRoles.SuperAdmin);

        // SuperAdmin cannot change their own roles
        if (targetUserId == actorUserId && isActorSuperAdmin)
            throw new ForbiddenException("SuperAdmin cannot change their own roles.");

        // Admin (not SuperAdmin) cannot assign Admin or SuperAdmin roles
        if (!isActorSuperAdmin)
        {
            if (roles.Contains(IdentityRoles.Admin) || roles.Contains(IdentityRoles.SuperAdmin))
                throw new ForbiddenException("Admin role cannot assign Admin or SuperAdmin roles.");

            // Admin cannot modify another Admin or SuperAdmin
            var targetRoles = await userManager.GetRolesAsync(user);
            if (targetRoles.Contains(IdentityRoles.Admin) || targetRoles.Contains(IdentityRoles.SuperAdmin))
                throw new ForbiddenException("Admin role cannot modify other Admin or SuperAdmin accounts.");
        }

        // Remove existing roles
        var existingRoles = await userManager.GetRolesAsync(user);
        if (existingRoles.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, existingRoles);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to remove existing roles: {errors}");
            }
        }

        // Add new roles
        if (roles.Count > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, roles);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to add new roles: {errors}");
            }
        }
    }
}
