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

        var requestUrl = _options.Sandbox ? _options.SandboxRequestUrl : _options.RequestUrl;
        var response = await httpClient.PostAsJsonAsync(requestUrl, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken)
            ?? throw new InvalidDataException("Payment provider returned an empty response.");
        var root = document.RootElement;
        var data = GetObject(root, "data");
        var code = ReadCode(data, root);
        var message = ReadMessage(data, root);
        var authority = string.Empty;
        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("authority", out var authorityElement))
        {
            authority = authorityElement.ToString();
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

        var verifyUrl = _options.Sandbox ? _options.SandboxVerifyUrl : _options.VerifyUrl;
        var response = await httpClient.PostAsJsonAsync(verifyUrl, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken)
            ?? throw new InvalidDataException("Payment provider returned an empty response.");
        var root = document.RootElement;
        var data = GetObject(root, "data");
        var code = ReadCode(data, root);
        var message = ReadMessage(data, root);
        var referenceId = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("ref_id", out var referenceElement)
            ? referenceElement.ToString()
            : null;

        logger.LogInformation("ZarinPal verification returned code {Code}.", code);
        return new ZarinPalVerifyResult(code, message, referenceId);
    }

    private static JsonElement GetObject(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Object
            ? property
            : default;

    private static int ReadCode(JsonElement data, JsonElement root)
    {
        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("code", out var code))
            return ParseCode(code);
        if (root.TryGetProperty("code", out var rootCode))
            return ParseCode(rootCode);
        if (root.TryGetProperty("errors", out var errors)
            && errors.ValueKind == JsonValueKind.Array
            && errors.GetArrayLength() > 0
            && errors[0].ValueKind == JsonValueKind.Object
            && errors[0].TryGetProperty("code", out var errorCode))
            return ParseCode(errorCode);
        return -1;
    }

    private static int ParseCode(JsonElement code) =>
        code.ValueKind == JsonValueKind.Number && code.TryGetInt32(out var numeric)
            ? numeric
            : int.TryParse(code.ToString(), out var parsed) ? parsed : -1;

    private static string ReadMessage(JsonElement data, JsonElement root)
    {
        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("message", out var dataMessage))
            return dataMessage.ToString();
        return root.TryGetProperty("message", out var rootMessage) ? rootMessage.ToString() : string.Empty;
    }
}
