using IranJob.Modules.Payments.Application.Contracts;

namespace IranJob.Modules.Payments.Application.Abstractions;

/// <summary>
/// Abstraction over the ZarinPal v4 (WebGate) REST endpoints. Replaced with a
/// stub in integration tests so no real HTTP traffic occurs.
/// </summary>
public interface IZarinPalClient
{
    Task<ZarinPalRequestResult> RequestAsync(ZarinPalRequest request, CancellationToken cancellationToken);
    Task<ZarinPalVerifyResult> VerifyAsync(ZarinPalVerifyRequest request, CancellationToken cancellationToken);
}
