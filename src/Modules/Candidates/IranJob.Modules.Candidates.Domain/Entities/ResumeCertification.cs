using System;
using IranJob.SharedKernel.Entities;

namespace IranJob.Modules.Candidates.Domain.Entities;

/// <summary>
/// A certification entry on the candidate's resume.
/// </summary>
public sealed class ResumeCertification : Entity
{
    public Guid ResumeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? IssuingOrganization { get; private set; }
    public DateOnly? IssueDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public string? CertificateUrl { get; private set; }
    public int SortOrder { get; private set; }

    private ResumeCertification() { }

    public static ResumeCertification Create(
        Guid resumeId, string name, string? issuingOrganization,
        DateOnly? issueDate, DateOnly? expiryDate, string? certificateUrl, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Certification name is required.", nameof(name));

        return new ResumeCertification
        {
            Id = Guid.NewGuid(),
            ResumeId = resumeId,
            Name = name.Trim(),
            IssuingOrganization = NormalizeOptional(issuingOrganization),
            IssueDate = issueDate,
            ExpiryDate = expiryDate,
            CertificateUrl = NormalizeOptional(certificateUrl),
            SortOrder = sortOrder
        };
    }

    public void Update(string name, string? issuingOrganization,
        DateOnly? issueDate, DateOnly? expiryDate, string? certificateUrl)
    {
        Name = name.Trim();
        IssuingOrganization = NormalizeOptional(issuingOrganization);
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        CertificateUrl = NormalizeOptional(certificateUrl);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}