using IranJob.Modules.Candidates.Infrastructure.Persistence;
using IranJob.Modules.JobPostings.Application.Contracts;
using IranJob.Modules.JobPostings.Domain.Entities;
using IranJob.Modules.JobPostings.Domain.Enums;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.Modules.ReferenceData.Domain.Entities;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using IranJob.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using EmployerProfileEntity = IranJob.Modules.EmployerProfile.Domain.Entities.EmployerProfile;

namespace IranJob.Modules.JobPostings.Infrastructure.Services;

public sealed class CandidateJobPreferenceService(
    CandidateDbContext candidateDb,
    JobPostingDbContext jobsDb,
    ReferenceDataDbContext referenceDb) : ICandidateJobPreferenceService
{
    public async Task<bool> SaveJobAsync(Guid userId, Guid jobId, CancellationToken ct)
    {
        var profileId = await GetCandidateProfileIdAsync(userId, ct);
        var published = await jobsDb.JobPostings.AnyAsync(x => x.Id == jobId && x.Status == JobPostingStatus.Published, ct);
        if (!published) throw new NotFoundException("Published job posting was not found.");
        if (await jobsDb.SavedJobs.AnyAsync(x => x.CandidateProfileId == profileId && x.JobPostingId == jobId, ct))
            return false;
        jobsDb.SavedJobs.Add(SavedJob.Create(profileId, jobId));
        try
        {
            await jobsDb.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            if (await jobsDb.SavedJobs.AsNoTracking().AnyAsync(x => x.CandidateProfileId == profileId && x.JobPostingId == jobId, ct))
                return false;
            throw;
        }
    }

    public async Task<bool> RemoveSavedJobAsync(Guid userId, Guid jobId, CancellationToken ct)
    {
        var profileId = await GetCandidateProfileIdAsync(userId, ct);
        var saved = await jobsDb.SavedJobs.SingleOrDefaultAsync(x => x.CandidateProfileId == profileId && x.JobPostingId == jobId, ct);
        if (saved is null) return false;
        jobsDb.SavedJobs.Remove(saved);
        await jobsDb.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SavedJobPage> ListSavedJobsAsync(Guid userId, int page, int pageSize, CancellationToken ct)
    {
        ValidatePage(page, pageSize);
        var profileId = await GetCandidateProfileIdAsync(userId, ct);
        var query = jobsDb.SavedJobs.AsNoTracking().Where(x => x.CandidateProfileId == profileId);
        var total = await query.CountAsync(ct);
        var savedRows = await query.OrderByDescending(x => x.SavedAt).ThenBy(x => x.JobPostingId)
            .Skip((int)((long)page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.JobPostingId, x.SavedAt }).ToListAsync(ct);
        var ids = savedRows.Select(x => x.JobPostingId).ToArray();
        var available = await (from job in jobsDb.JobPostings.AsNoTracking()
                               join employer in jobsDb.Set<EmployerProfileEntity>().AsNoTracking() on job.EmployerProfileId equals employer.Id
                               join category in jobsDb.Set<JobCategory>().AsNoTracking() on job.CategoryId equals category.Id
                               join city in jobsDb.Set<City>().AsNoTracking() on job.CityId equals (Guid?)city.Id
                               where ids.Contains(job.Id) && job.Status == JobPostingStatus.Published
                               select new { job.Id, job.Title, employer.CompanyName, Category = category.Name, City = city.Name })
            .ToDictionaryAsync(x => x.Id, ct);
        var items = savedRows.Select(saved => available.TryGetValue(saved.JobPostingId, out var job)
            ? new SavedJobItem(saved.JobPostingId, job.Title, job.CompanyName, job.Category, job.City, saved.SavedAt, true)
            : new SavedJobItem(saved.JobPostingId, "آگهی دیگر در دسترس نیست", string.Empty, string.Empty, string.Empty, saved.SavedAt, false)).ToArray();
        return new SavedJobPage(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<Guid>> GetSavedStatusesAsync(Guid userId, IReadOnlyCollection<Guid> jobIds, CancellationToken ct)
    {
        if (jobIds.Count > 100)
            throw new ValidationException(new Dictionary<string, string[]> { [nameof(jobIds)] = ["At most 100 job identifiers may be checked."] });
        if (jobIds.Count == 0) return [];
        var profileId = await GetCandidateProfileIdAsync(userId, ct);
        return await jobsDb.SavedJobs.AsNoTracking()
            .Where(x => x.CandidateProfileId == profileId && jobIds.Contains(x.JobPostingId))
            .Select(x => x.JobPostingId).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<JobAlertDto>> ListAlertsAsync(Guid userId, CancellationToken ct)
    {
        var profileId = await GetCandidateProfileIdAsync(userId, ct);
        var alerts = await jobsDb.JobAlerts.AsNoTracking().Include(x => x.Skills)
            .Where(x => x.CandidateProfileId == profileId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return alerts.Select(Map).ToArray();
    }

    public async Task<JobAlertDto> CreateAlertAsync(Guid userId, JobAlertRequest request, CancellationToken ct)
    {
        var profileId = await GetCandidateProfileIdAsync(userId, ct);
        await ValidateAlertAsync(request, ct);
        var alert = JobAlert.Create(profileId, request.Name, request.CategoryId, request.CityId,
            request.WorkArrangement, request.SkillIds);
        alert.SetEnabled(request.IsEnabled);
        jobsDb.JobAlerts.Add(alert);
        await jobsDb.SaveChangesAsync(ct);
        return Map(alert);
    }

    public async Task<JobAlertDto> UpdateAlertAsync(Guid userId, Guid alertId, JobAlertRequest request, CancellationToken ct)
    {
        var profileId = await GetCandidateProfileIdAsync(userId, ct);
        await ValidateAlertAsync(request, ct);
        var alert = await jobsDb.JobAlerts.Include(x => x.Skills)
            .SingleOrDefaultAsync(x => x.Id == alertId && x.CandidateProfileId == profileId, ct)
            ?? throw new NotFoundException("Job alert was not found.");
        alert.Update(request.Name, request.CategoryId, request.CityId, request.WorkArrangement, request.SkillIds);
        alert.SetEnabled(request.IsEnabled);
        await jobsDb.SaveChangesAsync(ct);
        return Map(alert);
    }

    public async Task DeleteAlertAsync(Guid userId, Guid alertId, CancellationToken ct)
    {
        var profileId = await GetCandidateProfileIdAsync(userId, ct);
        var alert = await jobsDb.JobAlerts.SingleOrDefaultAsync(x => x.Id == alertId && x.CandidateProfileId == profileId, ct)
            ?? throw new NotFoundException("Job alert was not found.");
        jobsDb.JobAlerts.Remove(alert);
        await jobsDb.SaveChangesAsync(ct);
    }

    private async Task ValidateAlertAsync(JobAlertRequest request, CancellationToken ct)
    {
        if (request.SkillIds is null)
            throw new ValidationException(new Dictionary<string, string[]> { [nameof(request.SkillIds)] = ["Skill identifiers are required as a list."] });
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
            throw new ValidationException(new Dictionary<string, string[]> { [nameof(request.Name)] = ["Name is required and must be at most 100 characters."] });
        if (request.SkillIds.Count > 30 || request.SkillIds.Distinct().Count() != request.SkillIds.Count)
            throw new ValidationException(new Dictionary<string, string[]> { [nameof(request.SkillIds)] = ["Select up to 30 distinct skills."] });
        if (!request.CategoryId.HasValue && !request.CityId.HasValue && string.IsNullOrWhiteSpace(request.WorkArrangement) && request.SkillIds.Count == 0)
            throw new ValidationException(new Dictionary<string, string[]> { [nameof(request)] = ["Select at least one alert criterion."] });
        if (request.WorkArrangement is not null && (!Enum.TryParse<IranJob.Modules.JobPostings.Domain.Enums.WorkArrangement>(request.WorkArrangement, false, out var arrangement)
            || !Enum.IsDefined(arrangement)))
            throw new ValidationException(new Dictionary<string, string[]> { [nameof(request.WorkArrangement)] = ["Select a supported work arrangement."] });
        if (request.CategoryId.HasValue && !await referenceDb.JobCategories.AnyAsync(x => x.Id == request.CategoryId && x.IsActive, ct))
            throw new ValidationException(new Dictionary<string, string[]> { [nameof(request.CategoryId)] = ["Category is inactive or unavailable."] });
        if (request.CityId.HasValue && !await referenceDb.Cities.AnyAsync(x => x.Id == request.CityId && x.IsActive && x.Province.IsActive && x.Province.Country.IsActive, ct))
            throw new ValidationException(new Dictionary<string, string[]> { [nameof(request.CityId)] = ["City is inactive or unavailable."] });
        if (request.SkillIds.Count > 0 && await referenceDb.Skills.CountAsync(x => request.SkillIds.Contains(x.Id) && x.IsActive, ct) != request.SkillIds.Count)
            throw new ValidationException(new Dictionary<string, string[]> { [nameof(request.SkillIds)] = ["Every skill must be active."] });
    }

    private async Task<Guid> GetCandidateProfileIdAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty) throw new UnauthorizedException();
        return await candidateDb.CandidateProfiles.AsNoTracking().Where(x => x.UserId == userId)
            .Select(x => x.Id).SingleOrDefaultAsync(ct) is var id && id != Guid.Empty
            ? id : throw new NotFoundException("A candidate profile is required.");
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)page * pageSize > int.MaxValue)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(page)] = ["Page must be a positive 1-based index."],
                [nameof(pageSize)] = ["Page size must be between 1 and 100."]
            });
    }

    private static JobAlertDto Map(JobAlert x) => new(x.Id, x.Name, x.CategoryId, x.CityId, x.WorkArrangement,
        x.Skills.Select(s => s.SkillId).ToArray(), x.IsEnabled, x.CreatedAt, x.UpdatedAt);
}
