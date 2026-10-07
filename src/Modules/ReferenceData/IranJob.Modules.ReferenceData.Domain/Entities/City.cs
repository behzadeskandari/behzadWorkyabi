namespace IranJob.Modules.ReferenceData.Domain.Entities;

public sealed class City
{
    private City() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProvinceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Province Province { get; private set; } = null!;

    public static City Create(Guid provinceId, string name) => new()
    {
        ProvinceId = provinceId,
        Name = name.Trim(),
        NormalizedName = Normalize(name)
    };

    public void Update(Guid provinceId, string name)
    {
        ProvinceId = provinceId;
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
