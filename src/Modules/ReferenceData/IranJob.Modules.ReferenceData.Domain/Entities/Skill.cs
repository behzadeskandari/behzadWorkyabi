namespace IranJob.Modules.ReferenceData.Domain.Entities;

public sealed class Skill
{
    private Skill() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Skill Create(string name, string? description) => new()
    {
        Name = name.Trim(),
        NormalizedName = Normalize(name),
        Description = NormalizeOptional(description)
    };

    public void Update(string name, string? description)
    {
        Name = name.Trim();
        NormalizedName = Normalize(name);
        Description = NormalizeOptional(description);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
