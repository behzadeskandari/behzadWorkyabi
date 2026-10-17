using System;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.Candidates.Domain.Entities;

/// <summary>
/// A portfolio link entry on the candidate's resume.
/// </summary>
public sealed class ResumePortfolioLink : Entity
{
    public Guid ResumeId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }

    private ResumePortfolioLink() { }

    public static ResumePortfolioLink Create(
        Guid resumeId, string title, string url, string? description, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url is required.", nameof(url));

        return new ResumePortfolioLink
        {
            Id = Guid.NewGuid(),
            ResumeId = resumeId,
            Title = title.Trim(),
            Url = url.Trim(),
            Description = NormalizeOptional(description),
            SortOrder = sortOrder
        };
    }

    public void Update(string title, string url, string? description)
    {
        Title = title.Trim();
        Url = url.Trim();
        Description = NormalizeOptional(description);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}