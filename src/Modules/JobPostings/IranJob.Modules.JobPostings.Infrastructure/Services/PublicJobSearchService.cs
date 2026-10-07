using FluentValidation;
using IranJob.Modules.JobPostings.Application.Abstractions;
using IranJob.Modules.JobPostings.Application.Contracts;
using IranJob.Modules.JobPostings.Domain.Entities;
using IranJob.Modules.JobPostings.Domain.Enums;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.Modules.ReferenceData.Domain.Entities;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using IranJob.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = IranJob.SharedKernel.Exceptions.ValidationException;
using EmployerProfileEntity = IranJob.Modules.EmployerProfile.Domain.Entities.EmployerProfile;

namespace IranJob.Modules.JobPostings.Infrastructure.Services;

public sealed class PublicJobSearchService(
    JobPostingDbContext jobDbContext,
    ReferenceDataDbContext referenceDataDbContext,
    IValidator<PublicJobSearchQuery> queryValidator) : IPublicJobSearchService
{
    public async Task<PublicJobSearchPage> SearchAsync(PublicJobSearchQuery query, CancellationToken cancellationToken)
    {
        await ValidateQueryAsync(query, cancellationToken);
        await ValidateReferencesAsync(query, cancellationToken);

        IQueryable<JobPosting> postings = jobDbContext.JobPostings.AsNoTracking()
            .Where(posting => posting.Status == JobPostingStatus.Published);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var keyword = query.Q.Trim();
            postings = postings.Where(posting => posting.Title.Contains(keyword) || posting.Description.Contains(keyword));
        }

        if (query.CategoryId.HasValue)
            postings = postings.Where(posting => posting.CategoryId == query.CategoryId.Value);

        foreach (var skillId in query.SkillIds)
            postings = postings.Where(posting => posting.Skills.Any(link => link.SkillId == skillId));

        if (query.CityId.HasValue)
            postings = postings.Where(posting => posting.CityId == query.CityId.Value);

        if (query.ProvinceId.HasValue)
        {
            var provinceId = query.ProvinceId.Value;
            postings = postings.Where(posting => posting.CityId.HasValue && jobDbContext.Set<City>()
                .Any(city => city.Id == posting.CityId.Value && city.ProvinceId == provinceId));
        }

        if (query.CountryId.HasValue)
        {
            var countryId = query.CountryId.Value;
            postings = postings.Where(posting => posting.CityId.HasValue && jobDbContext.Set<City>()
                .Any(city => city.Id == posting.CityId.Value && city.Province.CountryId == countryId));
        }

        if (query.WorkArrangement is not null)
        {
            var arrangement = Enum.Parse<WorkArrangement>(query.WorkArrangement, ignoreCase: false);
            postings = postings.Where(posting => posting.WorkArrangement == arrangement);
        }

        if (query.EmploymentType is not null)
        {
            var employmentType = Enum.Parse<EmploymentType>(query.EmploymentType, ignoreCase: false);
            postings = postings.Where(posting => posting.EmploymentType == employmentType);
        }

        if (query.SalaryMinimum.HasValue)
            postings = postings.Where(posting => posting.SalaryMaximum >= query.SalaryMinimum);

        if (query.SalaryMaximum.HasValue)
            postings = postings.Where(posting => posting.SalaryMinimum <= query.SalaryMaximum);

        if (!string.IsNullOrWhiteSpace(query.SalaryCurrency))
            postings = postings.Where(posting => posting.SalaryCurrency == query.SalaryCurrency.ToUpperInvariant());

        var totalCount = await postings.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);
        var sorted = ApplySort(postings, query.Sort, jobDbContext.Database.ProviderName);
        var rows = await Project(sorted).Skip((int)((long)query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var skills = await LoadSkillsAsync(rows.Select(row => row.Id).ToArray(), cancellationToken);

        return new PublicJobSearchPage(
            rows.Select(row => row.ToSearchItem(skills.GetValueOrDefault(row.Id) ?? []))
                .ToArray(),
            query.Page,
            query.PageSize,
            totalCount,
            totalPages);
    }

    public async Task<PublicJobDetails> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await Project(jobDbContext.JobPostings.AsNoTracking()
                .Where(posting => posting.Id == id && posting.Status == JobPostingStatus.Published))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Published job posting was not found.");

        var skills = await LoadSkillsAsync([id], cancellationToken);
        return row.ToDetails(skills.GetValueOrDefault(id) ?? []);
    }

    private async Task ValidateQueryAsync(PublicJobSearchQuery query, CancellationToken cancellationToken)
    {
        var result = await queryValidator.ValidateAsync(query, cancellationToken);
        if (!result.IsValid)
            throw new DomainValidationException(result.Errors.GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
    }

    private async Task ValidateReferencesAsync(PublicJobSearchQuery query, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (query.CategoryId.HasValue && !await referenceDataDbContext.JobCategories.AnyAsync(
                category => category.Id == query.CategoryId.Value && category.IsActive, cancellationToken))
            errors[nameof(query.CategoryId)] = ["The selected category does not exist or is inactive."];

        if (query.SkillIds.Count > 0)
        {
            var activeCount = await referenceDataDbContext.Skills.CountAsync(
                skill => query.SkillIds.Contains(skill.Id) && skill.IsActive, cancellationToken);
            if (activeCount != query.SkillIds.Count)
                errors[nameof(query.SkillIds)] = ["Every selected skill must exist and be active."];
        }

        if (query.CountryId.HasValue && !await referenceDataDbContext.Countries.AnyAsync(
                country => country.Id == query.CountryId.Value && country.IsActive, cancellationToken))
            errors[nameof(query.CountryId)] = ["The selected country does not exist or is inactive."];

        if (query.ProvinceId.HasValue && !await referenceDataDbContext.Provinces.AnyAsync(
                province => province.Id == query.ProvinceId.Value && province.IsActive
                    && province.Country.IsActive
                    && (!query.CountryId.HasValue || province.CountryId == query.CountryId.Value), cancellationToken))
            errors[nameof(query.ProvinceId)] = ["The selected province is inactive or does not belong to the selected country."];

        if (query.CityId.HasValue && !await referenceDataDbContext.Cities.AnyAsync(
                city => city.Id == query.CityId.Value && city.IsActive && city.Province.IsActive
                    && city.Province.Country.IsActive
                    && (!query.ProvinceId.HasValue || city.ProvinceId == query.ProvinceId.Value)
                    && (!query.CountryId.HasValue || city.Province.CountryId == query.CountryId.Value), cancellationToken))
            errors[nameof(query.CityId)] = ["The selected city does not belong to the selected province and country."];

        if (errors.Count > 0)
            throw new DomainValidationException(errors);
    }

    private IQueryable<SearchRow> Project(IQueryable<JobPosting> postings) =>
        from posting in postings
        join employer in jobDbContext.Set<EmployerProfileEntity>().AsNoTracking()
            on posting.EmployerProfileId equals employer.Id
        join category in jobDbContext.Set<JobCategory>().AsNoTracking()
            on posting.CategoryId equals category.Id
        join city in jobDbContext.Set<City>().AsNoTracking()
            on posting.CityId equals (Guid?)city.Id
        join province in jobDbContext.Set<Province>().AsNoTracking()
            on city.ProvinceId equals province.Id
        join country in jobDbContext.Set<Country>().AsNoTracking()
            on province.CountryId equals country.Id
        select new SearchRow(
            posting.Id,
            posting.Title,
            posting.Description,
            employer.CompanyName,
            employer.CompanyDescription,
            employer.Industry,
            employer.WebsiteUrl,
            employer.LogoUrl,
            category.Name,
            city.Name,
            province.Name,
            country.Name,
            posting.EmploymentType.ToString(),
            posting.WorkArrangement.ToString(),
            posting.SalaryMinimum,
            posting.SalaryMaximum,
            posting.SalaryCurrency,
            posting.SalaryPeriod.HasValue ? posting.SalaryPeriod.GetValueOrDefault().ToString() : null,
            posting.PublishedAt!.Value);

    private async Task<Dictionary<Guid, IReadOnlyList<string>>> LoadSkillsAsync(
        Guid[] postingIds,
        CancellationToken cancellationToken)
    {
        if (postingIds.Length == 0)
            return [];

        var rows = await (
            from link in jobDbContext.JobPostingSkills.AsNoTracking()
            join skill in jobDbContext.Set<Skill>().AsNoTracking() on link.SkillId equals skill.Id
            where postingIds.Contains(link.JobPostingId)
            orderby skill.Name
            select new SkillRow(link.JobPostingId, skill.Name))
            .ToListAsync(cancellationToken);

        return rows.GroupBy(row => row.JobPostingId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.Select(row => row.Name).ToArray());
    }

    private static IOrderedQueryable<JobPosting> ApplySort(IQueryable<JobPosting> postings, string sort, string? providerName)
    {
        var isSqlite = providerName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        if (sort == "SalaryAscending" && isSqlite)
            return postings.OrderBy(posting => posting.SalaryMinimum.HasValue ? 0 : 1)
                .ThenBy(posting => (double?)posting.SalaryMinimum).ThenBy(posting => posting.Id);
        if (sort == "SalaryDescending" && isSqlite)
            return postings.OrderBy(posting => posting.SalaryMaximum.HasValue ? 0 : 1)
                .ThenByDescending(posting => (double?)posting.SalaryMaximum).ThenBy(posting => posting.Id);
        if (sort == "SalaryAscending")
            return postings.OrderBy(posting => posting.SalaryMinimum.HasValue ? 0 : 1)
                .ThenBy(posting => posting.SalaryMinimum).ThenBy(posting => posting.Id);
        if (sort == "SalaryDescending")
            return postings.OrderBy(posting => posting.SalaryMaximum.HasValue ? 0 : 1)
                .ThenByDescending(posting => posting.SalaryMaximum).ThenBy(posting => posting.Id);

        var oldestFirst = sort == "Oldest";
        if (isSqlite)
        {
            return oldestFirst
                ? postings.OrderBy(posting => posting.PublishedAt.ToString()).ThenBy(posting => posting.Id)
                : postings.OrderByDescending(posting => posting.PublishedAt.ToString()).ThenBy(posting => posting.Id);
        }

        return oldestFirst
            ? postings.OrderBy(posting => posting.PublishedAt).ThenBy(posting => posting.Id)
            : postings.OrderByDescending(posting => posting.PublishedAt).ThenBy(posting => posting.Id);
    }

    private sealed record SkillRow(Guid JobPostingId, string Name);

    private sealed record SearchRow(
        Guid Id,
        string Title,
        string Description,
        string CompanyName,
        string? CompanyDescription,
        string Industry,
        string? WebsiteUrl,
        string? LogoUrl,
        string Category,
        string City,
        string Province,
        string Country,
        string EmploymentType,
        string WorkArrangement,
        decimal? SalaryMinimum,
        decimal? SalaryMaximum,
        string? SalaryCurrency,
        string? SalaryPeriod,
        DateTimeOffset PublishedAt)
    {
        public PublicJobSearchItem ToSearchItem(IReadOnlyList<string> skills) => new(
            Id, Title, CompanyName, LogoUrl, Category, Description, skills, City, Province, Country,
            EmploymentType, WorkArrangement, SalaryMinimum, SalaryMaximum, SalaryCurrency, SalaryPeriod, PublishedAt);

        public PublicJobDetails ToDetails(IReadOnlyList<string> skills) => new(
            Id, Title, Description,
            new PublicEmployerInformation(CompanyName, CompanyDescription, Industry, WebsiteUrl, LogoUrl),
            Category, skills, City, Province, Country, EmploymentType, WorkArrangement,
            SalaryMinimum, SalaryMaximum, SalaryCurrency, SalaryPeriod, PublishedAt);
    }
}
