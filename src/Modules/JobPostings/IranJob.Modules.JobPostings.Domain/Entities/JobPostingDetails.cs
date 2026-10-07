using IranJob.Modules.JobPostings.Domain.Enums;

namespace IranJob.Modules.JobPostings.Domain.Entities;

public sealed record JobPostingDetails(
    string Title,
    string Description,
    Guid CategoryId,
    IReadOnlyCollection<Guid> SkillIds,
    EmploymentType EmploymentType,
    WorkArrangement WorkArrangement,
    Guid? CityId,
    decimal? SalaryMinimum,
    decimal? SalaryMaximum,
    string? SalaryCurrency,
    SalaryPeriod? SalaryPeriod,
    DateTimeOffset? ClosingAt);
