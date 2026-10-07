using FluentValidation;
using IranJob.Modules.ReferenceData.Application.Abstractions;
using IranJob.Modules.ReferenceData.Application.Contracts;
using IranJob.Modules.ReferenceData.Domain.Entities;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using IranJob.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.ReferenceData.Infrastructure.Services;

public sealed class ReferenceDataService(
    ReferenceDataDbContext dbContext,
    IValidator<NamedReferenceDataInput> namedValidator,
    IValidator<CountryInput> countryValidator,
    IValidator<ProvinceInput> provinceValidator,
    IValidator<CityInput> cityValidator) : IReferenceDataService
{
    public async Task<IReadOnlyList<ReferenceDataItem>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await dbContext.JobCategories.AsNoTracking().Where(category => category.IsActive)
            .OrderBy(category => category.Name).Select(category => Map(category)).ToListAsync(cancellationToken);

    public async Task<ReferenceDataItem> GetCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await dbContext.JobCategories.AsNoTracking().FirstOrDefaultAsync(category => category.Id == id && category.IsActive, cancellationToken)
            ?? throw Missing("Job category", id));

    public async Task<ReferenceDataItem> CreateCategoryAsync(NamedReferenceDataInput input, CancellationToken cancellationToken)
    {
        await ValidateAsync(namedValidator, input, cancellationToken);
        var normalizedName = Normalize(input.Name);
        if (await dbContext.JobCategories.AnyAsync(category => category.NormalizedName == normalizedName, cancellationToken))
            throw Duplicate("Job category", input.Name);
        var category = JobCategory.Create(input.Name, input.Description);
        dbContext.JobCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(category);
    }

    public async Task<ReferenceDataItem> UpdateCategoryAsync(Guid id, NamedReferenceDataInput input, CancellationToken cancellationToken)
    {
        await ValidateAsync(namedValidator, input, cancellationToken);
        var category = await dbContext.JobCategories.FirstOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw Missing("Job category", id);
        if (await dbContext.JobCategories.AnyAsync(item => item.Id != id && item.NormalizedName == Normalize(input.Name), cancellationToken))
            throw Duplicate("Job category", input.Name);
        category.Update(input.Name, input.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(category);
    }

    public async Task DeactivateCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await dbContext.JobCategories.FirstOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw Missing("Job category", id);
        category.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReferenceDataItem>> GetSkillsAsync(CancellationToken cancellationToken) =>
        await dbContext.Skills.AsNoTracking().Where(skill => skill.IsActive)
            .OrderBy(skill => skill.Name).Select(skill => Map(skill)).ToListAsync(cancellationToken);

    public async Task<ReferenceDataItem> GetSkillAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await dbContext.Skills.AsNoTracking().FirstOrDefaultAsync(skill => skill.Id == id && skill.IsActive, cancellationToken)
            ?? throw Missing("Skill", id));

    public async Task<ReferenceDataItem> CreateSkillAsync(NamedReferenceDataInput input, CancellationToken cancellationToken)
    {
        await ValidateAsync(namedValidator, input, cancellationToken);
        var normalizedName = Normalize(input.Name);
        if (await dbContext.Skills.AnyAsync(skill => skill.NormalizedName == normalizedName, cancellationToken))
            throw Duplicate("Skill", input.Name);
        var skill = Skill.Create(input.Name, input.Description);
        dbContext.Skills.Add(skill);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(skill);
    }

    public async Task<ReferenceDataItem> UpdateSkillAsync(Guid id, NamedReferenceDataInput input, CancellationToken cancellationToken)
    {
        await ValidateAsync(namedValidator, input, cancellationToken);
        var skill = await dbContext.Skills.FirstOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw Missing("Skill", id);
        if (await dbContext.Skills.AnyAsync(item => item.Id != id && item.NormalizedName == Normalize(input.Name), cancellationToken))
            throw Duplicate("Skill", input.Name);
        skill.Update(input.Name, input.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(skill);
    }

    public async Task DeactivateSkillAsync(Guid id, CancellationToken cancellationToken)
    {
        var skill = await dbContext.Skills.FirstOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw Missing("Skill", id);
        skill.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CountryItem>> GetCountriesAsync(CancellationToken cancellationToken) =>
        await dbContext.Countries.AsNoTracking().Where(country => country.IsActive)
            .OrderBy(country => country.Name).Select(country => Map(country)).ToListAsync(cancellationToken);

    public async Task<CountryItem> GetCountryAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await dbContext.Countries.AsNoTracking().FirstOrDefaultAsync(country => country.Id == id && country.IsActive, cancellationToken)
            ?? throw Missing("Country", id));

    public async Task<CountryItem> CreateCountryAsync(CountryInput input, CancellationToken cancellationToken)
    {
        await ValidateAsync(countryValidator, input, cancellationToken);
        var normalizedCode = Normalize(input.Code);
        if (await dbContext.Countries.AnyAsync(country => country.NormalizedCode == normalizedCode, cancellationToken))
            throw Duplicate("Country code", input.Code);
        var country = Country.Create(input.Name, input.Code);
        dbContext.Countries.Add(country);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(country);
    }

    public async Task<CountryItem> UpdateCountryAsync(Guid id, CountryInput input, CancellationToken cancellationToken)
    {
        await ValidateAsync(countryValidator, input, cancellationToken);
        var country = await dbContext.Countries.FirstOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw Missing("Country", id);
        if (await dbContext.Countries.AnyAsync(item => item.Id != id && item.NormalizedCode == Normalize(input.Code), cancellationToken))
            throw Duplicate("Country code", input.Code);
        country.Update(input.Name, input.Code);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(country);
    }

    public async Task DeactivateCountryAsync(Guid id, CancellationToken cancellationToken)
    {
        var country = await dbContext.Countries.FirstOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw Missing("Country", id);
        if (await dbContext.Provinces.AnyAsync(province => province.CountryId == id && province.IsActive, cancellationToken))
            throw new DomainException("A country with active provinces cannot be deactivated.");
        country.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProvinceItem>> GetProvincesAsync(Guid countryId, CancellationToken cancellationToken)
    {
        await EnsureCountryActiveAsync(countryId, cancellationToken);
        return await dbContext.Provinces.AsNoTracking().Where(province => province.CountryId == countryId && province.IsActive)
            .OrderBy(province => province.Name)
            .Select(province => new ProvinceItem(province.Id, province.CountryId, province.Country.Name, province.Name,
                province.IsActive, province.CreatedAt, province.UpdatedAt)).ToListAsync(cancellationToken);
    }

    public async Task<ProvinceItem> GetProvinceAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Provinces.AsNoTracking().Where(province => province.Id == id && province.IsActive)
            .Select(province => new ProvinceItem(province.Id, province.CountryId, province.Country.Name, province.Name,
                province.IsActive, province.CreatedAt, province.UpdatedAt)).FirstOrDefaultAsync(cancellationToken)
        ?? throw Missing("Province", id);

    public async Task<ProvinceItem> CreateProvinceAsync(ProvinceInput input, CancellationToken cancellationToken)
    {
        await ValidateAsync(provinceValidator, input, cancellationToken);
        await EnsureCountryActiveAsync(input.CountryId, cancellationToken);
        if (await dbContext.Provinces.AnyAsync(province => province.CountryId == input.CountryId && province.NormalizedName == Normalize(input.Name), cancellationToken))
            throw Duplicate("Province", input.Name);
        var province = Province.Create(input.CountryId, input.Name);
        dbContext.Provinces.Add(province);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetProvinceAsync(province.Id, cancellationToken);
    }

    public async Task<ProvinceItem> UpdateProvinceAsync(Guid id, ProvinceInput input, CancellationToken cancellationToken)
    {
        await ValidateAsync(provinceValidator, input, cancellationToken);
        var province = await dbContext.Provinces.FirstOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw Missing("Province", id);
        await EnsureCountryActiveAsync(input.CountryId, cancellationToken);
        if (await dbContext.Provinces.AnyAsync(item => item.Id != id && item.CountryId == input.CountryId && item.NormalizedName == Normalize(input.Name), cancellationToken))
            throw Duplicate("Province", input.Name);
        province.Update(input.CountryId, input.Name);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetProvinceAsync(id, cancellationToken);
    }

    public async Task DeactivateProvinceAsync(Guid id, CancellationToken cancellationToken)
    {
        var province = await dbContext.Provinces.FirstOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw Missing("Province", id);
        if (await dbContext.Cities.AnyAsync(city => city.ProvinceId == id && city.IsActive, cancellationToken))
            throw new DomainException("A province with active cities cannot be deactivated.");
        province.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CityItem>> GetCitiesAsync(Guid provinceId, CancellationToken cancellationToken)
    {
        await EnsureProvinceActiveAsync(provinceId, cancellationToken);
        return await dbContext.Cities.AsNoTracking().Include(city => city.Province).ThenInclude(province => province.Country)
            .Where(city => city.ProvinceId == provinceId && city.IsActive)
            .OrderBy(city => city.Name).Select(city => Map(city)).ToListAsync(cancellationToken);
    }

    public async Task<CityItem> GetCityAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Cities.AsNoTracking().Include(city => city.Province).ThenInclude(province => province.Country)
            .Where(city => city.Id == id && city.IsActive)
            .Select(city => Map(city)).FirstOrDefaultAsync(cancellationToken) ?? throw Missing("City", id);

    public async Task<CityItem> CreateCityAsync(CityInput input, CancellationToken cancellationToken)
    {
        await ValidateAsync(cityValidator, input, cancellationToken);
        await EnsureProvinceActiveAsync(input.ProvinceId, cancellationToken);
        if (await dbContext.Cities.AnyAsync(city => city.ProvinceId == input.ProvinceId && city.NormalizedName == Normalize(input.Name), cancellationToken))
            throw Duplicate("City", input.Name);
        var city = City.Create(input.ProvinceId, input.Name);
        dbContext.Cities.Add(city);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetCityAsync(city.Id, cancellationToken);
    }

    public async Task<CityItem> UpdateCityAsync(Guid id, CityInput input, CancellationToken cancellationToken)
    {
        await ValidateAsync(cityValidator, input, cancellationToken);
        var city = await dbContext.Cities.FirstOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw Missing("City", id);
        await EnsureProvinceActiveAsync(input.ProvinceId, cancellationToken);
        if (await dbContext.Cities.AnyAsync(item => item.Id != id && item.ProvinceId == input.ProvinceId && item.NormalizedName == Normalize(input.Name), cancellationToken))
            throw Duplicate("City", input.Name);
        city.Update(input.ProvinceId, input.Name);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetCityAsync(id, cancellationToken);
    }

    public async Task DeactivateCityAsync(Guid id, CancellationToken cancellationToken)
    {
        var city = await dbContext.Cities.FirstOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw Missing("City", id);
        city.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<LocationItem> GetLocationAsync(Guid id, CancellationToken cancellationToken)
    {
        var city = await dbContext.Cities.AsNoTracking().Where(item => item.Id == id && item.IsActive)
            .Select(item => new LocationItem(item.Id, item.Name, "City", item.ProvinceId, item.Province.Name,
                item.Province.CountryId, item.Province.Country.Name, item.IsActive)).FirstOrDefaultAsync(cancellationToken);
        if (city is not null) return city;

        var province = await dbContext.Provinces.AsNoTracking().Where(item => item.Id == id && item.IsActive)
            .Select(item => new LocationItem(item.Id, item.Name, "Province", item.CountryId, item.Country.Name,
                item.CountryId, item.Country.Name, item.IsActive)).FirstOrDefaultAsync(cancellationToken);
        if (province is not null) return province;

        var country = await dbContext.Countries.AsNoTracking().Where(item => item.Id == id && item.IsActive)
            .Select(item => new LocationItem(item.Id, item.Name, "Country", null, null, item.Id, item.Name, item.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
        return country ?? throw Missing("Location", id);
    }

    private async Task EnsureCountryActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await dbContext.Countries.AnyAsync(country => country.Id == id && country.IsActive, cancellationToken))
            throw InvalidParent("CountryId", "An active country is required.");
    }

    private async Task EnsureProvinceActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await dbContext.Provinces.AnyAsync(province => province.Id == id && province.IsActive && province.Country.IsActive, cancellationToken))
            throw InvalidParent("ProvinceId", "An active province with an active country is required.");
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T input, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(input, cancellationToken);
        if (!result.IsValid)
            throw new IranJob.SharedKernel.Exceptions.ValidationException(result.Errors.GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
    }

    private static ReferenceDataItem Map(JobCategory category) =>
        new(category.Id, category.Name, category.Description, category.IsActive, category.CreatedAt, category.UpdatedAt);

    private static ReferenceDataItem Map(Skill skill) =>
        new(skill.Id, skill.Name, skill.Description, skill.IsActive, skill.CreatedAt, skill.UpdatedAt);

    private static CountryItem Map(Country country) =>
        new(country.Id, country.Name, country.Code, country.IsActive, country.CreatedAt, country.UpdatedAt);

    private static CityItem Map(City city) => new(
        city.Id, city.ProvinceId, city.Province.Name, city.Province.CountryId, city.Province.Country.Name,
        city.Name, city.IsActive, city.CreatedAt, city.UpdatedAt);

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static NotFoundException Missing(string name, Guid id) => new($"{name} '{id}' was not found.");
    private static DomainException Duplicate(string name, string value) => new($"{name} '{value.Trim()}' already exists.");
    private static IranJob.SharedKernel.Exceptions.ValidationException InvalidParent(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
