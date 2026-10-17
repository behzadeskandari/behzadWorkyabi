using System;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.Candidates.Domain.Entities;

/// <summary>
/// A single education entry belonging to a <see cref="CandidateResume"/>.
/// </summary>
public sealed class ResumeEducation : Entity
{
    public Guid ResumeId { get; private set; }
    public string Institution { get; private set; } = string.Empty;
    public string Degree { get; private set; } = string.Empty;
    public string? FieldOfStudy { get; private set; }
    public string? Grade { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsCurrent { get; private set; }
    public int SortOrder { get; private set; }

    private ResumeEducation() { }

    public static ResumeEducation Create(
        Guid resumeId, string institution, string degree, string? fieldOfStudy,
        string? grade, DateOnly? startDate, DateOnly? endDate, bool isCurrent, int sortOrder)
    {
        return new ResumeEducation
        {
            Id = Guid.NewGuid(),
            ResumeId = resumeId,
            Institution = institution.Trim(),
            Degree = degree.Trim(),
            FieldOfStudy = NormalizeOptional(fieldOfStudy),
            Grade = NormalizeOptional(grade),
            StartDate = startDate,
            EndDate = endDate,
            IsCurrent = isCurrent,
            SortOrder = sortOrder
        };
    }

    public void Update(string institution, string degree, string? fieldOfStudy,
        string? grade, DateOnly? startDate, DateOnly? endDate, bool isCurrent)
    {
        Institution = institution.Trim();
        Degree = degree.Trim();
        FieldOfStudy = NormalizeOptional(fieldOfStudy);
        Grade = NormalizeOptional(grade);
        StartDate = startDate;
        EndDate = endDate;
        IsCurrent = isCurrent;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}