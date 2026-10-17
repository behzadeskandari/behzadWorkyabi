using System;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.Candidates.Domain.Entities;

/// <summary>
/// A portfolio project entry on the candidate's resume.
/// </summary>
public sealed class ResumeProject : Entity
{
    public Guid ResumeId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Url { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsCurrent { get; private set; }
    public int SortOrder { get; private set; }

    private ResumeProject() { }

    public static ResumeProject Create(
        Guid resumeId, string title, string? description, string? url,
        DateOnly? startDate, DateOnly? endDate, bool isCurrent, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Project title is required.", nameof(title));

        return new ResumeProject
        {
            Id = Guid.NewGuid(),
            ResumeId = resumeId,
            Title = title.Trim(),
            Description = NormalizeOptional(description),
            Url = NormalizeOptional(url),
            StartDate = startDate,
            EndDate = endDate,
            IsCurrent = isCurrent,
            SortOrder = sortOrder
        };
    }

    public void Update(string title, string? description, string? url,
        DateOnly? startDate, DateOnly? endDate, bool isCurrent)
    {
        Title = title.Trim();
        Description = NormalizeOptional(description);
        Url = NormalizeOptional(url);
        StartDate = startDate;
        EndDate = endDate;
        IsCurrent = isCurrent;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}