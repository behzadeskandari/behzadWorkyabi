using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IranJob.Modules.JobPostings.Domain.Entities;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.Modules.ReferenceData.Domain.Entities;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IranJob.IntegrationTests;

public sealed class JobApplicationTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string ApplicationsEndpoint = "/api/v1/candidates/applications";
    private const string ProfileEndpoint = "/api/v1/candidates/profile";
    private readonly CustomWebApplicationFactory factory;
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private static int userCounter;

    public JobApplicationTests(CustomWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Candidate_AppliesListsReadsAndWithdrawsOwnApplication()
    {
        var job = await CreateJobAsync(JobState.Published);
        var candidate = await CreateCandidateAsync(createProfile: true);

        var apply = await candidate.PostAsJsonAsync($"/api/v1/jobs/{job.Id}/applications", new
        {
            userId = Guid.NewGuid(),
            candidateProfileId = Guid.NewGuid()
        }, json);
        apply.StatusCode.Should().Be(HttpStatusCode.Created);
        apply.Headers.Location.Should().NotBeNull();
        var created = await apply.Content.ReadFromJsonAsync<JsonElement>(json);
        created.GetProperty("jobId").GetGuid().Should().Be(job.Id);
        created.GetProperty("jobTitle").GetString().Should().Be(job.Title);
        created.GetProperty("companyName").GetString().Should().Be(job.CompanyName);
        created.GetProperty("status").GetString().Should().Be("Applied");
        created.TryGetProperty("candidateProfileId", out _).Should().BeFalse();
        created.TryGetProperty("userId", out _).Should().BeFalse();
        var applicationId = created.GetProperty("id").GetGuid();

        var list = await candidate.GetAsync(ApplicationsEndpoint);
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await list.Content.ReadFromJsonAsync<JsonElement>(json);
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("id").GetGuid().Should().Be(applicationId);

        var details = await candidate.GetAsync($"{ApplicationsEndpoint}/{applicationId}");
        details.StatusCode.Should().Be(HttpStatusCode.OK);
        (await details.Content.ReadFromJsonAsync<JsonElement>(json)).GetProperty("jobTitle").GetString().Should().Be(job.Title);

        var duplicate = await candidate.PostAsync($"/api/v1/jobs/{job.Id}/applications", null);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var withdrawn = await candidate.PostAsync($"{ApplicationsEndpoint}/{applicationId}/withdraw", null);
        withdrawn.StatusCode.Should().Be(HttpStatusCode.OK);
        (await withdrawn.Content.ReadFromJsonAsync<JsonElement>(json)).GetProperty("status").GetString().Should().Be("Withdrawn");

        (await candidate.PostAsync($"{ApplicationsEndpoint}/{applicationId}/withdraw", null))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await candidate.PostAsync($"/api/v1/jobs/{job.Id}/applications", null))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Verify that duplicate protection is also enforced below the service check.
        using var scope = factory.Services.CreateScope();
        var candidateProfile = await candidate.GetAsync(ProfileEndpoint);
        var candidateProfileId = (await candidateProfile.Content.ReadFromJsonAsync<JsonElement>(json))
            .GetProperty("id").GetGuid();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobPostingDbContext>();
        dbContext.JobApplications.Add(JobApplication.Create(candidateProfileId, job.Id));
        var uniqueViolation = async () => await dbContext.SaveChangesAsync();
        await uniqueViolation.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ApplicationEndpoints_RequireCandidateAuthenticationAndProfile()
    {
        var job = await CreateJobAsync(JobState.Published);
        var anonymous = factory.CreateClient();
        (await anonymous.PostAsync($"/api/v1/jobs/{job.Id}/applications", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync(ApplicationsEndpoint)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var employer = await CreateEmployerAsync();
        (await employer.PostAsync($"/api/v1/jobs/{job.Id}/applications", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await employer.GetAsync(ApplicationsEndpoint)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var candidateWithoutProfile = await CreateCandidateAsync(createProfile: false);
        (await candidateWithoutProfile.PostAsync($"/api/v1/jobs/{job.Id}/applications", null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await candidateWithoutProfile.GetAsync(ApplicationsEndpoint)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Candidate_CannotApplyToMissingDraftOrClosedJobs()
    {
        var candidate = await CreateCandidateAsync(createProfile: true);
        (await candidate.PostAsync($"/api/v1/jobs/{Guid.NewGuid()}/applications", null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        var draft = await CreateJobAsync(JobState.Draft);
        (await candidate.PostAsync($"/api/v1/jobs/{draft.Id}/applications", null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        var closed = await CreateJobAsync(JobState.Closed);
        (await candidate.PostAsync($"/api/v1/jobs/{closed.Id}/applications", null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

    }

    [Fact]
    public async Task Candidate_CannotReadOrWithdrawAnotherCandidatesApplication()
    {
        var job = await CreateJobAsync(JobState.Published);
        var owner = await CreateCandidateAsync(createProfile: true);
        var other = await CreateCandidateAsync(createProfile: true);
        var response = await owner.PostAsync($"/api/v1/jobs/{job.Id}/applications", null);
        var applicationId = (await response.Content.ReadFromJsonAsync<JsonElement>(json)).GetProperty("id").GetGuid();

        (await other.GetAsync($"{ApplicationsEndpoint}/{applicationId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await other.PostAsync($"{ApplicationsEndpoint}/{applicationId}/withdraw", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await other.GetAsync(ApplicationsEndpoint)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await (await other.GetAsync(ApplicationsEndpoint)).Content.ReadFromJsonAsync<JsonElement>(json)).GetArrayLength().Should().Be(0);
    }

    private async Task<JobInfo> CreateJobAsync(JobState state)
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
            description = "A role for application workflow tests.",
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

        if (state is JobState.Published or JobState.Closed)
        {
            var publish = await employer.PostAsync($"/api/v1/employers/jobs/{jobId}/publish", null);
            publish.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        if (state == JobState.Closed)
        {
            (await employer.PostAsync($"/api/v1/employers/jobs/{jobId}/close", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        return new JobInfo(jobId, title, companyName);
    }

    private async Task<HttpClient> CreateCandidateAsync(bool createProfile)
    {
        var user = await CreateAuthenticatedClientAsync("Candidate");
        if (createProfile)
        {
            var profile = await user.PostAsJsonAsync(ProfileEndpoint, new { headline = "Candidate" }, json);
            profile.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        return user;
    }

    private async Task<HttpClient> CreateEmployerAsync(string? companyName = null)
    {
        var employer = await CreateAuthenticatedClientAsync("Employer");
        var profile = await employer.PostAsJsonAsync("/api/v1/employers/profile", new
        {
            companyName = companyName ?? $"Phase 7 Company {Guid.NewGuid():N}",
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

    private sealed record JobInfo(Guid Id, string Title, string CompanyName);
    private enum JobState { Draft, Published, Closed }
}
