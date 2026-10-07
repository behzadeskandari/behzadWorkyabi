using IranJob.Modules.ReferenceData.Application.Contracts;

namespace IranJob.Modules.ReferenceData.Application.Abstractions;

public interface IReferenceDataService
{
    Task<IReadOnlyList<ReferenceDataItem>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<ReferenceDataItem> GetCategoryAsync(Guid id, CancellationToken cancellationToken);
    Task<ReferenceDataItem> CreateCategoryAsync(NamedReferenceDataInput input, CancellationToken cancellationToken);
    Task<ReferenceDataItem> UpdateCategoryAsync(Guid id, NamedReferenceDataInput input, CancellationToken cancellationToken);
    Task DeactivateCategoryAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReferenceDataItem>> GetSkillsAsync(CancellationToken cancellationToken);
    Task<ReferenceDataItem> GetSkillAsync(Guid id, CancellationToken cancellationToken);
    Task<ReferenceDataItem> CreateSkillAsync(NamedReferenceDataInput input, CancellationToken cancellationToken);
    Task<ReferenceDataItem> UpdateSkillAsync(Guid id, NamedReferenceDataInput input, CancellationToken cancellationToken);
    Task DeactivateSkillAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CountryItem>> GetCountriesAsync(CancellationToken cancellationToken);
    Task<CountryItem> GetCountryAsync(Guid id, CancellationToken cancellationToken);
    Task<CountryItem> CreateCountryAsync(CountryInput input, CancellationToken cancellationToken);
    Task<CountryItem> UpdateCountryAsync(Guid id, CountryInput input, CancellationToken cancellationToken);
    Task DeactivateCountryAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProvinceItem>> GetProvincesAsync(Guid countryId, CancellationToken cancellationToken);
    Task<ProvinceItem> GetProvinceAsync(Guid id, CancellationToken cancellationToken);
    Task<ProvinceItem> CreateProvinceAsync(ProvinceInput input, CancellationToken cancellationToken);
    Task<ProvinceItem> UpdateProvinceAsync(Guid id, ProvinceInput input, CancellationToken cancellationToken);
    Task DeactivateProvinceAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CityItem>> GetCitiesAsync(Guid provinceId, CancellationToken cancellationToken);
    Task<CityItem> GetCityAsync(Guid id, CancellationToken cancellationToken);
    Task<CityItem> CreateCityAsync(CityInput input, CancellationToken cancellationToken);
    Task<CityItem> UpdateCityAsync(Guid id, CityInput input, CancellationToken cancellationToken);
    Task DeactivateCityAsync(Guid id, CancellationToken cancellationToken);
    Task<LocationItem> GetLocationAsync(Guid id, CancellationToken cancellationToken);
}