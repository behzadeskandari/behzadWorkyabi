using System.Net.Http.Json;
using System.Text.Json;
using IranJob.Modules.Payments.Application.Abstractions;
using IranJob.Modules.Payments.Application.Contracts;
using IranJob.Modules.Payments.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IranJob.Modules.Payments.Infrastructure.Integrations.ZarinPal;

/// <summary>
/// Concrete <see cref="IZarinPalClient"/> targeting the ZarinPal v4 (WebGate) REST API.
/// The exact endpoints are configuration-driven (<see cref="ZarinPalOptions"/>) so sandbox
/// and production are selected via configuration without code changes. In integration tests
/// this client is replaced by a stub; no real HTTP traffic occurs during tests.
/// </summary>
public sealed class ZarinPalClient(
    HttpClient httpClient,
    IOptions<ZarinPalOptions> options,
    ILogger<ZarinPalClient> logger) : IZarinPalClient
{
    private readonly ZarinPalOptions _options = options.Value;

    public async Task<ZarinPalRequestResult> RequestAsync(ZarinPalRequest request, CancellationToken cancellationToken)
    {
        var payload = new
        {
            merchant_id = _options.MerchantId,
            amount = request.Amount,
            description = request.Description,
            callback_url = request.CallbackUrl,
            currency = request.Currency,
            metadata = request.Metadata
        };

        var response = await httpClient.PostAsJsonAsync(_options.RequestUrl, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        var root = document.RootElement;
        var code = root.TryGetProperty("code", out var c) ? c.GetInt32() : -1;
        var message = root.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : string.Empty;
        var authority = string.Empty;
        if (root.TryGetProperty("data", out var data) && data.ValueKind is JsonValueKind.Object)
        {
            if (data.TryGetProperty("authority", out var a)) authority = a.GetString() ?? string.Empty;
        }

        logger.LogInformation("ZarinPal payment request returned code {Code}.", code);
        return new ZarinPalRequestResult(code, message, authority);
    }

    public async Task<ZarinPalVerifyResult> VerifyAsync(ZarinPalVerifyRequest request, CancellationToken cancellationToken)
    {
        var payload = new
        {
            merchant_id = _options.MerchantId,
            amount = request.Amount,
            authority = request.Authority,
            currency = request.Currency
        };

        var response = await httpClient.PostAsJsonAsync(_options.VerifyUrl, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        var root = document.RootElement;
        var code = root.TryGetProperty("code", out var c) ? c.GetInt32() : -1;
        var message = root.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : string.Empty;
        var referenceId = root.TryGetProperty("data", out var data) && data.TryGetProperty("ref_id", out var r)
            ? r.GetString()
            : null;

        logger.LogInformation("ZarinPal verification for authority {Authority} returned code {Code}.", request.Authority, code);
        return new ZarinPalVerifyResult(code, message, referenceId);
    }
}
