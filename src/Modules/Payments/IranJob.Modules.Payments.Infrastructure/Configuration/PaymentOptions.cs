namespace IranJob.Modules.Payments.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>Payment</c> configuration section.
/// </summary>
public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    public ZarinPalOptions ZarinPal { get; set; } = new();

    /// <summary>
    /// Fee charged to an employer for publishing a single job posting, expressed in the
    /// smallest currency unit (Rial for IRR). <c>0</c> (the test default) means publishing
    /// is free and proceeds without a payment gateway round-trip.
    /// </summary>
    public long PublishFeeAmount { get; set; }

    public string PublishFeeCurrency { get; set; } = "IRR";

    public string PublishFeeDescription { get; set; } = "Job posting publication fee";
}

public sealed class ZarinPalOptions
{
    public const string SectionName = "ZarinPal";

    public string MerchantId { get; set; } = string.Empty;

    /// <summary>When true the sandbox endpoints/start URL are used.</summary>
    public bool Sandbox { get; set; } = true;

    /// <summary>URL ZarinPal redirects the payer's browser to after payment (Angular callback page).</summary>
    public string CallbackUrl { get; set; } = string.Empty;

    public string RequestUrl { get; set; } = "https://api.zarinpal.com/pg/v4/payment/request.json";

    public string VerifyUrl { get; set; } = "https://api.zarinpal.com/pg/v4/payment/verify.json";

    public string SandboxRequestUrl { get; set; } = "https://sandbox.zarinpal.com/pg/v4/payment/request.json";

    public string SandboxVerifyUrl { get; set; } = "https://sandbox.zarinpal.com/pg/v4/payment/verify.json";

    public string StartPaySandboxUrl { get; set; } = "https://sandbox.zarinpal.com/pg/StartPay/{0}";

    public string StartPayUrl { get; set; } = "https://www.zarinpal.com/pg/StartPay/{0}";
}
