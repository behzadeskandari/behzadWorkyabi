using IranJob.Api.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace IranJob.IntegrationTests;

public sealed class ProductionConfigurationTests
{
    [Fact]
    public void DevelopmentDoesNotRequireProductionSecrets()
    {
        var configuration = new ConfigurationBuilder().Build();
        var action = () => ProductionConfigurationValidator.Validate(configuration, "Development");
        action.Should().NotThrow();
    }

    [Fact]
    public void ProductionRejectsMissingDatabaseConnectionWithoutEchoingValues()
    {
        var configuration = CreateProductionConfiguration();
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(configuration, "Production"));
        exception.Message.Should().Contain("ConnectionStrings:DefaultConnection");
        exception.Message.Should().NotContain("Password=");
    }

    [Fact]
    public void ProductionRejectsWildcardCorsOrigins()
    {
        var configuration = CreateProductionConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=local;Database=IranJob;Integrated Security=True",
            ["Authentication:Jwt:SecretKey"] = "integration-test-signing-key-long-enough-for-validation",
            ["Cors:AllowedOrigins:0"] = "https://*.example.test",
            ["Payment:PublishFeeAmount"] = "0",
            ["Database:ApplyMigrationsOnStartup"] = "false"
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(configuration, "Production"));
        exception.Message.Should().Contain("Cors:AllowedOrigins");
    }

    [Fact]
    public void ProductionRejectsStartupMigrations()
    {
        var configuration = CreateProductionConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=local;Database=IranJob;Integrated Security=True",
            ["Authentication:Jwt:SecretKey"] = "integration-test-signing-key-long-enough-for-validation",
            ["Cors:AllowedOrigins:0"] = "https://jobs.example.test",
            ["Payment:PublishFeeAmount"] = "0",
            ["Database:ApplyMigrationsOnStartup"] = "true"
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(configuration, "Production"));
        exception.Message.Should().Contain("controlled deployment step");
    }

    [Fact]
    public void ProductionSandboxRequiresHttpsSandboxEndpoints()
    {
        var configuration = CreateProductionConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=local;Database=IranJob;Integrated Security=True",
            ["Authentication:Jwt:SecretKey"] = "integration-test-signing-key-long-enough-for-validation",
            ["Cors:AllowedOrigins:0"] = "https://jobs.example.test",
            ["Payment:PublishFeeAmount"] = "1000",
            ["Payment:PublishFeeCurrency"] = "IRR",
            ["Payment:ZarinPal:MerchantId"] = "sandbox-merchant-id",
            ["Payment:ZarinPal:CallbackUrl"] = "https://jobs.example.test/employer/payments/return",
            ["Payment:ZarinPal:Sandbox"] = "true",
            ["Payment:ZarinPal:SandboxRequestUrl"] = "http://sandbox.example.test/request",
            ["Database:ApplyMigrationsOnStartup"] = "false"
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(configuration, "Production"));
        exception.Message.Should().Contain("Payment:ZarinPal:SandboxRequestUrl");
    }

    [Fact]
    public void ProductionRejectsInsecureSelectedProviderEndpointsInSandboxAndLiveModes()
    {
        var endpointOverrides = new[]
        {
            (Sandbox: true, Key: "Payment:ZarinPal:SandboxVerifyUrl"),
            (Sandbox: true, Key: "Payment:ZarinPal:StartPaySandboxUrl"),
            (Sandbox: false, Key: "Payment:ZarinPal:VerifyUrl"),
            (Sandbox: false, Key: "Payment:ZarinPal:StartPayUrl")
        };

        foreach (var (sandbox, key) in endpointOverrides)
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=local;Database=IranJob;Integrated Security=True",
                ["Authentication:Jwt:SecretKey"] = "integration-test-signing-key-long-enough-for-validation",
                ["Cors:AllowedOrigins:0"] = "https://jobs.example.test",
                ["Payment:PublishFeeAmount"] = "1000",
                ["Payment:PublishFeeCurrency"] = "IRR",
                ["Payment:ZarinPal:MerchantId"] = "integration-test-merchant",
                ["Payment:ZarinPal:CallbackUrl"] = "https://jobs.example.test/employer/payments/return",
                ["Payment:ZarinPal:Sandbox"] = sandbox.ToString(),
                [key] = "http://insecure.example.test/provider",
                ["Database:ApplyMigrationsOnStartup"] = "false"
            };
            var configuration = CreateProductionConfiguration(values);

            var exception = Assert.Throws<InvalidOperationException>(
                () => ProductionConfigurationValidator.Validate(configuration, "Production"));
            exception.Message.Should().Contain(key);
        }
    }

    private static IConfiguration CreateProductionConfiguration(IDictionary<string, string?>? values = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(values ?? new Dictionary<string, string?>()).Build();
}
