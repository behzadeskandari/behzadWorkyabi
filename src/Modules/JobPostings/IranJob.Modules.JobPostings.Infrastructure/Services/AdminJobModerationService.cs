using IranJob.Modules.EmployerProfile.Infrastructure.Persistence;
using IranJob.Modules.JobPostings.Application.Abstractions;
using IranJob.Modules.JobPostings.Application.Contracts;
using IranJob.Modules.JobPostings.Domain.Entities;
using IranJob.Modules.JobPostings.Domain.Enums;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.Modules.ReferenceData.Domain.Entities;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using IranJob.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.JobPostings.Infrastructure.Services;

public sealed class AdminJobModerationService(
    JobPostingDbContext jobDbContext,
    EmployerDbContext employerDbContext,
    ReferenceDataDbContext referenceDataDbContext) : IAdminJobModerationService
{
    public async Task<AdminJobPageResult> GetJobsAsync(
        int page,
        int pageSize,
        string? search = null,
        string? status = null,
        Guid? categoryId = null,
        Guid? cityId = null,
        string? employerName = null,
        string sort = "Newest",
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var isSqlite = jobDbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";

        IQueryable<JobPosting> postings = jobDbContext.JobPostings.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            postings = postings.Where(posting => posting.Title.Contains(keyword) || posting.Description.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<JobPostingStatus>(status, ignoreCase: true, out var parsedStatus))
                postings = postings.Where(posting => posting.Status == parsedStatus);
        }

        if (categoryId.HasValue)
            postings = postings.Where(posting => posting.CategoryId == categoryId.Value);

        if (cityId.HasValue)
            postings = postings.Where(posting => posting.CityId == cityId.Value);

        if (!string.IsNullOrWhiteSpace(employerName))
        {
            var employerIds = await employerDbContext.EmployerProfiles.AsNoTracking()
                .Where(profile => profile.CompanyName.Contains(employerName.Trim()))
                .Select(profile => profile.Id)
                .ToArrayAsync(cancellationToken);
            postings = postings.Where(posting => employerIds.Contains(posting.EmployerProfileId));
        }

        postings = sort switch
        {
            "Oldest" => isSqlite
                ? postings.OrderBy(posting => posting.CreatedAt.ToString()).ThenBy(posting => posting.Id)
                : postings.OrderBy(posting => posting.CreatedAt).ThenBy(posting => posting.Id),
            "SalaryAscending" => isSqlite
                ? postings.OrderBy(posting => posting.SalaryMinimum.HasValue ? 0 : 1)
                    .ThenBy(posting => (double?)posting.SalaryMinimum).ThenBy(posting => posting.Id)
                : postings.OrderBy(posting => posting.SalaryMinimum.HasValue ? 0 : 1)
                    .ThenBy(posting => posting.SalaryMinimum).ThenBy(posting => posting.Id),
            "SalaryDescending" => isSqlite
                ? postings.OrderByDescending(posting => posting.SalaryMaximum.HasValue ? 0 : 1)
                    .ThenByDescending(posting => (double?)posting.SalaryMaximum).ThenBy(posting => posting.Id)
                : postings.OrderByDescending(posting => posting.SalaryMaximum.HasValue ? 0 : 1)
                    .ThenByDescending(posting => posting.SalaryMaximum).ThenBy(posting => posting.Id),
            _ => isSqlite
                ? postings.OrderByDescending(posting => posting.CreatedAt.ToString()).ThenBy(posting => posting.Id)
                : postings.OrderByDescending(posting => posting.CreatedAt).ThenBy(posting => posting.Id),
        };

        var totalCount = await postings.CountAsync(cancellationToken);
        var items = await ApplyPaginationAsync(postings.Skip((page - 1) * pageSize).Take(pageSize), cancellationToken);

        var totalPages = (totalCount + pageSize - 1) / pageSize;

        return new AdminJobPageResult(
            Items: items,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            TotalPages: totalPages);
    }

    public async Task<AdminJobDetailResponse> GetJobByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var posting = await jobDbContext.JobPostings.AsNoTracking()
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(posting => posting.Id == id, cancellationToken);

        if (posting is null)
            throw new NotFoundException($"Job posting with ID '{id}' was not found.");

        return await BuildDetailAsync(posting, cancellationToken);
    }

    public async Task<AdminJobDetailResponse> ModerateUnpublishAsync(
        Guid id, Guid actorUserId, string? reason, CancellationToken cancellationToken)
    {
        return await ExecuteModerationAsync(id, actorUserId, reason, posting => posting.ModerateUnpublish(actorUserId, reason), cancellationToken);
    }

    public async Task<AdminJobDetailResponse> ModeratePublishAsync(
        Guid id, Guid actorUserId, string? reason, CancellationToken cancellationToken)
    {
        return await ExecuteModerationAsync(id, actorUserId, reason, posting => posting.ModeratePublish(actorUserId, reason), cancellationToken);
    }

    public async Task<AdminJobDetailResponse> ModerateCloseAsync(
        Guid id, Guid actorUserId, string? reason, CancellationToken cancellationToken)
    {
        return await ExecuteModerationAsync(id, actorUserId, reason, posting => posting.ModerateClose(actorUserId, reason), cancellationToken);
    }

    private async Task<AdminJobDetailResponse> ExecuteModerationAsync(
        Guid id, Guid actorUserId, string? reason,
        Action<JobPosting> applyModeration, CancellationToken cancellationToken)
    {
        var posting = await jobDbContext.JobPostings
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(posting => posting.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Job posting with ID '{id}' was not found.");

        applyModeration(posting);
        await jobDbContext.SaveChangesAsync(cancellationToken);
        return await BuildDetailAsync(posting, cancellationToken);
    }

    private async Task<IReadOnlyList<AdminJobListItemResponse>> ApplyPaginationAsync(
        IQueryable<JobPosting> postings, CancellationToken cancellationToken)
    {
        var rows = await postings.ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return Array.Empty<AdminJobListItemResponse>();

        var categoryIds = rows.Select(r => r.CategoryId).Distinct().ToArray();
        var cityIds = rows.Where(r => r.CityId.HasValue).Select(r => r.CityId!.Value).Distinct().ToArray();
        var employerProfileIds = rows.Select(r => r.EmployerProfileId).Distinct().ToArray();

        var categories = categoryIds.Length == 0
            ? new Dictionary<Guid, JobCategory>()
            : await referenceDataDbContext.JobCategories.AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);

        var cities = cityIds.Length == 0
            ? new Dictionary<Guid, City>()
            : await referenceDataDbContext.Cities.AsNoTracking()
                .Include(c => c.Province).ThenInclude(p => p.Country)
                .Where(c => cityIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);

        var employers = await employerDbContext.EmployerProfiles.AsNoTracking()
            .Where(e => employerProfileIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);

        return rows.Select(row =>
        {
            cities.TryGetValue(row.CityId ?? Guid.Empty, out var city);
            employers.TryGetValue(row.EmployerProfileId, out var employer);
            categories.TryGetValue(row.CategoryId, out var category);
            return new AdminJobListItemResponse(
                row.Id,
                row.Title,
                row.CategoryId,
                category?.Name ?? string.Empty,
                row.EmploymentType.ToString(),
                row.WorkArrangement.ToString(),
                city?.Name,
                city?.Province.Name,
                city?.Province.Country.Name,
                employer?.CompanyName ?? string.Empty,
                employer?.UserId ?? Guid.Empty,
                row.SalaryMinimum,
                row.SalaryMaximum,
                row.SalaryCurrency,
                row.Status.ToString(),
                row.CreatedAt,
                row.PublishedAt,
                row.ClosedAt,
                row.ModeratedByUserId,
                row.ModerationReason,
                row.ModeratedAt);
        }).ToArray();
    }

    private async Task<AdminJobDetailResponse> BuildDetailAsync(JobPosting posting, CancellationToken cancellationToken)
    {
        var skillIds = posting.Skills.Select(s => s.SkillId).Distinct().ToArray();
        var skills = skillIds.Length == 0
            ? new Dictionary<Guid, Skill>()
            : await referenceDataDbContext.Skills.AsNoTracking()
                .Where(s => skillIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);

        var skillsList = posting.Skills.Select(link => skills.TryGetValue(link.SkillId, out var skill)
                ? new JobPostingReference(skill.Id, skill.Name)
                : null)
            .Where(s => s is not null)
            .Select(s => s!)
            .OrderBy(s => s.Name)
            .ToArray();

        var category = await referenceDataDbContext.JobCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == posting.CategoryId, cancellationToken)
            ?? throw new NotFoundException($"Job category with ID '{posting.CategoryId}' was not found.");

        var employer = await employerDbContext.EmployerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == posting.EmployerProfileId, cancellationToken)
            ?? throw new NotFoundException($"Employer profile with ID '{posting.EmployerProfileId}' was not found.");

        string? cityName = null, provinceName = null, countryName = null;
        if (posting.CityId.HasValue)
        {
            var city = await referenceDataDbContext.Cities.AsNoTracking()
                .Include(c => c.Province).ThenInclude(p => p.Country)
                .FirstOrDefaultAsync(c => c.Id == posting.CityId.Value, cancellationToken);
            if (city is not null)
            {
                cityName = city.Name;
                provinceName = city.Province?.Name;
                countryName = city.Province?.Country?.Name;
            }
        }

        return new AdminJobDetailResponse(
            posting.Id,
            posting.Title,
            posting.Description,
            posting.CategoryId,
            category.Name,
            skillsList,
            posting.EmploymentType.ToString(),
            posting.WorkArrangement.ToString(),
            posting.CityId,
            cityName,
            provinceName,
            countryName,
            posting.SalaryMinimum,
            posting.SalaryMaximum,
            posting.SalaryCurrency,
            posting.SalaryPeriod?.ToString(),
            posting.ClosingAt,
            posting.Status.ToString(),
            posting.CreatedAt,
            posting.UpdatedAt,
            posting.PublishedAt,
            posting.ClosedAt,
            employer.UserId,
            employer.CompanyName,
            employer.Industry,
            posting.ModeratedByUserId,
            posting.ModerationReason,
            posting.ModeratedAt);
    }
}