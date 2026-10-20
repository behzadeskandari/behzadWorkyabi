using IranJob.Modules.Identity.Infrastructure.Persistence;
using IranJob.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Resolves a user's email address from their user id using the Identity user
/// store. Implements the shared-kernel <see cref="IUserEmailResolver"/> so other
/// modules (e.g. JobPostings' event handlers) can send email without a direct
/// dependency on this module.
/// </summary>
public sealed class UserEmailResolver(IdentityDbContext dbContext) : IUserEmailResolver
{
    public async Task<string?> ResolveEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return null;

        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.Email)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
