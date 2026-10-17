using IranJob.Modules.Identity.Application.Abstractions;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.Identity.Domain.Entities;
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
    UserManager<ApplicationUser> userManager) : IAdminDashboardService
{
    private const int RecentRegistrationDays = 7;

    public async Task<SystemStatsResult> GetSystemStatsAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users.AsNoTracking().ToListAsync(cancellationToken);

        var totalUsers = users.Count;
        var activeUsers = users.Count(user => user.IsActive);
        var lockedOutUsers = users.Count(user => user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow);

        var recentThreshold = DateTimeOffset.UtcNow.AddDays(-RecentRegistrationDays);
        var recentRegistrations = users.Count(user => user.CreatedAt >= recentThreshold);

        var usersByRole = new Dictionary<string, int>();
        foreach (var roleName in IdentityRoles.All)
        {
            var roleUsers = await userManager.GetUsersInRoleAsync(roleName);
            usersByRole[roleName] = roleUsers?.Count ?? 0;
        }

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

        var totalCount = await userManager.Users.AsNoTracking()
            .CountAsync(cancellationToken);

        // SQLite stores DateTimeOffset as TEXT and cannot translate ORDER BY on it.
        // We load all users and sort client-side so the same code path works
        // with SQL Server (production) and SQLite (tests).
        var users = userManager.Users.AsNoTracking()
            .AsEnumerable()
            .OrderByDescending(user => user.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = new List<AdminUserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            items.Add(new AdminUserDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email ?? string.Empty,
                user.PhoneNumber,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                user.LockoutEnd,
                roles.ToList()));
        }

        return new UserListResult(items, totalCount, page, pageSize);
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

        // Role filtering requires materialization because GetUsersInRoleAsync is not IQueryable
        List<ApplicationUser> filteredUsers;

        if (!string.IsNullOrWhiteSpace(role))
        {
            var usersInRole = await userManager.GetUsersInRoleAsync(role);
            var roleUserIds = usersInRole.Select(u => u.Id).ToHashSet();

            filteredUsers = await query
                .Where(u => roleUserIds.Contains(u.Id))
                .ToListAsync(cancellationToken);
        }
        else
        {
            filteredUsers = await query.ToListAsync(cancellationToken);
        }

        var totalCount = filteredUsers.Count;

        var pagedUsers = filteredUsers
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = new List<AdminUserDto>(pagedUsers.Count);
        foreach (var user in pagedUsers)
        {
            var roles = await userManager.GetRolesAsync(user);
            items.Add(new AdminUserDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email ?? string.Empty,
                user.PhoneNumber,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                user.LockoutEnd,
                roles.ToList()));
        }

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