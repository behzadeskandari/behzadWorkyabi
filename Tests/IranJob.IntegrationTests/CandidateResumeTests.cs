using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IranJob.Modules.Candidates.Application.Contracts;

namespace IranJob.IntegrationTests;

/// <summary>
/// Integration tests for the candidate resume aggregate (CRUD + CV upload/download).
/// These exercise the rewritten CandidateResumeService end-to-end through the HTTP API.
/// </summary>
public sealed class CandidateResumeTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string ResumeEndpoint = "/api/v1/candidates/resume";
    private const string CvEndpoint = ResumeEndpoint + "/cv";

    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private static int _userCounter;
    private static int _phoneNumberCounter;

    public CandidateResumeTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetResume_WithoutResume_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedCandidateAsync();
        var response = await client.GetAsync(ResumeEndpoint);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Resume_WithoutAuthentication_ReturnsUnauthorized()
    {
        await _factory.InitializeDatabaseAsync();
        var client = _factory.CreateClient();
        (await client.GetAsync(ResumeEndpoint)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync(ResumeEndpoint, ValidResumeRequest(), _json)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync(CvEndpoint)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpsertResume_ThenGet_ReturnsFullResume_WithCompletenessScore()
    {
        var client = await CreateAuthenticatedCandidateAsync();

        var upsert = await client.PutAsJsonAsync(ResumeEndpoint, ValidResumeRequest(), _json);
        upsert.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await upsert.Content.ReadFromJsonAsync<JsonElement>(_json);
        payload.GetProperty("professionalSummary").GetString().Should().Be("Backend engineer.");

        // UpsertResumeAsync intentionally ignores the Status field on the request — the
        // aggregate always starts at Draft. Status is advanced by the dedicated endpoint.
        payload.GetProperty("status").GetString().Should().Be("Draft");

        // All section buckets + summary = 15+15+15+15+10+10+10+5 = 95 (no CV file yet).
        payload.GetProperty("completenessScore").GetInt32().Should().Be(95);
        payload.GetProperty("hasCvFile").GetBoolean().Should().BeFalse();

        payload.GetProperty("workExperiences").GetArrayLength().Should().Be(1);
        payload.GetProperty("educations").GetArrayLength().Should().Be(1);
        payload.GetProperty("skills").GetArrayLength().Should().Be(2);
        payload.GetProperty("languages").GetArrayLength().Should().Be(1);
        payload.GetProperty("projects").GetArrayLength().Should().Be(1);
        payload.GetProperty("certifications").GetArrayLength().Should().Be(1);
        payload.GetProperty("portfolioLinks").GetArrayLength().Should().Be(1);

        var skillNames = payload.GetProperty("skills").EnumerateArray()
            .Select(e => e.GetProperty("skillName").GetString())
            .ToArray();
        skillNames.Should().Contain("C#");
        skillNames.Should().Contain("ASP.NET Core");

        // A fresh GET returns the same persisted resume.
        var get = await client.GetAsync(ResumeEndpoint);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        (await get.Content.ReadFromJsonAsync<JsonElement>(_json))
            .GetProperty("completenessScore").GetInt32().Should().Be(95);
    }

    [Fact]
    public async Task UpsertResume_InvalidStatus_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedCandidateAsync();
        var request = ValidResumeRequest() with { Status = "NotARealStatus" };
        var response = await client.PutAsJsonAsync(ResumeEndpoint, request, _json);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SyncSkills_RemovesItemsNotPresentOnUpdate()
    {
        var client = await CreateAuthenticatedCandidateAsync();

        // Seed a resume with two skills.
        await client.PutAsJsonAsync(ResumeEndpoint, ValidResumeRequest(), _json);
        var current = await (await client.GetAsync(ResumeEndpoint)).Content.ReadFromJsonAsync<JsonElement>(_json);
        var keptId = current.GetProperty("skills")[0].GetProperty("id").GetGuid();

        // Re-upsert keeping one skill (renamed) and adding a new one; the other is dropped.
        var replacement = ValidResumeRequest() with
        {
            Skills = new[]
            {
                new ResumeSkillItem(keptId, Guid.NewGuid(), "Keep", null, 3),
                new ResumeSkillItem(null, Guid.NewGuid(), "Extra", null, 1)
            }
        };

        var response = await client.PutAsJsonAsync(ResumeEndpoint, replacement, _json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        var skillNames = after.GetProperty("skills").EnumerateArray()
            .Select(e => e.GetProperty("skillName").GetString())
            .ToArray();

        // 2 skills remain: the kept skill retains its canonical name ("C#"); the orphan is removed; "Extra" is added.
        skillNames.Should().HaveCount(2);
        skillNames.Should().BeEquivalentTo(new[] { "C#", "Extra" });
    }

    [Fact]
    public async Task UploadDownloadDeleteCv_RoundTripsFile_AndCompletesResume()
    {
        var client = await CreateAuthenticatedCandidateAsync();
        await client.PutAsJsonAsync(ResumeEndpoint, ValidResumeRequest(), _json);

        // No CV yet -> 404
        (await client.GetAsync(CvEndpoint)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Upload
        var cvBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // "%PDF-1.4"
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(new MemoryStream(cvBytes));
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        content.Add(streamContent, "file", "cv.pdf");

        var upload = await client.PostAsync(CvEndpoint, content);
        upload.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploaded = await upload.Content.ReadFromJsonAsync<JsonElement>(_json);
        uploaded.GetProperty("hasCvFile").GetBoolean().Should().BeTrue();
        uploaded.GetProperty("cvFileSizeBytes").GetInt64().Should().Be(cvBytes.Length);
        uploaded.GetProperty("completenessScore").GetInt32().Should().Be(100);

        // Download returns the original bytes.
        var download = await client.GetAsync(CvEndpoint);
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var downloadedBytes = await download.Content.ReadAsByteArrayAsync();
        downloadedBytes.Should().BeEquivalentTo(cvBytes);

        // Delete the CV -> completeness drops back to 95, download -> 404.
        (await client.DeleteAsync(CvEndpoint)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync(CvEndpoint)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var afterDelete = await (await client.GetAsync(ResumeEndpoint)).Content.ReadFromJsonAsync<JsonElement>(_json);
        afterDelete.GetProperty("completenessScore").GetInt32().Should().Be(95);
    }

    [Fact]
    public async Task UploadCv_WithDisallowedExtension_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedCandidateAsync();
        await client.PutAsJsonAsync(ResumeEndpoint, ValidResumeRequest(), _json);

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(new MemoryStream(new byte[] { 1, 2, 3 }));
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(streamContent, "file", "resume.txt");

        var response = await client.PostAsync(CvEndpoint, content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetStatus_ValidStatus_UpdatesStatus()
    {
        var client = await CreateAuthenticatedCandidateAsync();
        await client.PutAsJsonAsync(ResumeEndpoint, ValidResumeRequest(), _json);

        var response = await client.PostAsync($"/api/v1/candidates/resume/status?status=ReadyToShare", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<JsonElement>(_json))
            .GetProperty("status").GetString().Should().Be("ReadyToShare");
    }

    [Fact]
    public async Task SetStatus_InvalidStatus_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedCandidateAsync();
        await client.PutAsJsonAsync(ResumeEndpoint, ValidResumeRequest(), _json);

        var response = await client.PostAsync($"{ResumeEndpoint}/status?status=Bogus", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static SaveResumeRequest ValidResumeRequest() => new(
        "Backend engineer.",
        "Complete",
        new[]
        {
            new ResumeWorkExperienceItem(null, "Acme Corp", "Senior .NET Engineer",
                "Built platforms.", new DateOnly(2020, 1, 1), new DateOnly(2024, 1, 1), false)
        },
        new[]
        {
            new ResumeEducationItem(null, "Sharif University", "Bachelor", "Computer Engineering",
                "18.5/20", new DateOnly(2016, 9, 1), new DateOnly(2020, 6, 1), false)
        },
        new[]
        {
            new ResumeSkillItem(null, Guid.NewGuid(), "C#", "Advanced", 5),
            new ResumeSkillItem(null, Guid.NewGuid(), "ASP.NET Core", "Advanced", 4)
        },
        new[] { new ResumeLanguageItem(null, "English", "Fluent") },
        new[]
        {
            new ResumeProjectItem(null, "IranJob", "Open source platform", "https://iranjob.test",
                new DateOnly(2022, 3, 1), new DateOnly(2023, 3, 1), false)
        },
        new[]
        {
            new ResumeCertificationItem(null, "AWS Certified Solutions Architect", "Amazon",
                new DateOnly(2023, 1, 1), null, "https://aws.amazon.com/certification/")
        },
        new[] { new ResumePortfolioLinkItem(null, "GitHub", "https://github.com/test", "Public code") });

    private async Task<HttpClient> CreateAuthenticatedCandidateAsync()
    {
        await _factory.InitializeDatabaseAsync();
        var number = Interlocked.Increment(ref _userCounter);
        var email = $"resume-{number}@test.ir";
        var client = _factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            firstName = "Candidate",
            lastName = "Resume",
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
