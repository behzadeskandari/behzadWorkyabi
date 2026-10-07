namespace IranJob.Modules.ReferenceData.Domain.Entities;

public sealed class Province
{
    private Province() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CountryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Country Country { get; private set; } = null!;

    public static Province Create(Guid countryId, string name) => new()
    {
        CountryId = countryId,
        Name = name.Trim(),
        NormalizedName = Normalize(name)
    };

    public void Update(Guid countryId, string name)
    {
        CountryId = countryId;
        Name = name.Trim();
        NormalizedName = Normalize(name);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
