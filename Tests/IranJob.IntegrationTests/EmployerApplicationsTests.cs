using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IranJob.Modules.Candidates.Application.Contracts;
using IranJob.Modules.ReferenceData.Domain.Entities;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IranJob.IntegrationTests;

public sealed class EmployerApplicationsTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string EmployerApplicationsEndpoint = "/api/v1/employers/applications";
    private const string CandidateProfileEndpoint = "/api/v1/candidates/profile";
    private readonly CustomWebApplicationFactory factory;
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private static int userCounter;

    public EmployerApplicationsTests(CustomWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Employer_Lists_Details_Reviews_Accepts_Rejects_Application()
    {
        var (employer, jobId, jobTitle) = await CreateEmployerJobAsync(JobState.Published);
        var candidate = await CreateCandidateAsync(createProfile: true);

        var apply = await candidate.PostAsJsonAsync($"/api/v1/jobs/{jobId}/applications", new
        {
            userId = Guid.NewGuid(),
            candidateProfileId = Guid.NewGuid()
        }, json);
        apply.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await apply.Content.ReadFromJsonAsync<JsonElement>(json);
        created.GetProperty("status").GetString().Should().Be("Applied");
        var applicationId = created.GetProperty("id").GetGuid();

        var list = await employer.GetAsync(EmployerApplicationsEndpoint);
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await list.Content.ReadFromJsonAsync<JsonElement>(json);
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("id").GetGuid().Should().Be(applicationId);
        items[0].GetProperty("jobTitle").GetString().Should().Be(jobTitle);
        items[0].GetProperty("candidateHeadline").GetString().Should().Be("Candidate");
        items[0].GetProperty("status").GetString().Should().Be("Applied");

        var details = await employer.GetAsync($"{EmployerApplicationsEndpoint}/{applicationId}");
        details.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await details.Content.ReadFromJsonAsync<JsonElement>(json);
        detail.GetProperty("jobTitle").GetString().Should().Be(jobTitle);
        detail.GetProperty("candidateProfileId").GetGuid().Should().NotBeEmpty();
        detail.GetProperty("candidateHeadline").GetString().Should().Be("Candidate");
        detail.GetProperty("status").GetString().Should().Be("Applied");

        var review = await employer.PostAsync($"{EmployerApplicationsEndpoint}/{applicationId}/review", null);
        review.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviewed = await review.Content.ReadFromJsonAsync<JsonElement>(json);
        reviewed.GetProperty("status").GetString().Should().Be("Reviewed");

        var accept = await employer.PostAsync($"{EmployerApplicationsEndpoint}/{applicationId}/accept", null);
        accept.StatusCode.Should().Be(HttpStatusCode.OK);
        var accepted = await accept.Content.ReadFromJsonAsync<JsonElement>(json);
        accepted.GetProperty("status").GetString().Should().Be("Accepted");

        var rejectAfterAccept = await employer.PostAsync($"{EmployerApplicationsEndpoint}/{applicationId}/reject", null);
        rejectAfterAccept.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
    [Fact]
    public async Task Employer_Cannot_See_Applications_For_Other_Employers_Jobs()
    {
        var (employerA, jobIdA, _) = await CreateEmployerJobAsync(JobState.Published);
        var (employerB, jobIdB, _) = await CreateEmployerJobAsync(JobState.Published);
        var candidate = await CreateCandidateAsync(createProfile: true);

        var applyA = await candidate.PostAsJsonAsync($"/api/v1/jobs/{jobIdA}/applications", new
        {
            userId = Guid.NewGuid(),
            candidateProfileId = Guid.NewGuid()
        }, json);
        applyA.StatusCode.Should().Be(HttpStatusCode.Created);

        var applyB = await candidate.PostAsJsonAsync($"/api/v1/jobs/{jobIdB}/applications", new
        {
            userId = Guid.NewGuid(),
            candidateProfileId = Guid.NewGuid()
        }, json);
        applyB.StatusCode.Should().Be(HttpStatusCode.Created);
        var applicationIdB = (await applyB.Content.ReadFromJsonAsync<JsonElement>(json)).GetProperty("id").GetGuid();

        var listA = await employerA.GetAsync(EmployerApplicationsEndpoint);
        listA.StatusCode.Should().Be(HttpStatusCode.OK);
        var itemsA = await listA.Content.ReadFromJsonAsync<JsonElement>(json);
        itemsA.GetArrayLength().Should().Be(1);
        itemsA[0].GetProperty("jobId").GetGuid().Should().Be(jobIdA);

        var detailsB = await employerA.GetAsync($"{EmployerApplicationsEndpoint}/{applicationIdB}");
        detailsB.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Candidate_Cannot_Access_Employer_Application_Endpoints()
    {
        var (employer, jobId, _) = await CreateEmployerJobAsync(JobState.Published);
        var candidate = await CreateCandidateAsync(createProfile: true);

        await candidate.PostAsJsonAsync($"/api/v1/jobs/{jobId}/applications", new
        {
            userId = Guid.NewGuid(),
            candidateProfileId = Guid.NewGuid()
        }, json);

        var list = await candidate.GetAsync(EmployerApplicationsEndpoint);
        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var details = await candidate.GetAsync($"{EmployerApplicationsEndpoint}/{Guid.NewGuid()}");
        details.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var review = await candidate.PostAsync($"{EmployerApplicationsEndpoint}/{Guid.NewGuid()}/review", null);
        review.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unauthenticated_User_Cannot_Access_Employer_Applications()
    {
        var (employer, jobId, _) = await CreateEmployerJobAsync(JobState.Published);
        var candidate = await CreateCandidateAsync(createProfile: true);

        await candidate.PostAsJsonAsync($"/api/v1/jobs/{jobId}/applications", new
        {
            userId = Guid.NewGuid(),
            candidateProfileId = Guid.NewGuid()
        }, json);

        var anonymous = factory.CreateClient();
        var list = await anonymous.GetAsync(EmployerApplicationsEndpoint);
        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Employer_Can_Reject_Application_Directly_From_Applied()
    {
        var (employer, jobId, _) = await CreateEmployerJobAsync(JobState.Published);
        var candidate = await CreateCandidateAsync(createProfile: true);

        var apply = await candidate.PostAsJsonAsync($"/api/v1/jobs/{jobId}/applications", new
        {
            userId = Guid.NewGuid(),
            candidateProfileId = Guid.NewGuid()
        }, json);
        apply.StatusCode.Should().Be(HttpStatusCode.Created);
        var applicationId = (await apply.Content.ReadFromJsonAsync<JsonElement>(json)).GetProperty("id").GetGuid();

        var reject = await employer.PostAsync($"{EmployerApplicationsEndpoint}/{applicationId}/reject", null);
        reject.StatusCode.Should().Be(HttpStatusCode.OK);
        var rejected = await reject.Content.ReadFromJsonAsync<JsonElement>(json);
        rejected.GetProperty("status").GetString().Should().Be("Rejected");

        var acceptAfterReject = await employer.PostAsync($"{EmployerApplicationsEndpoint}/{applicationId}/accept", null);
        acceptAfterReject.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Application_NotFound_For_Unknown_ApplicationId()
    {
        var (employer, _, _) = await CreateEmployerJobAsync(JobState.Published);

        var details = await employer.GetAsync($"{EmployerApplicationsEndpoint}/{Guid.NewGuid()}");
        details.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var review = await employer.PostAsync($"{EmployerApplicationsEndpoint}/{Guid.NewGuid()}/review", null);
        review.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<(HttpClient employer, Guid jobId, string title)> CreateEmployerJobAsync(JobState state)
    {
        await factory.InitializeDatabaseAsync();
        using var scope = factory.Services.CreateScope();
        var referenceDb = scope.ServiceProvider.GetRequiredService<ReferenceDataDbContext>();
        var country = Country.Create("Test Country", UniqueCountryCode());
        referenceDb.Countries.Add(country);
        var province = Province.Create(country.Id, $"Province {Guid.NewGuid():N}");
        referenceDb.Provinces.Add(province);
        var city = City.Create(province.Id, $"City {Guid.NewGuid():N}");
        var category = JobCategory.Create($"Category {Guid.NewGuid():N}", null);
        var skill = Skill.Create($"Skill {Guid.NewGuid():N}", null);
        referenceDb.Cities.Add(city);
        referenceDb.JobCategories.Add(category);
        referenceDb.Skills.Add(skill);
        await referenceDb.SaveChangesAsync();

        var companyName = $"Phase 7 Company {Guid.NewGuid():N}";
        var employer = await CreateEmployerAsync(companyName);
        var title = $"Phase 7 Role {Guid.NewGuid():N}";
        var create = await employer.PostAsJsonAsync("/api/v1/employers/jobs", new
        {
            title,
            description = "A role for employer application workflow tests.",
            categoryId = category.Id,
            skillIds = new[] { skill.Id },
            employmentType = "FullTime",
            workArrangement = "Remote",
            cityId = city.Id,
            closingAt = DateTimeOffset.UtcNow.AddDays(30)
        }, json);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>(json);
        var jobId = created.GetProperty("id").GetGuid();

        if (state is JobState.Published)
        {
            var publish = await employer.PostAsync($"/api/v1/employers/jobs/{jobId}/publish", null);
            publish.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        return (employer, jobId, title);
    }

    private async Task<HttpClient> CreateCandidateAsync(bool createProfile)
    {
        var user = await CreateAuthenticatedClientAsync("Candidate");
        if (createProfile)
        {
            var profile = await user.PostAsJsonAsync(CandidateProfileEndpoint, new { headline = "Candidate" }, json);
            profile.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        return user;
    }

    private async Task<HttpClient> CreateEmployerAsync(string companyName)
    {
        var employer = await CreateAuthenticatedClientAsync("Employer");
        var profile = await employer.PostAsJsonAsync("/api/v1/employers/profile", new
        {
            companyName,
            industry = "Technology",
            companySize = "11-50",
            city = "Tehran",
            province = "Tehran"
        }, json);
        profile.StatusCode.Should().Be(HttpStatusCode.Created);
        return employer;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string role)
    {
        await factory.InitializeDatabaseAsync();
        var number = Interlocked.Increment(ref userCounter);
        var email = $"phase7-{number}@test.ir";
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            firstName = "Phase",
            lastName = "Seven",
            email,
            phoneNumber = $"0977{number:D7}",
            password = "Passw0rd!",
            role
        }, json);
        register.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = email, password = "Passw0rd!" }, json);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginPayload = await login.Content.ReadFromJsonAsync<JsonElement>(json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.GetProperty("accessToken").GetString());
        return client;
    }

    private static string UniqueCountryCode() => new string(Guid.NewGuid().ToByteArray()
        .Take(3).Select(value => (char)('A' + value % 26)).ToArray());

    private enum JobState { Draft, Published, Closed }

    [Fact]
    public async Task Employer_Downloads_Candidates_Uploaded_Cv()
    {
        var (employer, jobId, _) = await CreateEmployerJobAsync(JobState.Published);
        var candidate = await CreateCandidateAsync(createProfile: true);

        // Candidate uploads a CV and consents to sharing it with employers.
        await UploadCandidateCvAsync(candidate);
        (await candidate.PostAsync("/api/v1/candidates/resume/status?status=ReadyToShare", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Candidate applies to the employer published job posting.
        var apply = await candidate.PostAsJsonAsync($"/api/v1/jobs/{jobId}/applications",
            new { userId = Guid.NewGuid(), candidateProfileId = Guid.NewGuid() }, json);
        apply.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await apply.Content.ReadFromJsonAsync<JsonElement>(json);
        var applicationId = created.GetProperty("id").GetGuid();

        // Employer downloads the candidate CV file through the CV endpoint.
        var cv = await employer.GetAsync($"/api/v1/employers/applications/{applicationId}/cv");
        cv.StatusCode.Should().Be(HttpStatusCode.OK);
        cv.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var bytes = await cv.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

                private async Task UpsertCandidateResumeAsync(HttpClient candidate, bool share = false)
    {
        var request = new SaveResumeRequest(
            "Backend engineer with .NET experience.",
            "Complete",
            new[]
            {
                new ResumeWorkExperienceItem(null, "Acme Corp", "Senior .NET Engineer",
                    "Built cloud platforms.", new DateOnly(2020, 1, 1), new DateOnly(2024, 1, 1), false)
            },
            null, null, null, null, null, null);
        var upsert = await candidate.PutAsJsonAsync("/api/v1/candidates/resume", request, json);
        upsert.StatusCode.Should().Be(HttpStatusCode.OK);
        if (share)
            (await candidate.PostAsync("/api/v1/candidates/resume/status?status=ReadyToShare", null))
                .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> ApplyAsync(HttpClient candidate, Guid jobId)
    {
        var apply = await candidate.PostAsJsonAsync($"/api/v1/jobs/{jobId}/applications",
            new { userId = Guid.NewGuid(), candidateProfileId = Guid.NewGuid() }, json);
        apply.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await apply.Content.ReadFromJsonAsync<JsonElement>(json);
        return created.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Employer_Views_Consented_Candidate_Resume_And_Downloads_Cv()
    {
        var (employer, jobId, _) = await CreateEmployerJobAsync(JobState.Published);
        var candidate = await CreateCandidateAsync(createProfile: true);

        // Candidate builds a resume and consents to sharing it, then uploads a CV document.
        await UpsertCandidateResumeAsync(candidate, share: true);
        await UploadCandidateCvAsync(candidate);
        var applicationId = await ApplyAsync(candidate, jobId);

        var resume = await employer.GetAsync($"{EmployerApplicationsEndpoint}/{applicationId}/resume");
        resume.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await resume.Content.ReadFromJsonAsync<JsonElement>(json);
        payload.GetProperty("candidateHeadline").GetString().Should().Be("Candidate");
        payload.GetProperty("status").GetString().Should().Be("ReadyToShare");
        payload.GetProperty("completenessScore").GetInt32().Should().BeGreaterThan(0);
        payload.GetProperty("workExperiences").GetArrayLength().Should().Be(1);
        payload.GetProperty("hasCvFile").GetBoolean().Should().BeTrue();

        var cv = await employer.GetAsync($"{EmployerApplicationsEndpoint}/{applicationId}/cv");
        cv.StatusCode.Should().Be(HttpStatusCode.OK);
        cv.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        (await cv.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Employer_Cannot_View_Candidate_Resume_Until_Consented()
    {
        var (employer, jobId, _) = await CreateEmployerJobAsync(JobState.Published);
        var candidate = await CreateCandidateAsync(createProfile: true);

        // Resume exists and a CV is uploaded, but the candidate has not consented to sharing.
        await UpsertCandidateResumeAsync(candidate, share: false);
        await UploadCandidateCvAsync(candidate);
        var applicationId = await ApplyAsync(candidate, jobId);

        var resume = await employer.GetAsync($"{EmployerApplicationsEndpoint}/{applicationId}/resume");
        resume.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Consent gates the CV download too — a Draft resume must not leak the CV even if present.
        var cv = await employer.GetAsync($"{EmployerApplicationsEndpoint}/{applicationId}/cv");
        cv.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Employer_Cannot_View_Resume_Of_Other_Employer_Application()
    {
        var (employerA, _, _) = await CreateEmployerJobAsync(JobState.Published);
        var (_, jobIdB, _) = await CreateEmployerJobAsync(JobState.Published);
        var candidate = await CreateCandidateAsync(createProfile: true);

        await UpsertCandidateResumeAsync(candidate, share: true);
        await UploadCandidateCvAsync(candidate);
        // Candidate applies to employer B's posting only.
        var applicationIdB = await ApplyAsync(candidate, jobIdB);

        // Employer A has no application for this resume → 404 (owner gate).
        var resume = await employerA.GetAsync($"{EmployerApplicationsEndpoint}/{applicationIdB}/resume");
        resume.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task UploadCandidateCvAsync(HttpClient candidate)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 })); // "%PDF-1.4"
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        content.Add(streamContent, "file", "cv.pdf");
        var response = await candidate.PostAsync("/api/v1/candidates/resume/cv", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

}
