using IranJob.Modules.Identity.Application.Abstractions;
using IranJob.Modules.Identity.Domain.Entities;
using IranJob.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.Identity.Infrastructure.Services;

public sealed class UserLookupService(IdentityDbContext dbContext) : IUserLookupService
{
    public async Task<ApplicationUser?> FindByIdentifierAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        var trimmed = identifier.Trim();
        if (trimmed.Contains('@', StringComparison.Ordinal))
        {
            var normalizedEmail = trimmed.ToUpperInvariant();
            return await dbContext.Users
                .FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
        }

        var normalizedPhone = NormalizePhoneNumber(trimmed);
        return await dbContext.Users
            .FirstOrDefaultAsync(user => user.PhoneNumber == normalizedPhone, cancellationToken);
    }

    public async Task<bool> PhoneNumberExistsAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        return await dbContext.Users.AnyAsync(user => user.PhoneNumber == normalizedPhone, cancellationToken);
    }

    private static string NormalizePhoneNumber(string phoneNumber) =>
        phoneNumber.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
}
