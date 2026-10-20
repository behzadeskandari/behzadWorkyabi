namespace IranJob.SharedKernel.Abstractions;

/// <summary>
/// Resolves a user's email address from their user id. Lives in the shared
/// kernel so that any module (e.g. JobPostings' event handlers) can resolve a
/// recipient's email without taking a direct dependency on the Identity module,
/// which owns the user/email store. Implementations are registered by the
/// Identity module.
/// </summary>
public interface IUserEmailResolver
{
    /// <summary>
    /// Returns the email address for <paramref name="userId"/>, or
    /// <see langword="null"/> when the user does not exist or has no email.
    /// </summary>
    Task<string?> ResolveEmailAsync(Guid userId, CancellationToken cancellationToken = default);
}
