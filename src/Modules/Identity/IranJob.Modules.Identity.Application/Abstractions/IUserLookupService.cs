using IranJob.Modules.Identity.Domain.Entities;

namespace IranJob.Modules.Identity.Application.Abstractions;

public interface IUserLookupService
{
    Task<ApplicationUser?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken cancellationToken = default);
}
