using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IranJob.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IranJob.IntegrationTests;

public class ApiStartupTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiStartupTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Application_StartsSuccessfully()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsResponse()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SystemInfoEndpoint_ReturnsApplicationMetadata()
    {
        var response = await _client.GetAsync("/api/v1/system/info");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<SystemInfoResponse>();

        payload.Should().NotBeNull();
        payload!.ApplicationName.Should().Be("IranJob");
        payload.Version.Should().Be("0.1.0");
        payload.Environment.Should().Be("Testing");
    }

    [Fact]
    public async Task CorrelationId_IsReturnedInResponseHeader()
    {
        var response = await _client.GetAsync("/api/v1/system/info");

        response.Headers.Should().ContainKey("X-Correlation-ID");
    }

    [Fact]
    public async Task GlobalExceptionHandler_ReturnsProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/diagnostics/throw");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task NotFoundException_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/diagnostics/not-found");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
