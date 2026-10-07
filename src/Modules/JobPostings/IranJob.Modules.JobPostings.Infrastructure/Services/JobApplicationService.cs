using IranJob.Modules.Candidates.Domain.Entities;
using IranJob.Modules.Candidates.Infrastructure.Persistence;
using IranJob.Modules.JobPostings.Application.Abstractions;
using IranJob.Modules.JobPostings.Application.Contracts;
using IranJob.Modules.JobPostings.Domain.Entities;
using IranJob.Modules.JobPostings.Domain.Enums;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using EmployerProfileEntity = IranJob.Modules.EmployerProfile.Domain.Entities.EmployerProfile;

namespace IranJob.Modules.JobPostings.Infrastructure.Services;

public sealed class JobApplicationService(
    JobPostingDbContext jobDbContext,
    CandidateDbContext candidateDbContext) : IJobApplicationService
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
            select new { Application = application, Job = job })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Application was not found.");

        var candidateProfile = await candidateDbContext.CandidateProfiles.AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.Id == row.Application.CandidateProfileId, cancellationToken);

        return MapEmployerDetail(row.Application, row.Job, candidateProfile);
    }

    public async Task<EmployerApplicationResponse> ReviewApplicationAsync(
        Guid employerUserId,
        Guid applicationId,
        CancellationToken cancellationToken) =>
        await UpdateEmployerApplicationStatusAsync(employerUserId, applicationId, application => application.Review(), cancellationToken);

    public async Task<EmployerApplicationResponse> AcceptApplicationAsync(
        Guid employerUserId,
        Guid applicationId,
        CancellationToken cancellationToken) =>
        await UpdateEmployerApplicationStatusAsync(employerUserId, applicationId, application => application.Accept(), cancellationToken);

    public async Task<EmployerApplicationResponse> RejectApplicationAsync(
        Guid employerUserId,
        Guid applicationId,
        CancellationToken cancellationToken) =>
        await UpdateEmployerApplicationStatusAsync(employerUserId, applicationId, application => application.Reject(), cancellationToken);

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

        return await jobDbContext.Set<EmployerProfileEntity>().AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => profile.Id)
            .SingleOrDefaultAsync(cancellationToken) is var profileId && profileId != Guid.Empty
            ? profileId
            : throw new NotFoundException("An employer profile is required to manage applications.");
    }

    private IQueryable<ApplicationRow> ProjectOwn(
        Guid candidateProfileId,
        Guid? applicationId = null,
        bool orderInDatabase = false)
    {
        var rows =
            from application in jobDbContext.JobApplications.AsNoTracking()
            join job in jobDbContext.JobPostings.AsNoTracking() on application.JobPostingId equals job.Id
            join employer in jobDbContext.Set<EmployerProfileEntity>().AsNoTracking()
                on job.EmployerProfileId equals employer.Id
            where application.CandidateProfileId == candidateProfileId
            select new { Application = application, Job = job, Employer = employer };

        if (applicationId.HasValue)
            rows = rows.Where(row => row.Application.Id == applicationId.Value);
        else if (orderInDatabase)
            rows = rows.OrderByDescending(row => row.Application.CreatedAt);

        return rows.Select(row => new ApplicationRow(
            row.Application.Id,
            row.Job.Id,
            row.Job.Title,
            row.Employer.CompanyName,
            row.Application.Status,
            row.Application.CreatedAt,
            row.Application.UpdatedAt));
    }

    private IQueryable<EmployerApplicationRow> ProjectEmployer(
        Guid employerProfileId,
        bool orderInDatabase = false)
    {
        var rows =
            from application in jobDbContext.JobApplications.AsNoTracking()
            join job in jobDbContext.JobPostings.AsNoTracking() on application.JobPostingId equals job.Id
            where job.EmployerProfileId == employerProfileId
            select new EmployerApplicationRow(
                application.Id,
                job.Id,
                job.Title,
                application.CandidateProfileId,
                application.Status,
                application.CreatedAt,
                application.UpdatedAt);

        if (orderInDatabase)
            rows = rows.OrderByDescending(row => row.AppliedAt);

        return rows;
    }

    private static JobApplicationResponse Map(ApplicationRow row) => new(
        row.Id,
        row.JobId,
        row.JobTitle,
        row.CompanyName,
        row.Status.ToString(),
        row.AppliedAt,
        row.UpdatedAt);

    private async Task<EmployerApplicationResponse> UpdateEmployerApplicationStatusAsync(
        Guid employerUserId,
        Guid applicationId,
        Action<JobApplication> update,
        CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(employerUserId, cancellationToken);

        var application = await (
            from app in jobDbContext.JobApplications
            join job in jobDbContext.JobPostings on app.JobPostingId equals job.Id
            where app.Id == applicationId && job.EmployerProfileId == employerProfileId
            select app)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Application was not found.");

        update(application);
        await jobDbContext.SaveChangesAsync(cancellationToken);

        return await BuildEmployerDetailAsync(application, cancellationToken);
    }

    private async Task<EmployerApplicationResponse> BuildEmployerDetailAsync(
        JobApplication application,
        CancellationToken cancellationToken)
    {
        var job = await jobDbContext.JobPostings.AsNoTracking()
            .FirstOrDefaultAsync(posting => posting.Id == application.JobPostingId, cancellationToken)
            ?? throw new NotFoundException("Job posting was not found.");

        var candidateProfile = await candidateDbContext.CandidateProfiles.AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.Id == application.CandidateProfileId, cancellationToken);

        return MapEmployerDetail(application, job, candidateProfile);
    }

    private async Task<IReadOnlyList<EmployerApplicationListResponse>> MapEmployerAsync(
        IReadOnlyList<EmployerApplicationRow> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
            return Array.Empty<EmployerApplicationListResponse>();

        var candidateIds = rows.Select(row => row.CandidateProfileId).Distinct().ToArray();
        var candidateProfiles = await candidateDbContext.CandidateProfiles.AsNoTracking()
            .Where(profile => candidateIds.Contains(profile.Id))
            .ToDictionaryAsync(profile => profile.Id, cancellationToken);

        return rows.Select(row =>
        {
            candidateProfiles.TryGetValue(row.CandidateProfileId, out var profile);
            return new EmployerApplicationListResponse(
                row.Id,
                row.JobId,
                row.JobTitle,
                row.CandidateProfileId,
                profile?.Headline,
                profile?.City,
                row.Status.ToString(),
                row.AppliedAt,
                row.UpdatedAt);
        }).ToArray();
    }

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

    private static DomainException AlreadyApplied() =>
        new("You have already applied to this job.");
}

