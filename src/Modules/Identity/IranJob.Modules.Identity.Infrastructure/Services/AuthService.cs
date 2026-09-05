using FluentValidation;
using Microsoft.AspNetCore.Identity;
using IranJob.Modules.Identity.Application.Abstractions;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.Identity.Domain.Entities;
using IranJob.SharedKernel.Exceptions;
using DomainValidationException = IranJob.SharedKernel.Exceptions.ValidationException;

namespace IranJob.Modules.Identity.Infrastructure.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService,
    IAuthenticationAuditService auditService,
    IUserLookupService userLookupService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IRequestContext requestContext) : IAuthService
{
    private const string SafeAuthErrorMessage = "Invalid credentials.";

    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
        }

        if (!IdentityRoles.PublicRegistrationRoles.Contains(request.Role))
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                [nameof(RegisterRequest.Role)] = ["Invalid registration role."]
            });
        }

        var normalizedEmail = userManager.NormalizeEmail(request.Email);
        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);

        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            throw new DomainException("A user with this email already exists.");
        }

        if (await userLookupService.PhoneNumberExistsAsync(normalizedPhone, cancellationToken))
        {
            throw new DomainException("A user with this phone number already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = request.Email,
            NormalizedEmail = normalizedEmail,
            PhoneNumber = normalizedPhone,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        IdentityResult createResult;
        try
        {
            createResult = await userManager.CreateAsync(user, request.Password);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            throw new DomainException("A user with this email or phone number already exists.");
        }

        if (!createResult.Succeeded)
        {
            throw new DomainValidationException(MapIdentityErrors(createResult.Errors));
        }

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw new DomainValidationException(MapIdentityErrors(roleResult.Errors));
        }

        await auditService.RecordAsync(
            AuthenticationAuditEventTypes.RegistrationSucceeded,
            user.Id,
            metadata: $"role={request.Role}",
            cancellationToken);
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
        }

        var user = await userLookupService.FindByIdentifierAsync(request.Identifier, cancellationToken);
        if (user is null || !user.IsActive)
        {
            await auditService.RecordAsync(
                AuthenticationAuditEventTypes.LoginFailed,
                metadata: "reason=invalid_credentials",
                cancellationToken: cancellationToken);
            throw new UnauthorizedException(SafeAuthErrorMessage);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            await auditService.RecordAsync(
                AuthenticationAuditEventTypes.AccountLocked,
                user.Id,
                cancellationToken: cancellationToken);
            throw new UnauthorizedException(SafeAuthErrorMessage);
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            if (signInResult.IsLockedOut)
            {
                await auditService.RecordAsync(
                    AuthenticationAuditEventTypes.AccountLocked,
                    user.Id,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await auditService.RecordAsync(
                    AuthenticationAuditEventTypes.LoginFailed,
                    user.Id,
                    cancellationToken: cancellationToken);
            }

            throw new UnauthorizedException(SafeAuthErrorMessage);
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var accessToken = jwtTokenService.GenerateAccessToken(
            user.Id,
            user.UserName ?? user.Email ?? user.Id.ToString(),
            roles,
            out var expiresAt);
        var refresh = await refreshTokenService.CreateAsync(user.Id, requestContext.IpAddress, cancellationToken);

        await auditService.RecordAsync(
            AuthenticationAuditEventTypes.LoginSucceeded,
            user.Id,
            cancellationToken: cancellationToken);

        return new LoginResult(
            accessToken,
            expiresAt,
            MapUserProfile(user, roles),
            refresh.RawToken);
    }

    public async Task<RefreshResult> RefreshAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            await auditService.RecordAsync(
                AuthenticationAuditEventTypes.RefreshFailed,
                metadata: "reason=missing_token",
                cancellationToken: cancellationToken);
            throw new UnauthorizedException(SafeAuthErrorMessage);
        }

        var rotation = await refreshTokenService.RotateAsync(refreshToken, requestContext.IpAddress, cancellationToken);
        if (rotation is null)
        {
            await auditService.RecordAsync(
                AuthenticationAuditEventTypes.RefreshFailed,
                metadata: "reason=invalid_token",
                cancellationToken: cancellationToken);
            throw new UnauthorizedException(SafeAuthErrorMessage);
        }

        var user = await userManager.FindByIdAsync(rotation.Value.Token.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException(SafeAuthErrorMessage);
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var accessToken = jwtTokenService.GenerateAccessToken(
            user.Id,
            user.UserName ?? user.Email ?? user.Id.ToString(),
            roles,
            out var expiresAt);

        await auditService.RecordAsync(
            AuthenticationAuditEventTypes.RefreshSucceeded,
            user.Id,
            cancellationToken: cancellationToken);

        return new RefreshResult(
            accessToken,
            expiresAt,
            MapUserProfile(user, roles),
            rotation.Value.RawToken);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var revoked = await refreshTokenService.RevokeAsync(
                refreshToken,
                requestContext.IpAddress,
                "Logout",
                cancellationToken);

            if (revoked)
            {
                await auditService.RecordAsync(
                    AuthenticationAuditEventTypes.Logout,
                    cancellationToken: cancellationToken);
            }
        }
    }

    public async Task<UserProfileResult> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new UnauthorizedException();

        if (!user.IsActive)
        {
            throw new UnauthorizedException();
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        return MapUserProfile(user, roles);
    }

    private static UserProfileResult MapUserProfile(ApplicationUser user, IReadOnlyList<string> roles) =>
        new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email ?? string.Empty,
            user.PhoneNumber ?? string.Empty,
            roles);

    private static string NormalizePhoneNumber(string phoneNumber) =>
        phoneNumber.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);

    private static Dictionary<string, string[]> MapIdentityErrors(IEnumerable<IdentityError> errors) =>
        errors.GroupBy(error => error.Code)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());
}
