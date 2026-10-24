using System.Net;
using System.Text;
using FluentAssertions;
using IranJob.Modules.Payments.Application.Contracts;
using IranJob.Modules.Payments.Infrastructure.Configuration;
using IranJob.Modules.Payments.Infrastructure.Integrations.ZarinPal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IranJob.IntegrationTests;

public sealed class ZarinPalClientTests
{
    [Fact]
    public async Task RequestParsesV4DataEnvelope()
    {
        var client = CreateClient("""
            {"data":{"code":100,"message":"Success","authority":"authority-value"},"errors":[]}
            """);

        var result = await client.RequestAsync(
            new ZarinPalRequest("merchant", 1000, "publish", "https://jobs.example/return", "IRR"),
            CancellationToken.None);

        result.Code.Should().Be(100);
        result.Authority.Should().Be("authority-value");
    }

    [Fact]
    public async Task VerifyParsesNumericReferenceIdFromV4DataEnvelope()
    {
        var client = CreateClient("""
            {"data":{"code":100,"message":"Paid","ref_id":12345678},"errors":[]}
            """);

        var result = await client.VerifyAsync(
            new ZarinPalVerifyRequest("merchant", 1000, "authority-value", "IRR"),
            CancellationToken.None);

        result.Code.Should().Be(100);
        result.ReferenceId.Should().Be("12345678");
    }

    [Fact]
    public async Task SandboxRequestUsesSandboxEndpoint()
    {
        var handler = new StubResponseHandler("""
            {"data":{"code":100,"message":"Success","authority":"sandbox-authority"},"errors":[]}
            """);
        var client = CreateClient(handler, new ZarinPalOptions
        {
            Sandbox = true,
            RequestUrl = "https://live.example/request",
            SandboxRequestUrl = "https://sandbox.example/request"
        });

        await client.RequestAsync(
            new ZarinPalRequest("merchant", 1000, "publish", "https://jobs.example/return", "IRR"),
            CancellationToken.None);

        handler.LastRequestUri.Should().Be(new Uri("https://sandbox.example/request"));
    }

    [Fact]
    public async Task SandboxVerificationUsesSandboxEndpoint()
    {
        var handler = new StubResponseHandler("""
            {"data":{"code":100,"message":"Paid","ref_id":12345678},"errors":[]}
            """);
        var client = CreateClient(handler, new ZarinPalOptions
        {
            Sandbox = true,
            VerifyUrl = "https://live.example/verify",
            SandboxVerifyUrl = "https://sandbox.example/verify"
        });

        await client.VerifyAsync(
            new ZarinPalVerifyRequest("merchant", 1000, "authority-value", "IRR"),
            CancellationToken.None);

        handler.LastRequestUri.Should().Be(new Uri("https://sandbox.example/verify"));
    }

    private static ZarinPalClient CreateClient(string responseBody)
    {
        var handler = new StubResponseHandler(responseBody);
        return CreateClient(handler, new ZarinPalOptions
        {
            MerchantId = "test-merchant",
            Sandbox = false,
            RequestUrl = "https://gateway.invalid/request",
            VerifyUrl = "https://gateway.invalid/verify"
        });
    }

    private static ZarinPalClient CreateClient(StubResponseHandler handler, ZarinPalOptions configuredOptions)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(configuredOptions);
        return new ZarinPalClient(httpClient, options, NullLogger<ZarinPalClient>.Instance);
    }

    private sealed class StubResponseHandler(string body) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
