using IranJob.SharedKernel;
using IranJob.SharedKernel.Entities;
using IranJob.SharedKernel.Exceptions;

namespace IranJob.Modules.Payments.Domain.Settings;

/// <summary>
/// Persisted application settings. Only explicitly approved, non-secret settings
/// are stored here; deployment secrets (JWT keys, connection strings, provider
/// credentials, SMTP credentials) are never persisted by this module.
/// </summary>
public sealed class Settings : Entity
{
    public const string PaymentFeeSettingName = "Payment.Fee.Amount";

    public static Settings Create(
        long paymentFeeAmount,
        string paymentFeeCurrency,
        bool emailNotificationsEnabled,
        string emailFromAddress,
        string appName,
        string appVersion)
    {
        return new Settings
        {
            Name = PaymentFeeSettingName,
            Value = $"{paymentFeeAmount}|{paymentFeeCurrency}|{emailNotificationsEnabled}|{emailFromAddress}|{appName}|{appVersion}"
        };
    }

    public static Settings CreateDefault()
    {
        return new Settings
        {
            Name = PaymentFeeSettingName,
            Value = "0|IRR|true|no-reply@iranjob.local|IranJob|1.0.0"
        };
    }

    private Settings() { }

    public string Name { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    public void UpdatePaymentFee(long amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Payment fee amount cannot be negative.");

        var parts = Value.Split('|');
        if (parts.Length < 2)
            throw new DomainException("Settings payload is malformed.");

        Value = $"{amount}|{currency}|{parts[2]}|{parts[3]}|{parts[4]}|{parts[5]}";
    }

    public void UpdateEmailNotifications(bool enabled, string fromAddress)
    {
        var parts = Value.Split('|');
        if (parts.Length < 6)
            throw new DomainException("Settings payload is malformed.");

        Value = $"{parts[0]}|{parts[1]}|{enabled.ToString().ToLowerInvariant()}|{fromAddress}|{parts[4]}|{parts[5]}";
    }

    public void UpdateAppInfo(string name, string version)
    {
        var parts = Value.Split('|');
        if (parts.Length < 6)
            throw new DomainException("Settings payload is malformed.");

        Value = $"{parts[0]}|{parts[1]}|{parts[2]}|{parts[3]}|{name}|{version}";
    }

    public long GetPaymentFeeAmount()
    {
        var parts = Value.Split('|');
        if (parts.Length < 2 || !long.TryParse(parts[0], out var amount))
            return 0;
        return amount;
    }

    public string GetPaymentFeeCurrency() => Value.Split('|').Length > 1 ? Value.Split('|')[1] : "IRR";

    public bool GetEmailNotificationsEnabled() => Value.Split('|').Length > 2 && Value.Split('|')[2] == bool.TrueString;

    public string GetEmailFromAddress() => Value.Split('|').Length > 3 ? Value.Split('|')[3] : "no-reply@iranjob.local";

    public string GetAppName() => Value.Split('|').Length > 4 ? Value.Split('|')[4] : "IranJob";

    public string GetAppVersion() => Value.Split('|').Length > 5 ? Value.Split('|')[5] : "1.0.0";
}
