namespace IranJob.Modules.ReferenceData.Application.Contracts;

public sealed record NamedReferenceDataInput(string Name, string? Description);
public sealed record CountryInput(string Name, string Code);
public sealed record ProvinceInput(Guid CountryId, string Name);
public sealed record CityInput(Guid ProvinceId, string Name);

public sealed record ReferenceDataItem(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CountryItem(
    Guid Id,
    string Name,
    string Code,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ProvinceItem(
    Guid Id,
    Guid CountryId,
    string CountryName,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CityItem(
    Guid Id,
    Guid ProvinceId,
    string ProvinceName,
    Guid CountryId,
    string CountryName,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record LocationItem(
    Guid Id,
    string Name,
    string Type,
    Guid? ParentId,
    string? ParentName,
    Guid? CountryId,
    string? CountryName,
    bool IsActive);