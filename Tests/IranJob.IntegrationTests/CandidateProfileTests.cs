using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IranJob.IntegrationTests;

public class CandidateProfileTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string ProfileEndpoint = "/api/v1/candidates/profile";

    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public CandidateProfileTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateProfile_WithAuthenticatedUser_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync("candidate-create@test.ir");

        var response = await client.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetProfile_WithExistingProfile_ReturnsOwnProfile()
    {
        var client = await CreateAuthenticatedClientAsync("candidate-get@test.ir");
        await client.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        var response = await client.GetAsync(ProfileEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        payload.GetProperty("headline").GetString().Should().Be("Senior .NET Developer");
        payload.GetProperty("salaryType").GetString().Should().Be("Monthly");
    }

    [Fact]
    public async Task GetProfile_WithoutProfile_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync("candidate-empty@test.ir");

        var response = await client.GetAsync(ProfileEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProfile_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(ProfileEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProfile_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProfile_Twice_ReturnsConflict()
    {
        var client = await CreateAuthenticatedClientAsync("candidate-duplicate@test.ir");
        await client.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        var response = await client.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateProfile_WithExistingProfile_ReturnsUpdatedProfile()
    {
        var client = await CreateAuthenticatedClientAsync("candidate-update@test.ir");
        await client.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        var update = ValidProfileRequest() with { Headline = "Updated Headline", ExpectedSalary = 90_000_000m };
        var response = await client.PutAsJsonAsync(ProfileEndpoint, update, _json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await client.GetAsync(ProfileEndpoint);
        var payload = await fetched.Content.ReadFromJsonAsync<JsonElement>(_json);
        payload.GetProperty("headline").GetString().Should().Be("Updated Headline");
        payload.GetProperty("expectedSalary").GetDecimal().Should().Be(90_000_000m);
    }

    [Fact]
    public async Task UpdateProfile_WithoutProfile_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync("candidate-update-missing@test.ir");

        var response = await client.PutAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProfile_WithInvalidData_ReturnsValidationProblem()
    {
        var client = await CreateAuthenticatedClientAsync("candidate-invalid@test.ir");

        var invalid = ValidProfileRequest() with
        {
            Phone = "12345",
            LinkedInUrl = "not-a-url",
            ExpectedSalary = -5m
        };

        var response = await client.PostAsJsonAsync(ProfileEndpoint, invalid, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Profile_OwnershipCannotBeCrossedBetweenUsers()
    {
        var clientA = await CreateAuthenticatedClientAsync("candidate-owner@test.ir");
        await clientA.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        var clientB = await CreateAuthenticatedClientAsync("candidate-other@test.ir");

        var getResponse = await clientB.GetAsync(ProfileEndpoint);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var putResponse = await clientB.PutAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);
        putResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownerResponse = await clientA.GetAsync(ProfileEndpoint);
        var payload = await ownerResponse.Content.ReadFromJsonAsync<JsonElement>(_json);
        payload.GetProperty("headline").GetString().Should().Be("Senior .NET Developer");
    }

    [Fact]
    public async Task CreateProfile_IgnoresUserIdFromRequestBody()
    {
        var client = await CreateAuthenticatedClientAsync("candidate-ownership@test.ir");

        var body = new Dictionary<string, object?>
        {
            ["userId"] = Guid.NewGuid().ToString(),
            ["headline"] = "Body UserId Ignored"
        };

        var response = await client.PostAsJsonAsync(ProfileEndpoint, body, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var fetched = await client.GetAsync(ProfileEndpoint);
        var payload = await fetched.Content.ReadFromJsonAsync<JsonElement>(_json);
        payload.GetProperty("headline").GetString().Should().Be("Body UserId Ignored");
    }

    private sealed record ProfileRequest(
        string Headline,
        string Biography,
        string DateOfBirth,
        string Gender,
        string City,
        string Province,
        string Phone,
        string Email,
        string LinkedInUrl,
        string GitHubUrl,
        string PortfolioUrl,
        decimal ExpectedSalary,
        string SalaryType,
        string EmploymentStatus,
        string Availability,
        string MilitaryStatus);

    private static ProfileRequest ValidProfileRequest() => new(
        Headline: "Senior .NET Developer",
        Biography: "Experienced backend developer.",
        DateOfBirth: "1995-08-20",
        Gender: "Male",
        City: "Tehran",
        Province: "Tehran",
        Phone: "09121234567",
        Email: "profile-contact@test.ir",
        LinkedInUrl: "https://linkedin.com/in/test",
        GitHubUrl: "https://github.com/test",
        PortfolioUrl: "https://test.dev",
        ExpectedSalary: 75_000_000m,
        SalaryType: "Monthly",
        EmploymentStatus: "Employed",
        Availability: "WithinOneMonth",
        MilitaryStatus: "Completed");

    private static int _phoneNumberCounter;

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        await _factory.InitializeDatabaseAsync();

        var client = _factory.CreateClient();

        var registerRequest = new
        {
            firstName = "Test",
            lastName = "Candidate",
            email,
            // Phone numbers are unique per user; generate a deterministic, non-colliding value.
            phoneNumber = $"0912{Interlocked.Increment(ref _phoneNumberCounter):D7}",
            password = "Passw0rd!",
            role = "Candidate"
        };

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest, _json);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { identifier = email, password = "Passw0rd!" },
            _json);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(_json);
        var accessToken = loginPayload.GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
