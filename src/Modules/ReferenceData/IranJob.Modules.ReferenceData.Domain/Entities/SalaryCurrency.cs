namespace IranJob.Modules.ReferenceData.Domain.Entities;

public sealed class SalaryCurrency
{
    private SalaryCurrency() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; } = string.Empty;
    public string NormalizedCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string? Symbol { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static SalaryCurrency Create(string code, string name, string? symbol) => new()
    {
        Code = code.Trim().ToUpperInvariant(),
        NormalizedCode = code.Trim().ToUpperInvariant(),
        Name = name.Trim(),
        NormalizedName = name.Trim().ToUpperInvariant(),
        Symbol = NormalizeOptional(symbol)
    };

    public void Update(string name, string? symbol)
    {
        Name = name.Trim();
        NormalizedName = name.Trim().ToUpperInvariant();
        Symbol = NormalizeOptional(symbol);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}