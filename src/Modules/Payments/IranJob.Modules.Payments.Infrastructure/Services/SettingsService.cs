using IranJob.Modules.Payments.Application.Abstractions;
using IranJob.Modules.Payments.Domain.Settings;
using IranJob.Modules.Payments.Infrastructure.Persistence;
using IranJob.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Runtime-managed, non-secret application settings backed by the <c>settings</c>
/// table. Updates are optimistic-concurrency-protected via RowVersion and
/// wrapped in a transaction. Secrets are never stored or returned.
/// </summary>
public sealed class SettingsService(PaymentDbContext dbContext, ILockService lockService) : ISettingsService
{
    private static readonly string ConcurrentUpdateMessage = "Another update is in progress for these settings. Please retry.";

    public async Task<Dictionary<string, string>> GetAllAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.Settings
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return settings.ToDictionary(s => s.Name, s => s.Value, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<(long Amount, string Currency)> GetPaymentFeeAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.Settings
            .AsNoTracking()
            .Where(s => s.Name == Settings.PaymentFeeSettingName)
            .ToListAsync(cancellationToken);

        if (settings.Count == 0)
            return (0, "IRR");

        return (settings[0].GetPaymentFeeAmount(), settings[0].GetPaymentFeeCurrency());
    }

    public async Task<(long Amount, string Currency)> UpdatePaymentFeeAsync(long amount, string currency, CancellationToken cancellationToken)
    {
        var (currentAmount, currentCurrency) = await GetPaymentFeeAsync(cancellationToken);
        if (amount == currentAmount && string.Equals(currency, currentCurrency, StringComparison.OrdinalIgnoreCase))
            return (currentAmount, currentCurrency);

        var row = await dbContext.Settings
            .Where(s => s.Name == Settings.PaymentFeeSettingName)
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
            throw new NotFoundException($"Setting '{Settings.PaymentFeeSettingName}' not found.");

        row.UpdatePaymentFee(amount, currency);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (row.GetPaymentFeeAmount(), row.GetPaymentFeeCurrency());
    }

    public async Task<(bool Enabled, string FromAddress)> GetEmailNotificationsAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.Settings
            .AsNoTracking()
            .Where(s => s.Name == Settings.PaymentFeeSettingName)
            .ToListAsync(cancellationToken);

        if (settings.Count == 0)
            return (true, "no-reply@iranjob.local");

        return (settings[0].GetEmailNotificationsEnabled(), settings[0].GetEmailFromAddress());
    }

    public async Task UpdateEmailNotificationsAsync(bool enabled, string fromAddress, CancellationToken cancellationToken)
    {
        var row = await dbContext.Settings
            .Where(s => s.Name == Settings.PaymentFeeSettingName)
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
            throw new NotFoundException($"Setting '{Settings.PaymentFeeSettingName}' not found.");

        row.UpdateEmailNotifications(enabled, fromAddress);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(string Name, string Version)> GetAppInfoAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.Settings
            .AsNoTracking()
            .Where(s => s.Name == Settings.PaymentFeeSettingName)
            .ToListAsync(cancellationToken);

        if (settings.Count == 0)
            return ("IranJob", "1.0.0");

        return (settings[0].GetAppName(), settings[0].GetAppVersion());
    }

    public async Task UpdateAppInfoAsync(string name, string version, CancellationToken cancellationToken)
    {
        var row = await dbContext.Settings
            .Where(s => s.Name == Settings.PaymentFeeSettingName)
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
            throw new NotFoundException($"Setting '{Settings.PaymentFeeSettingName}' not found.");

        row.UpdateAppInfo(name, version);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
