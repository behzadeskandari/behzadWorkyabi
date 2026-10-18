using System.IO;
using IranJob.Modules.Candidates.Domain.Entities;
using IranJob.Modules.Candidates.Domain.Enums;
using IranJob.Modules.Candidates.Infrastructure.Persistence;
using IranJob.Modules.JobPostings.Application.Abstractions;
using IranJob.Modules.JobPostings.Application.Contracts;
using IranJob.Modules.JobPostings.Domain.Entities;
using IranJob.Modules.JobPostings.Domain.Events;
using IranJob.Modules.JobPostings.Domain.Enums;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.BuildingBlocks.Infrastructure.Events;
using IranJob.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
// EmployerProfile is referenced only for its UserId (the job owner).
// The entity type is mapped in this DbContext via ConfigureExternalEntities
// (schema "employers", ExcludeFromMigrations) — kept as a read-only projection source.
using EmployerProfileEntity = IranJob.Modules.EmployerProfile.Domain.Entities.EmployerProfile;


namespace IranJob.Modules.JobPostings.Infrastructure.Services;

public sealed class JobApplicationService(
    JobPostingDbContext jobDbContext,
    CandidateDbContext candidateDbContext,
    IEventDispatcher eventDispatcher,
    IOptions<FileStorageOptions> fileStorageOptions) : IJobApplicationService
{
    public async Task<JobApplicationResponse> ApplyAsync(Guid userId, Guid jobId, CancellationToken cancellationToken)
    {
        var candidateProfileId = await GetCandidateProfileIdAsync(userId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var job = await jobDbContext.JobPostings.AsNoTracking()
            .Where(posting => posting.Id == jobId && posting.Status == JobPostingStatus.Published)
            .Select(posting => new { posting.Id, posting.ClosingAt })
            .SingleOrDefaultAsync(cancellationToken);
        if (job is null || job.ClosingAt.HasValue && job.ClosingAt.Value <= now)
            throw new NotFoundException("An open published job posting was not found.");

        if (await jobDbContext.JobApplications.AsNoTracking().AnyAsync(application =>
                application.CandidateProfileId == candidateProfileId && application.JobPostingId == jobId,
                cancellationToken))
            throw AlreadyApplied();

        var application = JobApplication.Create(candidateProfileId, jobId);
        jobDbContext.JobApplications.Add(application);
        try
        {
            await jobDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent request may have passed the earlier existence check. The unique index
            // is authoritative; re-query so that its loser receives the same conflict response.
            if (await jobDbContext.JobApplications.AsNoTracking().AnyAsync(existing =>
                    existing.CandidateProfileId == candidateProfileId && existing.JobPostingId == jobId,
                    cancellationToken))
                throw AlreadyApplied();

            throw;
        }

        // Dispatch the application-created event only after the row is committed.
        // Recipients (employer) and titles are resolved by the subscribed handler.
        await eventDispatcher.DispatchAsync(
            JobApplicationStatusChanged.Create(application, JobApplicationTransition.Applied),
            cancellationToken);

        return Map(await ProjectOwn(candidateProfileId, application.Id).SingleAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<JobApplicationResponse>> GetOwnAsync(Guid userId, CancellationToken cancellationToken)
    {
        var candidateProfileId = await GetCandidateProfileIdAsync(userId, cancellationToken);
        var sqlite = jobDbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
        var rows = await ProjectOwn(candidateProfileId, orderInDatabase: !sqlite).ToListAsync(cancellationToken);
        if (sqlite)
            rows = rows.OrderByDescending(application => application.AppliedAt).ToList();
        return rows.Select(Map).ToArray();
    }

    public async Task<JobApplicationResponse> GetOwnByIdAsync(
        Guid userId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var candidateProfileId = await GetCandidateProfileIdAsync(userId, cancellationToken);
        var application = await ProjectOwn(candidateProfileId, applicationId).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Application was not found.");
        return Map(application);
    }

    public async Task<JobApplicationResponse> WithdrawAsync(
        Guid userId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var candidateProfileId = await GetCandidateProfileIdAsync(userId, cancellationToken);
        var application = await jobDbContext.JobApplications.SingleOrDefaultAsync(item =>
                item.Id == applicationId && item.CandidateProfileId == candidateProfileId,
                cancellationToken)
            ?? throw new NotFoundException("Application was not found.");

        application.Withdraw();
        await jobDbContext.SaveChangesAsync(cancellationToken);

        await eventDispatcher.DispatchAsync(
            JobApplicationStatusChanged.Create(application, JobApplicationTransition.Withdrawn),
            cancellationToken);

        return Map(await ProjectOwn(candidateProfileId, applicationId).SingleAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<EmployerApplicationListResponse>> GetEmployerApplicationsAsync(
        Guid employerUserId,
        CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(employerUserId, cancellationToken);
        var sqlite = jobDbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
        var rows = await ProjectEmployer(employerProfileId, orderInDatabase: !sqlite)
            .ToListAsync(cancellationToken);
        if (sqlite)
            rows = rows.OrderByDescending(row => row.AppliedAt).ToList();

        return await MapEmployerAsync(rows, cancellationToken);
    }

    public async Task<EmployerApplicationResponse> ReviewApplicationAsync(
        Guid employerUserId,
        Guid applicationId,
        CancellationToken cancellationToken) =>
        await UpdateEmployerApplicationStatusAsync(employerUserId, applicationId, application => application.Review(),
            JobApplicationTransition.Reviewed,
            cancellationToken);

    public async Task<EmployerApplicationResponse> AcceptApplicationAsync(
        Guid employerUserId,
        Guid applicationId,
        CancellationToken cancellationToken) =>
        await UpdateEmployerApplicationStatusAsync(employerUserId, applicationId, application => application.Accept(),
            JobApplicationTransition.Accepted,
            cancellationToken);

    public async Task<EmployerApplicationResponse> RejectApplicationAsync(
        Guid employerUserId,
        Guid applicationId,
        CancellationToken cancellationToken) =>
        await UpdateEmployerApplicationStatusAsync(employerUserId, applicationId, application => application.Reject(),
            JobApplicationTransition.Rejected,
            cancellationToken);

    private async Task<EmployerApplicationResponse> UpdateEmployerApplicationStatusAsync(
        Guid employerUserId,
        Guid applicationId,
        Action<JobApplication> applyTransition,
        JobApplicationTransition transition,
        CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(employerUserId, cancellationToken);
        var row = await (
            from application in jobDbContext.JobApplications
            join job in jobDbContext.JobPostings.AsNoTracking() on application.JobPostingId equals job.Id
            where application.Id == applicationId && job.EmployerProfileId == employerProfileId
            select new { application, job })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Application was not found.");

        applyTransition(row.application);
        await jobDbContext.SaveChangesAsync(cancellationToken);

        // Dispatch the domain event only after the row is committed, matching
        // the pattern used by ApplyAsync / WithdrawAsync.
        await eventDispatcher.DispatchAsync(
            JobApplicationStatusChanged.Create(row.application, transition),
            cancellationToken);

        var candidateProfile = await candidateDbContext.CandidateProfiles.AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.Id == row.application.CandidateProfileId, cancellationToken);

        return MapEmployerDetail(row.application, row.job, candidateProfile);
    }

    public async Task<EmployerApplicationResponse> GetEmployerApplicationByIdAsync(
        Guid employerUserId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(employerUserId, cancellationToken);
        var row = await (
            from application in jobDbContext.JobApplications.AsNoTracking()
            join job in jobDbContext.JobPostings.AsNoTracking() on application.JobPostingId equals job.Id
            where application.Id == applicationId && job.EmployerProfileId == employerProfileId
            select new { application, job })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Application was not found.");

        var candidateProfile = await candidateDbContext.CandidateProfiles.AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.Id == row.application.CandidateProfileId, cancellationToken);

        return MapEmployerDetail(row.application, row.job, candidateProfile);
    }

    public async Task<EmployerCvAccessResponse> DownloadCandidateCvAsync(
        Guid employerUserId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(employerUserId, cancellationToken);
        var application = await (
            from app in jobDbContext.JobApplications.AsNoTracking()
            join job in jobDbContext.JobPostings.AsNoTracking() on app.JobPostingId equals job.Id
            where app.Id == applicationId && job.EmployerProfileId == employerProfileId
            select app)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Application was not found.");
        var candidateProfile = await candidateDbContext.CandidateProfiles.AsNoTracking()
            .Where(profile => profile.Id == application.CandidateProfileId)
            .Select(profile => new { profile.UserId, profile.Headline, profile.City })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Candidate profile was not found.");
        var resume = await candidateDbContext.CandidateResumes.AsNoTracking()
            .SingleOrDefaultAsync(r => r.UserId == candidateProfile.UserId, cancellationToken)
            ?? throw new NotFoundException("Candidate resume was not found.");
        if (resume.Status != ResumeStatus.ReadyToShare)
            throw new NotFoundException("Candidate resume is not shared with employers.");
        if (string.IsNullOrEmpty(resume.CvFileName) || string.IsNullOrEmpty(resume.CvFileContentType))
            throw new NotFoundException("CV file was not found.");
        var opts = fileStorageOptions.Value;
        var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), opts.CvStoragePath, resume.CvFileName);
        if (!File.Exists(physicalPath))
            throw new NotFoundException("CV file was not found.");
        var contentStream = new MemoryStream();
        using (var fs = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
        {
            await fs.CopyToAsync(contentStream, 4096, cancellationToken);
        }
        contentStream.Position = 0;
        return new EmployerCvAccessResponse(
            applicationId,
            candidateProfile.UserId,
            candidateProfile.Headline,
            contentStream,
            resume.CvFileContentType,
            GetDownloadFileName(resume.CvFileContentType));
    }

    public async Task<EmployerResumeViewResponse> GetCandidateResumeAsync(
        Guid employerUserId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(employerUserId, cancellationToken);
        var application = await (
            from app in jobDbContext.JobApplications.AsNoTracking()
            join job in jobDbContext.JobPostings.AsNoTracking() on app.JobPostingId equals job.Id
            where app.Id == applicationId && job.EmployerProfileId == employerProfileId
            select app)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Application was not found.");
        var candidateProfile = await candidateDbContext.CandidateProfiles.AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.Id == application.CandidateProfileId, cancellationToken);
        var resume = await candidateDbContext.CandidateResumes.AsNoTracking()
            .Include(r => r.WorkExperiences)
            .Include(r => r.Educations)
            .Include(r => r.Skills)
            .Include(r => r.Languages)
            .Include(r => r.Projects)
            .Include(r => r.Certifications)
            .Include(r => r.PortfolioLinks)
            .SingleOrDefaultAsync(r => r.UserId == candidateProfile.UserId, cancellationToken)
            ?? throw new NotFoundException("Candidate resume was not found.");
        if (resume.Status != ResumeStatus.ReadyToShare)
            throw new NotFoundException("Candidate resume is not shared with employers.");
        return MapEmployerResumeView(application, candidateProfile, resume);
    }

    private static EmployerResumeViewResponse MapEmployerResumeView(
        JobApplication application,
        CandidateProfile? candidateProfile,
        CandidateResume resume) => new(
        application.Id,
        application.CandidateProfileId,
        candidateProfile?.Headline,
        resume.ProfessionalSummary,
        resume.Status.ToString(),
        resume.CalculateCompleteness(),
        resume.CvFileName is not null && resume.CvFileContentType is not null,
        resume.WorkExperiences.OrderBy(e => e.SortOrder)
            .Select(e => new EmployerResumeSectionView(
                e.Id,
                e.Company,
                e.JobTitle,
                e.Description,
                e.StartDate,
                e.EndDate,
                e.IsCurrent,
                DurationMonths(e.StartDate, e.EndDate, e.IsCurrent),
                e.SortOrder)).ToArray(),
        resume.Educations.OrderBy(e => e.SortOrder)
            .Select(e => new EmployerResumeSectionView(
                e.Id,
                e.Institution,
                e.Degree,
                e.FieldOfStudy,
                e.StartDate,
                e.EndDate,
                e.IsCurrent,
                DurationMonths(e.StartDate, e.EndDate, e.IsCurrent),
                e.SortOrder)).ToArray(),
        resume.Skills.OrderBy(e => e.SortOrder)
            .Select(e => new EmployerResumeSkillView(
                e.Id,
                e.SkillId,
                e.SkillName,
                e.ProficiencyLevel,
                e.YearsOfExperience,
                e.SortOrder)).ToArray(),
        resume.Languages.OrderBy(e => e.SortOrder)
            .Select(e => new EmployerResumeLanguageView(
                e.Id,
                e.LanguageName,
                e.ProficiencyLevel,
                e.SortOrder)).ToArray(),
        resume.Projects.OrderBy(e => e.SortOrder)
            .Select(e => new EmployerResumeProjectView(
                e.Id,
                e.Title,
                e.Description,
                e.Url,
                e.StartDate,
                e.EndDate,
                e.IsCurrent,
                e.SortOrder)).ToArray(),
        resume.Certifications.OrderBy(e => e.SortOrder)
            .Select(e => new EmployerResumeCertificationView(
                e.Id,
                e.Name,
                e.IssuingOrganization,
                e.IssueDate,
                e.ExpiryDate,
                e.CertificateUrl,
                e.SortOrder)).ToArray(),
        resume.PortfolioLinks.OrderBy(e => e.SortOrder)
            .Select(e => new EmployerResumePortfolioLinkView(
                e.Id,
                e.Title,
                e.Url,
                e.Description,
                e.SortOrder)).ToArray());

    private static int DurationMonths(DateOnly? start, DateOnly? end, bool isCurrent)
    {
        if (start is null)
            return 0;
        var to = isCurrent || end is null ? DateOnly.FromDateTime(DateTime.Today) : end.Value;
        var months = (to.Year - start.Value.Year) * 12 + to.Month - start.Value.Month;
        if (to.Day < start.Value.Day)
            months--;
        return months < 0 ? 0 : months;
    }

    private async Task<Guid> GetCandidateProfileIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            throw new UnauthorizedException();

        return await candidateDbContext.CandidateProfiles.AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => profile.Id)
            .SingleOrDefaultAsync(cancellationToken) is var profileId && profileId != Guid.Empty
            ? profileId
            : throw new NotFoundException("A candidate profile is required to manage applications.");
    }

    private async Task<Guid> GetEmployerProfileIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            throw new UnauthorizedException();

        return await jobDbContext.Set<EmployerProfileEntity>()
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => profile.Id)
            .SingleOrDefaultAsync(cancellationToken) is var profileId && profileId != Guid.Empty
            ? profileId
            : throw new NotFoundException("An employer profile is required to manage applications.");
    }

    // ── Projection helpers ──────────────────────────────────────────

    private sealed record ApplicationRow(
        Guid Id,
        Guid JobId,
        string JobTitle,
        string CompanyName,
        JobApplicationStatus Status,
        DateTimeOffset AppliedAt,
        DateTimeOffset? UpdatedAt);

    private sealed record EmployerApplicationRow(
        Guid Id,
        Guid JobId,
        string JobTitle,
        Guid CandidateProfileId,
        JobApplicationStatus Status,
        DateTimeOffset AppliedAt,
        DateTimeOffset? UpdatedAt);

    private IQueryable<ApplicationRow> ProjectOwn(Guid candidateProfileId, Guid applicationId) =>
        ProjectOwnQuery(candidateProfileId).Where(row => row.Id == applicationId);

    private IQueryable<ApplicationRow> ProjectOwn(
        Guid candidateProfileId,
        bool orderInDatabase = false)
    {
        var query = ProjectOwnQuery(candidateProfileId);
        return orderInDatabase
            ? query.OrderByDescending(row => row.AppliedAt)
            : query;
    }

    private IQueryable<ApplicationRow> ProjectOwnQuery(Guid candidateProfileId) =>
        from app in jobDbContext.JobApplications.AsNoTracking()
        join job in jobDbContext.JobPostings.AsNoTracking() on app.JobPostingId equals job.Id
        join profile in jobDbContext.Set<EmployerProfileEntity>().AsNoTracking() on job.EmployerProfileId equals profile.Id
        where app.CandidateProfileId == candidateProfileId
        select new ApplicationRow(
            app.Id,
            job.Id,
            job.Title,
            profile.CompanyName,
            app.Status,
            app.CreatedAt,
            app.UpdatedAt);

    private IQueryable<EmployerApplicationRow> ProjectEmployer(
        Guid employerProfileId,
        bool orderInDatabase = false)
    {
        var query =
            from app in jobDbContext.JobApplications.AsNoTracking()
            join job in jobDbContext.JobPostings.AsNoTracking() on app.JobPostingId equals job.Id
            where job.EmployerProfileId == employerProfileId
            select new EmployerApplicationRow(
                app.Id,
                job.Id,
                job.Title,
                app.CandidateProfileId,
                app.Status,
                app.CreatedAt,
                app.UpdatedAt);

        return orderInDatabase
            ? query.OrderByDescending(row => row.AppliedAt)
            : query;
    }

    private static JobApplicationResponse Map(ApplicationRow row) => new(
        row.Id,
        row.JobId,
        row.JobTitle,
        row.CompanyName,
        row.Status.ToString(),
        row.AppliedAt,
        row.UpdatedAt);

    private static EmployerApplicationResponse MapEmployerDetail(
        JobApplication application,
        JobPosting job,
        CandidateProfile? candidateProfile) => new(
        application.Id,
        job.Id,
        job.Title,
        job.Description,
        application.CandidateProfileId,
        candidateProfile?.Headline,
        candidateProfile?.Biography,
        candidateProfile?.City,
        candidateProfile?.Province,
        candidateProfile?.LinkedInUrl,
        candidateProfile?.GitHubUrl,
        candidateProfile?.PortfolioUrl,
        candidateProfile?.ExpectedSalary,
        candidateProfile?.SalaryType?.ToString(),
        candidateProfile?.EmploymentStatus?.ToString(),
        candidateProfile?.Availability?.ToString(),
        candidateProfile?.MilitaryStatus?.ToString(),
        application.Status.ToString(),
        application.CreatedAt,
        application.UpdatedAt);

    private async Task<IReadOnlyList<EmployerApplicationListResponse>> MapEmployerAsync(
        IReadOnlyList<EmployerApplicationRow> rows,
        CancellationToken cancellationToken)
    {
        var candidateProfileIds = rows.Select(row => row.CandidateProfileId).ToArray();

        var candidateHeadlines = await (
            from profile in candidateDbContext.CandidateProfiles.AsNoTracking()
            where candidateProfileIds.Contains(profile.Id)
            select new { profile.Id, profile.Headline, profile.City })
            .ToDictionaryAsync(row => row.Id, row => row, cancellationToken);

        return rows.Select(row =>
        {
            var headline = candidateHeadlines.TryGetValue(row.CandidateProfileId, out var profile)
                ? profile.Headline
                : null;
            var city = candidateHeadlines.TryGetValue(row.CandidateProfileId, out var profileCity)
                ? profileCity.City
                : null;
            return new EmployerApplicationListResponse(
                row.Id,
                row.JobId,
                row.JobTitle,
                row.CandidateProfileId,
                headline,
                city,
                row.Status.ToString(),
                row.AppliedAt,
                row.UpdatedAt);
        }).ToArray();
    }

    private static string GetDownloadFileName(string contentType)
    {
        var ext = contentType switch
        {
            "application/pdf" => ".pdf",
            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            _ => ".bin"
        };
        return $"cv-{Guid.NewGuid():N}{ext}";
    }

    private static DomainException AlreadyApplied() =>
        new("You have already applied to this job.");
}
