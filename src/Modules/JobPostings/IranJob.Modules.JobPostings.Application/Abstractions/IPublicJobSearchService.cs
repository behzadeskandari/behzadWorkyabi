using IranJob.Modules.JobPostings.Application.Contracts;

namespace IranJob.Modules.JobPostings.Application.Abstractions;

public interface IPublicJobSearchService
{
    Task<PublicJobSearchPage> SearchAsync(PublicJobSearchQuery query, CancellationToken cancellationToken);
    Task<PublicJobDetails> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken);
}
