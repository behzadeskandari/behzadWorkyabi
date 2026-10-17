using System;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.Candidates.Domain.Entities;

/// <summary>
/// A single work experience entry belonging to a <see cref="CandidateResume"/>.
/// </summary>
public sealed class ResumeWorkExperience : Entity
{
    public Guid ResumeId { get; private set; }
    public string Company { get; private set; } = string.Empty;
    public string JobTitle { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsCurrent { get; private set; }
    public int SortOrder { get; private set; }

    private ResumeWorkExperience() { }

    public static ResumeWorkExperience Create(
        Guid resumeId, string company, string jobTitle, string? description,
        DateOnly? startDate, DateOnly? endDate, bool isCurrent, int sortOrder)
    {
        return new ResumeWorkExperience
        {
            Id = Guid.NewGuid(),
            ResumeId = resumeId,
            Company = company.Trim(),
            JobTitle = jobTitle.Trim(),
            Description = NormalizeOptional(description),
            StartDate = startDate,
            EndDate = endDate,
            IsCurrent = isCurrent,
            SortOrder = sortOrder
        };
    }

    public void Update(string company, string jobTitle, string? description,
        DateOnly? startDate, DateOnly? endDate, bool isCurrent)
    {
        Company = company.Trim();
        JobTitle = jobTitle.Trim();
        Description = NormalizeOptional(description);
        StartDate = startDate;
        EndDate = endDate;
        IsCurrent = isCurrent;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}