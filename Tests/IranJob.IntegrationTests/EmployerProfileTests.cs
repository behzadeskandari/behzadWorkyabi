using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace IranJob.IntegrationTests;

public class EmployerProfileTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string ProfileEndpoint = "/api/v1/employers/profile";

    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public EmployerProfileTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateProfile_WithAuthenticatedUser_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync("employer-create@test.ir");

        var response = await client.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetProfile_WithExistingProfile_ReturnsOwnProfile()
    {
        var client = await CreateAuthenticatedClientAsync("employer-get@test.ir");
        await client.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        var response = await client.GetAsync(ProfileEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        payload.GetProperty("companyName").GetString().Should().Be("Acme Digital");
        payload.GetProperty("industry").GetString().Should().Be("Technology");
    }

    [Fact]
    public async Task GetProfile_WithoutProfile_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync("employer-empty@test.ir");

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
    public async Task UpdateProfile_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProfile_WithInvalidData_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync("employer-validation@test.ir");

        var invalidRequest = new Dictionary<string, object?>
        {
            ["companyName"] = string.Empty,
            ["industry"] = string.Empty,
            ["companySize"] = "11-50",
            ["city"] = "Tehran",
            ["province"] = "Tehran"
        };

        var response = await client.PostAsJsonAsync(ProfileEndpoint, invalidRequest, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProfile_Twice_ReturnsConflict()
    {
        var client = await CreateAuthenticatedClientAsync("employer-duplicate@test.ir");
        await client.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        var response = await client.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateProfile_WithExistingProfile_ReturnsUpdatedProfile()
    {
        var client = await CreateAuthenticatedClientAsync("employer-update@test.ir");
        await client.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        var update = ValidProfileRequest() with { CompanyName = "Acme Digital Labs", CompanySize = "51-200" };
        var response = await client.PutAsJsonAsync(ProfileEndpoint, update, _json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await client.GetAsync(ProfileEndpoint);
        var payload = await fetched.Content.ReadFromJsonAsync<JsonElement>(_json);
        payload.GetProperty("companyName").GetString().Should().Be("Acme Digital Labs");
        payload.GetProperty("companySize").GetString().Should().Be("51-200");
    }

    [Fact]
    public async Task Profile_OwnershipCannotBeCrossedBetweenUsers()
    {
        var clientA = await CreateAuthenticatedClientAsync("employer-owner@test.ir");
        await clientA.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);

        var clientB = await CreateAuthenticatedClientAsync("employer-other@test.ir");

        var getResponse = await clientB.GetAsync(ProfileEndpoint);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var putResponse = await clientB.PutAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);
        putResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProfile_IgnoresUserIdFromRequestBody()
    {
        var client = await CreateAuthenticatedClientAsync("employer-ownership@test.ir");

        var body = new Dictionary<string, object?>
        {
            ["userId"] = Guid.NewGuid().ToString(),
            ["companyName"] = "Body UserId Ignored",
            ["industry"] = "Technology",
            ["companySize"] = "11-50",
            ["city"] = "Tehran",
            ["province"] = "Tehran",
            ["companyDescription"] = "Ignored user id but valid profile payload.",
            ["websiteUrl"] = "https://example.com",
            ["linkedinUrl"] = "https://linkedin.com/company/ignored-user-id",
            ["logoUrl"] = "https://example.com/logo.png",
            ["foundedYear"] = 2020,
            ["address"] = "Tehran, Iran",
            ["postalCode"] = "12345",
            ["contactEmail"] = "hello@example.com",
            ["contactPhone"] = "+989121234567"
        };

        var response = await client.PostAsJsonAsync(ProfileEndpoint, body, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var fetched = await client.GetAsync(ProfileEndpoint);
        var payload = await fetched.Content.ReadFromJsonAsync<JsonElement>(_json);
        payload.GetProperty("companyName").GetString().Should().Be("Body UserId Ignored");
    }

    private sealed record ProfileRequest(
        string CompanyName,
        string Industry,
        string CompanySize,
        string City,
        string Province,
        string? CompanyDescription,
        string? WebsiteUrl,
        string? LinkedInUrl,
        string? LogoUrl,
        int? FoundedYear,
        string? Address,
        string? PostalCode,
        string? ContactEmail,
        string? ContactPhone);

    private static ProfileRequest ValidProfileRequest() => new(
        CompanyName: "Acme Digital",
        Industry: "Technology",
        CompanySize: "11-50",
        City: "Tehran",
        Province: "Tehran",
        CompanyDescription: "Modern digital product company.",
        WebsiteUrl: "https://acmedigital.ir",
        LinkedInUrl: "https://linkedin.com/company/acme-digital",
        LogoUrl: "https://cdn.example.com/logo.png",
        FoundedYear: 2018,
        Address: "Tehran, Iran",
        PostalCode: "1234567890",
        ContactEmail: "hello@acmedigital.ir",
        ContactPhone: "+989121234567");

    private static int _phoneNumberCounter;

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        await _factory.InitializeDatabaseAsync();

        var client = _factory.CreateClient();

        var registerRequest = new
        {
            firstName = "Test",
            lastName = "Employer",
            email,
            phoneNumber = $"0912{Interlocked.Increment(ref _phoneNumberCounter):D7}",
            password = "Passw0rd!",
            role = "Employer"
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
