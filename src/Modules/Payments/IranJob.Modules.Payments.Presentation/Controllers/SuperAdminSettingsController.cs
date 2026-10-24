using Asp.Versioning;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.Payments.Application.Abstractions;
using IranJob.Modules.Payments.Infrastructure.Persistence;
using IranJob.Modules.Payments.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.Payments.Presentation.Controllers;

/// <summary>
/// Super Admin settings management endpoints. Provides read and update of
/// non-secret runtime settings such as the payment publishing fee.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/super-admin/settings")]
[Authorize(Policy = PolicyNames.SuperAdminOnly)]
public sealed class SuperAdminSettingsController(
    PaymentDbContext dbContext,
    ISettingsService settingsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SettingsResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        var all = await settingsService.GetAllAsync(cancellationToken);
        var (amount, currency) = await settingsService.GetPaymentFeeAsync(cancellationToken);
        var (emailEnabled, fromAddress) = await settingsService.GetEmailNotificationsAsync(cancellationToken);
        var (appName, appVersion) = await settingsService.GetAppInfoAsync(cancellationToken);

        return Ok(new SettingsResponse(
            new SettingsSection("payment_fee", "Job publication payment fee",
                "The fee (in smallest currency unit, e.g. rial) charged to employers when they publish a paid job posting. Set to 0 to disable paid publishing (free publishing).",
                "settings", amount, currency),
            new SettingsSection("email_notifications", "Email notifications",
                "Controls whether the platform sends email notifications to candidates and employers.",
                "settings", emailEnabled, fromAddress),
            new SettingsSection("app_info", "Application information",
                "Read-only application metadata (name and version).",
                "deployment_config", appName, appVersion),
            all));
    }

    [HttpPut("payment-fee")]
    [ProducesResponseType(typeof(PaymentFeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaymentFeeResponse>> UpdatePaymentFeeAsync(
        [FromBody] UpdatePaymentFeeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount < 0)
            return BadRequest(new ErrorResponse("Payment fee amount cannot be negative."));

        var (savedAmount, savedCurrency) = await settingsService.UpdatePaymentFeeAsync(request.Amount, request.Currency, cancellationToken);

        return Ok(new PaymentFeeResponse(savedAmount, savedCurrency));
    }

    [HttpPut("email-notifications")]
    [ProducesResponseType(typeof(EmailNotificationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmailNotificationsResponse>> UpdateEmailNotificationsAsync(
        [FromBody] UpdateEmailNotificationsRequest request,
        CancellationToken cancellationToken = default)
    {
        await settingsService.UpdateEmailNotificationsAsync(request.Enabled, request.FromAddress, cancellationToken);
        return Ok(new EmailNotificationsResponse(request.Enabled, request.FromAddress));
    }

    [HttpPut("app-info")]
    [ProducesResponseType(typeof(AppInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AppInfoResponse>> UpdateAppInfoAsync(
        [FromBody] UpdateAppInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ErrorResponse("Application name is required."));
        if (string.IsNullOrWhiteSpace(request.Version))
            return BadRequest(new ErrorResponse("Application version is required."));

        await settingsService.UpdateAppInfoAsync(request.Name, request.Version, cancellationToken);
        return Ok(new AppInfoResponse(request.Name, request.Version));
    }
}


/// <summary>
/// Request to update the payment fee.
/// </summary>
public sealed record UpdatePaymentFeeRequest(long Amount, string Currency);

/// <summary>
/// Response after a successful payment fee update.
/// </summary>
public sealed record PaymentFeeResponse(long Amount, string Currency);

/// <summary>
/// Request to update email notification settings.
/// </summary>
public sealed record UpdateEmailNotificationsRequest(bool Enabled, string FromAddress);

/// <summary>
/// Response after a successful email notification update.
/// </summary>
public sealed record EmailNotificationsResponse(bool Enabled, string FromAddress);

/// <summary>
/// Request to update application information.
/// </summary>
public sealed record UpdateAppInfoRequest(string Name, string Version);

/// <summary>
/// Response after a successful app info update.
/// </summary>
public sealed record AppInfoResponse(string Name, string Version);

/// <summary>
/// Aggregated settings response for the Super Admin UI.
/// </summary>
public sealed record SettingsResponse(
    SettingsSection PaymentFee,
    SettingsSection EmailNotifications,
    SettingsSection AppInfo,
    Dictionary<string, string> All);


public sealed record ErrorResponse(string message);
/// <summary>
/// A single settings section with metadata and current value.
/// </summary>
public sealed record SettingsSection(
    string Key,
    string Label,
    string Description,
    string Source,
    object Value,
    string appVersion);

/// <summary>
/// Settings source metadata.
/// </summary>
public sealed record SettingsSource(string Key, string Name);


