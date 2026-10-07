using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IranJob.Modules.EmployerProfile.Infrastructure.Persistence;
using IranJob.Modules.Identity.Domain.Entities;
using IranJob.Modules.Identity.Infrastructure.Persistence;
using IranJob.Modules.ReferenceData.Domain.Entities;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IranJob.IntegrationTests;

public sealed class JobPostingTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string JobsEndpoint = "/api/v1/employers/jobs";
    private const string EmployerProfileEndpoint = "/api/v1/employers/profile";
    private readonly CustomWebApplicationFactory factory;
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private static int userCounter;

    public JobPostingTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task JobManagement_RequiresEmployerRole()
    {
        await factory.InitializeDatabaseAsync();
        var anonymous = factory.CreateClient();
        (await anonymous.GetAsync(JobsEndpoint)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync(JobsEndpoint, new { title = "anonymous" }, json)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var candidate = await CreateAuthenticatedClientAsync("Candidate");
        (await candidate.GetAsync(JobsEndpoint)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await candidate.PostAsJsonAsync(JobsEndpoint, new { title = "candidate" }, json)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Employer_CreatesListsAndReadsOwnDraft_WithoutAcceptingClientOwnership()
    {
        var references = await SeedReferenceDataAsync();
        var employerA = await CreateEmployerAsync();
        var employerB = await CreateEmployerAsync();
        var request = ValidRequest(references) with
        {
            UserId = Guid.NewGuid(),
            EmployerProfileId = employerB.ProfileId,
            Status = "Published"
        };

        var response = await employerA.Client.PostAsJsonAsync(JobsEndpoint, request, json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(json);
        created.GetProperty("status").GetString().Should().Be("Draft");
        created.GetProperty("categoryName").GetString().Should().Be(references.CategoryName);
        created.GetProperty("skills")[0].GetProperty("name").GetString().Should().Be(references.SkillName);
        created.GetProperty("cityName").GetString().Should().Be(references.CityName);
        created.TryGetProperty("userId", out _).Should().BeFalse();
        created.TryGetProperty("employerProfileId", out _).Should().BeFalse();
        var id = created.GetProperty("id").GetGuid();

        var ownList = await employerA.Client.GetAsync(JobsEndpoint);
        ownList.StatusCode.Should().Be(HttpStatusCode.OK);
        var ownJobs = await ownList.Content.ReadFromJsonAsync<JsonElement>(json);
        ownJobs.GetArrayLength().Should().Be(1);
        (await employerA.Client.GetAsync($"{JobsEndpoint}/{id}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var otherList = await employerB.Client.GetAsync(JobsEndpoint);
        otherList.StatusCode.Should().Be(HttpStatusCode.OK);
        (await otherList.Content.ReadFromJsonAsync<JsonElement>(json)).GetArrayLength().Should().Be(0);
        (await employerB.Client.GetAsync($"{JobsEndpoint}/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await employerB.Client.PutAsJsonAsync($"{JobsEndpoint}/{id}", ValidRequest(references), json))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await employerB.Client.PostAsync($"{JobsEndpoint}/{id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateJob_RejectsInvalidReferenceIdsSalaryAndDuplicateSkillIds()
    {
        var references = await SeedReferenceDataAsync();
        var employer = await CreateEmployerAsync();

        (await employer.Client.PostAsJsonAsync(JobsEndpoint, ValidRequest(references) with { CategoryId = Guid.NewGuid() }, json))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await employer.Client.PostAsJsonAsync(JobsEndpoint, ValidRequest(references) with { SkillIds = [Guid.NewGuid()] }, json))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await employer.Client.PostAsJsonAsync(JobsEndpoint, ValidRequest(references) with { SkillIds = [] }, json))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await employer.Client.PostAsJsonAsync(JobsEndpoint, ValidRequest(references) with { CityId = Guid.NewGuid() }, json))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await employer.Client.PostAsJsonAsync(JobsEndpoint, ValidRequest(references) with
        {
            WorkArrangement = "Remote",
            CityId = null
        }, json)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await employer.Client.PostAsJsonAsync(JobsEndpoint, ValidRequest(references) with { SalaryMinimum = 90000m, SalaryMaximum = 50000m }, json))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await employer.Client.PostAsJsonAsync(JobsEndpoint, ValidRequest(references) with
        {
            SalaryMinimum = null,
            SalaryMaximum = null,
            SalaryCurrency = "IRR",
            SalaryPeriod = "Monthly"
        }, json)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await employer.Client.PostAsJsonAsync(JobsEndpoint, ValidRequest(references) with { SkillIds = [references.SkillId, references.SkillId] }, json))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Draft_Update_Publish_Unpublish_Republish_AndClose_EnforceLifecycle()
    {
        var references = await SeedReferenceDataAsync();
        var employer = await CreateEmployerAsync();
        var createResponse = await employer.Client.PostAsJsonAsync(JobsEndpoint, ValidRequest(references), json);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        var id = created.GetProperty("id").GetGuid();

        (await employer.Client.PostAsync($"{JobsEndpoint}/{id}/unpublish", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await employer.Client.PostAsync($"{JobsEndpoint}/{id}/close", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);

        var update = ValidRequest(references) with { Title = "Updated draft title" };
        var updateResponse = await employer.Client.PutAsJsonAsync($"{JobsEndpoint}/{id}", update, json);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await updateResponse.Content.ReadFromJsonAsync<JsonElement>(json)).GetProperty("status").GetString().Should().Be("Draft");

        var publishResponse = await employer.Client.PostAsync($"{JobsEndpoint}/{id}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await publishResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        published.GetProperty("status").GetString().Should().Be("Published");
        published.GetProperty("publishedAt").ValueKind.Should().Be(JsonValueKind.String);

        var unpublishResponse = await employer.Client.PostAsync($"{JobsEndpoint}/{id}/unpublish", null);
        unpublishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var unpublished = await unpublishResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        unpublished.GetProperty("status").GetString().Should().Be("Draft");
        unpublished.GetProperty("publishedAt").ValueKind.Should().Be(JsonValueKind.Null);

        (await employer.Client.PostAsync($"{JobsEndpoint}/{id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var closeResponse = await employer.Client.PostAsync($"{JobsEndpoint}/{id}/close", null);
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closed = await closeResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        closed.GetProperty("status").GetString().Should().Be("Closed");
        closed.GetProperty("closedAt").ValueKind.Should().Be(JsonValueKind.String);

        (await employer.Client.PutAsJsonAsync($"{JobsEndpoint}/{id}", update, json)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await employer.Client.PostAsync($"{JobsEndpoint}/{id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Publish_RejectsReferencesThatWereDeactivatedAfterDraftCreation()
    {
        var references = await SeedReferenceDataAsync();
        var employer = await CreateEmployerAsync();
        var response = await employer.Client.PostAsJsonAsync(JobsEndpoint, ValidRequest(references), json);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(json);
        var id = created.GetProperty("id").GetGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var referenceDb = scope.ServiceProvider.GetRequiredService<ReferenceDataDbContext>();
            var category = await referenceDb.JobCategories.SingleAsync(item => item.Id == references.CategoryId);
            category.Deactivate();
            await referenceDb.SaveChangesAsync();
        }

        (await employer.Client.PostAsync($"{JobsEndpoint}/{id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EmployerRoleWithoutEmployerProfileCannotManageJobs()
    {
        var employer = await CreateAuthenticatedClientAsync("Employer");
        (await employer.GetAsync(JobsEndpoint)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await employer.PostAsJsonAsync(JobsEndpoint, ValidRequest(await SeedReferenceDataAsync()), json))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<ReferenceIds> SeedReferenceDataAsync()
    {
        await factory.InitializeDatabaseAsync();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReferenceDataDbContext>();
        var categoryName = $"Software {Guid.NewGuid():N}";
        var skillName = $"CSharp {Guid.NewGuid():N}";
        var country = Country.Create("Test Country", UniqueCountryCode());
        dbContext.Countries.Add(country);
        var province = Province.Create(country.Id, $"Test Province {Guid.NewGuid():N}");
        dbContext.Provinces.Add(province);
        var city = City.Create(province.Id, $"Test City {Guid.NewGuid():N}");
        var category = JobCategory.Create(categoryName, null);
        var skill = Skill.Create(skillName, null);
        dbContext.Cities.Add(city);
        dbContext.JobCategories.Add(category);
        dbContext.Skills.Add(skill);
        await dbContext.SaveChangesAsync();
        return new ReferenceIds(category.Id, category.Name, skill.Id, skill.Name, city.Id, city.Name);
    }

    private async Task<EmployerSession> CreateEmployerAsync()
    {
        var client = await CreateAuthenticatedClientAsync("Employer");
        var profileResponse = await client.PostAsJsonAsync(EmployerProfileEndpoint, new
        {
            companyName = $"Company {Guid.NewGuid():N}",
            industry = "Technology",
            companySize = "11-50",
            city = "Tehran",
            province = "Tehran"
        }, json);
        profileResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var profile = await profileResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        return new EmployerSession(client, profile.GetProperty("id").GetGuid());
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string role)
    {
        await factory.InitializeDatabaseAsync();
        var number = Interlocked.Increment(ref userCounter);
        var email = $"job-posting-{number}@test.ir";
        var client = factory.CreateClient();
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            firstName = "Test",
            lastName = "Job Employer",
            email,
            phoneNumber = $"0988{number:D7}",
            password = "Passw0rd!",
            role
        }, json);
        registration.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = email, password = "Passw0rd!" }, json);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static JobRequest ValidRequest(ReferenceIds references) => new(
        Title: "Senior .NET Engineer",
        Description: "Build and maintain reliable services.",
        CategoryId: references.CategoryId,
        SkillIds: [references.SkillId],
        EmploymentType: "FullTime",
        WorkArrangement: "OnSite",
        CityId: references.CityId,
        SalaryMinimum: 50000m,
        SalaryMaximum: 80000m,
        SalaryCurrency: "IRR",
        SalaryPeriod: "Monthly",
        ClosingAt: DateTimeOffset.UtcNow.AddDays(30));

    private static string UniqueCountryCode() => new string(Guid.NewGuid().ToByteArray()
        .Take(3).Select(value => (char)('A' + value % 26)).ToArray());

    private sealed record EmployerSession(HttpClient Client, Guid ProfileId);
    private sealed record ReferenceIds(Guid CategoryId, string CategoryName, Guid SkillId, string SkillName, Guid CityId, string CityName);
    private sealed record JobRequest(
        string Title,
        string Description,
        Guid CategoryId,
        IReadOnlyList<Guid> SkillIds,
        string EmploymentType,
        string WorkArrangement,
        Guid? CityId,
        decimal? SalaryMinimum,
        decimal? SalaryMaximum,
        string? SalaryCurrency,
        string? SalaryPeriod,
        DateTimeOffset? ClosingAt)
    {
        public Guid UserId { get; init; }
        public Guid EmployerProfileId { get; init; }
        public string? Status { get; init; }
    }
}
