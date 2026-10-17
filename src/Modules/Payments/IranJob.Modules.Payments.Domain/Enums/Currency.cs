namespace IranJob.Modules.Payments.Domain.Enums;

/// <summary>
/// Supported settlement currencies. IRR is the Iranian Rial (smallest unit = rial);
/// IRT is the Iranian Toman (smallest unit = Toman). ZarinPal accepts both via the
/// v4 <c>currency</c> request field.
/// </summary>
public enum Currency
{
    IRR = 1,
    IRT = 2
}
