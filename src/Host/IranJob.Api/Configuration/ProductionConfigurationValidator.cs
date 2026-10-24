using IranJob.Modules.Payments.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace IranJob.Api.Configuration;

public static class ProductionConfigurationValidator
{
    public static void Validate(IConfiguration configuration, string environmentName)
    {
        if (environmentName != "Production")
            return;

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString)
            || connectionString.Contains("123456", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Your_strong_Password", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production requires a configured database connection string; supply ConnectionStrings:DefaultConnection through deployment secrets.");

        var signingKey = configuration["Authentication:Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(signingKey)
            || Encoding.UTF8.GetByteCount(signingKey) < 32
            || signingKey.Contains("DEV_ONLY", StringComparison.OrdinalIgnoreCase)
            || signingKey.Contains("TEST_ONLY", StringComparison.OrdinalIgnoreCase)
            || signingKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production requires a non-placeholder JWT signing key of at least 32 UTF-8 bytes; supply Authentication:Jwt:SecretKey through deployment secrets.");

        _ = FrontendCors.GetAllowedOrigins(configuration, environmentName);

        var database = configuration.GetSection("Database");
        if (database.GetValue<bool>("ApplyMigrationsOnStartup"))
            throw new InvalidOperationException("Production startup migrations are disabled; apply migrations through the controlled deployment step.");

        if (!configuration.GetSection("Payment:PublishFeeAmount").Exists()
            || !long.TryParse(configuration["Payment:PublishFeeAmount"], out var fee)
            || fee < 0)
            throw new InvalidOperationException("Production requires an explicit non-negative Payment:PublishFeeAmount setting.");

        var currency = configuration["Payment:PublishFeeCurrency"] ?? "IRR";
        if (!Enum.TryParse<Currency>(currency, true, out _))
            throw new InvalidOperationException("Payment:PublishFeeCurrency must be a supported currency code.");

        if (fee > 0)
        {
            var payment = configuration.GetSection("Payment:ZarinPal");
            var merchantId = payment["MerchantId"];
            var callbackUrl = payment["CallbackUrl"];
            var sandboxValue = payment["Sandbox"];
            if (string.IsNullOrWhiteSpace(merchantId)
                || merchantId.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
                || !Uri.TryCreate(callbackUrl, UriKind.Absolute, out var callback)
                || callback.Scheme != Uri.UriSchemeHttps
                || !bool.TryParse(sandboxValue, out var sandbox))
                throw new InvalidOperationException("Paid publishing requires Payment:ZarinPal:MerchantId, an HTTPS Payment:ZarinPal:CallbackUrl, and an explicit Payment:ZarinPal:Sandbox setting.");

            if (sandbox)
            {
                RequireHttpsUrl(payment["SandboxRequestUrl"] ?? "https://sandbox.zarinpal.com/pg/v4/payment/request.json", "Payment:ZarinPal:SandboxRequestUrl");
                RequireHttpsUrl(payment["SandboxVerifyUrl"] ?? "https://sandbox.zarinpal.com/pg/v4/payment/verify.json", "Payment:ZarinPal:SandboxVerifyUrl");
                RequireHttpsUrl(payment["StartPaySandboxUrl"] ?? "https://sandbox.zarinpal.com/pg/StartPay/{0}", "Payment:ZarinPal:StartPaySandboxUrl");
            }
            else
            {
                RequireHttpsUrl(payment["RequestUrl"] ?? "https://api.zarinpal.com/pg/v4/payment/request.json", "Payment:ZarinPal:RequestUrl");
                RequireHttpsUrl(payment["VerifyUrl"] ?? "https://api.zarinpal.com/pg/v4/payment/verify.json", "Payment:ZarinPal:VerifyUrl");
                RequireHttpsUrl(payment["StartPayUrl"] ?? "https://www.zarinpal.com/pg/StartPay/{0}", "Payment:ZarinPal:StartPayUrl");
            }
        }
    }

    private static void RequireHttpsUrl(string value, string settingName)
    {
        var parsed = value.Replace("{0}", "authority", StringComparison.Ordinal);
        if (!Uri.TryCreate(parsed, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"{settingName} must be an absolute HTTPS URL.");
    }
}
