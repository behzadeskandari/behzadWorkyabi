using System.IO;
using IranJob.Modules.Candidates.Application.Contracts;

namespace IranJob.Modules.Candidates.Application.Abstractions;

/// <summary>
/// Manages a candidate's structured resume (summary, work experience,
/// education, skills, languages, projects, certifications, portfolio
/// links) and their CV file upload. All operations are scoped to the
/// authenticated candidate derived from <paramref name="userId"/>.
/// </summary>
public interface ICandidateResumeService
{
    /// <summary>
    /// Returns the candidate's resume or throws <see cref="NotFoundException"/>
    /// when no resume has been created yet.
    /// </summary>
    Task<ResumeResult> GetResumeAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the resume (if it does not exist) or updates it in full,
    /// replacing the section collections. Ownership is always the
    /// authenticated user; the request carries no user identifier.
    /// </summary>
    Task<ResumeResult> UpsertResumeAsync(
        Guid userId,
        SaveResumeRequest request,
        IEnumerable<SkillReference> skillReferences,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads (or replaces) the candidate's CV document.
    /// Validates extension, content-type, and file size.
    /// </summary>
    Task<ResumeResult> UploadCvAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the candidate's CV file and clears the stored metadata.
    /// </summary>
    Task<ResumeResult> DeleteCvAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the candidate's own CV file. Returns the content,
    /// content-type and download name — or throws <see cref="NotFoundException"/>
    /// when no CV file is present.
    /// </summary>
    Task<CvDownload> DownloadCvAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the resume status (Draft, Complete, ReadyToShare).
    /// </summary>
    Task<ResumeResult> SetStatusAsync(Guid userId, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the entire resume (and associated sections) for the candidate.
    /// If a CV file is present it is removed from disk as well.
    /// </summary>
    Task DeleteResumeAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a persisted-data-based summary of the candidate's resume
    /// completeness (score, sharing status, CV presence and which sections
    /// are missing) for the candidate dashboard / application-readiness guidance.
    /// Returns null when the candidate has not created a resume yet.
    /// </summary>
    Task<ResumeCompletionResponse?> GetCompletionStatusAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// A validated reference to a ReferenceData skill.
/// </summary>
public sealed record SkillReference(Guid Id, string Name);

/// <summary>
/// Returned when a candidate downloads their own CV file.
/// </summary>
public sealed record CvDownload(Stream Content, string ContentType, string DownloadName);