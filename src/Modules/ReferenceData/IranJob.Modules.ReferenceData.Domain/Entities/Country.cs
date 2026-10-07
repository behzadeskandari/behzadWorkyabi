namespace IranJob.Modules.ReferenceData.Domain.Entities;

public sealed class Country
{
    private Country() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string NormalizedCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Country Create(string name, string code) => new()
    {
        Name = name.Trim(),
        NormalizedName = Normalize(name),
        Code = code.Trim().ToUpperInvariant(),
        NormalizedCode = code.Trim().ToUpperInvariant()
    };

    public void Update(string name, string code)
    {
        Name = name.Trim();
        NormalizedName = Normalize(name);
        Code = code.Trim().ToUpperInvariant();
        NormalizedCode = Code;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
