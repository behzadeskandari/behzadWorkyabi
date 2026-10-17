namespace IranJob.Modules.Identity.Application.Abstractions;

/// <summary>
/// Provides administrative dashboard data and user management operations
/// for Admin and SuperAdmin roles. All methods are protected by
/// policy-based authorization at the controller level.
/// </summary>
public interface IAdminDashboardService
{
    Task<SystemStatsResult> GetSystemStatsAsync(CancellationToken cancellationToken = default);

    Task<UserListResult> GetUsersAsync(int page, int pageSize, string? search = null, string? role = null, bool? isActive = null, CancellationToken cancellationToken = default);

    Task<UserDetailResult> GetUserByIdAsync(Guid userId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task SetUserActiveStatusAsync(Guid targetUserId, bool isActive, Guid actorUserId, CancellationToken cancellationToken = default);

    Task SetUserLockoutAsync(Guid targetUserId, bool lockOut, Guid actorUserId, CancellationToken cancellationToken = default);

    Task UpdateUserRolesAsync(Guid targetUserId, IReadOnlyList<string> roles, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<PasswordResetInitiationResult> InitiatePasswordResetAsync(Guid targetUserId, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record SystemStatsResult(
    int TotalUsers,
    int ActiveUsers,
    int LockedOutUsers,
    int RecentRegistrations,
    IReadOnlyDictionary<string, int> UsersByRole,
    DateTimeOffset GeneratedAt);

public sealed record AdminUserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset? LockoutEnd,
    IReadOnlyList<string> Roles);

public sealed record UserListResult(
    IReadOnlyList<AdminUserDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>
/// Detailed view of a user returned by <see cref="IAdminDashboardService.GetUserByIdAsync"/>.
/// </summary>
public sealed record UserDetailResult(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset? LockoutEnd,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Roles,
    CandidateProfileSummary? CandidateProfile,
    EmployerProfileSummary? EmployerProfile);

/// <summary>
/// Lightweight candidate profile summary linked to an Identity user.
/// </summary>
public sealed record CandidateProfileSummary(
    Guid UserId,
    string? Headline,
    string? City,
    string? Province,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

/// <summary>
/// Lightweight employer profile summary linked to an Identity user.
/// </summary>
public sealed record EmployerProfileSummary(
    Guid UserId,
    string CompanyName,
    string? CompanyDescription,
    string Industry,
    string CompanySize,
    string City,
    string Province,
    string? WebsiteUrl,
    int? FoundedYear,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

/// <summary>
/// Result of an admin-initiated password-reset attempt.
/// <see cref="Token"/> is always <c>null</c> — the raw token is never
/// exposed through the administrative API.
/// </summary>
public sealed record PasswordResetInitiationResult(
    Guid UserId,
    string Email,
    bool Initiated,
    string? ErrorMessage,
    string? Token);

/// <summary>
/// Request body for toggling a user's active status.
/// </summary>
public sealed record SetUserActiveStatusRequest(bool IsActive);

/// <summary>
/// Request body for locking or unlocking a user.
/// </summary>
public sealed record SetUserLockoutRequest(bool LockOut);

/// <summary>
/// Request body for replacing a user's roles.
/// </summary>
public sealed record UpdateUserRolesRequest(IReadOnlyList<string> Roles);
