using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IranJob.Modules.Candidates.Application.Contracts;
using Xunit;

namespace IranJob.IntegrationTests;

public sealed class CandidateCompletionTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string CompletionEndpoint = "/api/v1/candidates/profile/completion";
    private const string ProfileEndpoint = "/api/v1/candidates/profile";
    private const string ResumeEndpoint = "/api/v1/candidates/resume";

    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private static int _userCounter;
    private static int _phoneNumberCounter;

    public CandidateCompletionTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Completion_WithoutAuthentication_ReturnsUnauthorized()
    {
        await _factory.InitializeDatabaseAsync();
        var client = _factory.CreateClient();
        (await client.GetAsync(CompletionEndpoint)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Completion_WithoutProfileOrResume_ReturnsNotReadyWithNullResume()
    {
        var client = await CreateAuthenticatedCandidateAsync("completion-empty@test.ir");

        var response = await client.GetAsync(CompletionEndpoint);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        payload.GetProperty("hasProfile").GetBoolean().Should().BeFalse();
        payload.GetProperty("profileCompletenessScore").GetInt32().Should().Be(0);
        payload.GetProperty("resume").ValueKind.Should().Be(JsonValueKind.Null);

        var readiness = payload.GetProperty("readiness");
        readiness.GetProperty("isReady").GetBoolean().Should().BeFalse();
        readiness.GetProperty("hasRequiredProfile").GetBoolean().Should().BeFalse();
        readiness.GetProperty("hasRecommendedResume").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Completion_WithProfileButDraftResume_AdvisesSharing()
    {
        var client = await CreateAuthenticatedCandidateAsync("completion-partial@test.ir");
        await client.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);
        await client.PutAsJsonAsync(ResumeEndpoint, ValidResumeRequest() with { Status = "Draft" }, _json);

        var response = await client.GetAsync(CompletionEndpoint);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        payload.GetProperty("hasProfile").GetBoolean().Should().BeTrue();
        payload.GetProperty("resume").GetProperty("isShared").GetBoolean().Should().BeFalse();

        var readiness = payload.GetProperty("readiness");
        readiness.GetProperty("isReady").GetBoolean().Should().BeTrue();
        readiness.GetProperty("hasRecommendedResume").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Completion_WithFullProfileAndReadyToShareResume_IsReady()
    {
        var client = await CreateAuthenticatedCandidateAsync("completion-full@test.ir");
        await client.PostAsJsonAsync(ProfileEndpoint, ValidProfileRequest(), _json);
        await client.PutAsJsonAsync(ResumeEndpoint, ValidResumeRequest(), _json);
        var set = await client.PostAsync($"{ResumeEndpoint}/status?status=ReadyToShare", null);
        set.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.GetAsync(CompletionEndpoint);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        payload.GetProperty("hasProfile").GetBoolean().Should().BeTrue();
        payload.GetProperty("profileCompletenessScore").GetInt32().Should().BeGreaterOrEqualTo(40);
        payload.GetProperty("resume").GetProperty("isShared").GetBoolean().Should().BeTrue();

        var readiness = payload.GetProperty("readiness");
        readiness.GetProperty("isReady").GetBoolean().Should().BeTrue();
        readiness.GetProperty("hasRequiredProfile").GetBoolean().Should().BeTrue();
        readiness.GetProperty("hasRecommendedResume").GetBoolean().Should().BeTrue();
    }

    private static SaveResumeRequest ValidResumeRequest() => new(
        "Backend engineer.",
        "Complete",
        new[] { new ResumeWorkExperienceItem(null, "Acme Corp", "Senior .NET Engineer", "Built platforms.", new DateOnly(2020, 1, 1), new DateOnly(2024, 1, 1), false) },
        new[] { new ResumeEducationItem(null, "Sharif University", "Bachelor", "Computer Engineering", "18.5/20", new DateOnly(2016, 9, 1), new DateOnly(2020, 6, 1), false) },
        new[] { new ResumeSkillItem(null, Guid.NewGuid(), "C#", "Advanced", 5), new ResumeSkillItem(null, Guid.NewGuid(), "ASP.NET Core", "Advanced", 4) },
        new[] { new ResumeLanguageItem(null, "English", "Fluent") },
        new[] { new ResumeProjectItem(null, "IranJob", "Open source platform", "https://iranjob.test", new DateOnly(2022, 3, 1), new DateOnly(2023, 3, 1), false) },
        new[] { new ResumeCertificationItem(null, "AWS Certified Solutions Architect", "Amazon", new DateOnly(2023, 1, 1), null, "https://aws.amazon.com/certification/") },
        new[] { new ResumePortfolioLinkItem(null, "GitHub", "https://github.com/test", "Public code") });

    private static object ValidProfileRequest() => new
    {
        headline = "Senior .NET Developer",
        biography = "Experienced backend developer.",
        dateOfBirth = "1995-08-20",
        gender = "Male",
        city = "Tehran",
        province = "Tehran",
        linkedInUrl = "https://linkedin.com/in/test",
        gitHubUrl = "https://github.com/test",
        portfolioUrl = "https://test.dev",
        expectedSalary = 75_000_000m,
        salaryType = "Monthly",
        employmentStatus = "Employed",
        availability = "WithinOneMonth",
        militaryStatus = "Completed"
    };

    private async Task<HttpClient> CreateAuthenticatedCandidateAsync(string email)
    {
        await _factory.InitializeDatabaseAsync();
        var number = Interlocked.Increment(ref _userCounter);
        var client = _factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            firstName = "Candidate",
            lastName = "Completion",
            email,
            phoneNumber = $"0912{Interlocked.Increment(ref _phoneNumberCounter):D7}",
            password = "Passw0rd!",
            role = "Candidate"
        }, _json);
        register.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { identifier = email, password = "Passw0rd!" }, _json);
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginPayload = await login.Content.ReadFromJsonAsync<JsonElement>(_json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", loginPayload.GetProperty("accessToken").GetString()!);
        return client;
    }
}
