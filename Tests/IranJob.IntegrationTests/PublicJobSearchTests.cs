using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IranJob.Modules.ReferenceData.Domain.Entities;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IranJob.IntegrationTests;

public sealed class PublicJobSearchTests : IAsyncLifetime
{
    private const string SearchEndpoint = "/api/v1/jobs";
    private const string EmployerJobsEndpoint = "/api/v1/employers/jobs";
    private const string EmployerProfileEndpoint = "/api/v1/employers/profile";
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private static int userCounter;

    private readonly CustomWebApplicationFactory factory = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task PublicSearch_FiltersPublishedJobsAndHidesPrivateEmployerData()
    {
        var references = await SeedReferencesAsync();
        var employer = await CreateEmployerAsync();
        var backend = await CreateAndPublishJobAsync(employer, references, "Senior Backend Engineer", "Build transactional systems",
            "FullTime", "OnSite", references.CityAId, [references.SkillAId], 100m, 200m);
        await Task.Delay(5);
        var design = await CreateAndPublishJobAsync(employer, references, "Product Designer", "Shape accessible experiences",
            "PartTime", "Remote", references.CityBId, [references.SkillAId], 200m, 400m);
        var draft = await CreateJobAsync(employer, references, "Private Draft Position", "Never public", "FullTime", "Hybrid",
            references.CityAId, [references.SkillBId], null, null);
        var closed = await CreateAndPublishJobAsync(employer, references, "Closed Position", "Already closed", "Contract", "Hybrid",
            references.CityAId, [references.SkillBId], null, null);
        (await employer.PostAsync($"{EmployerJobsEndpoint}/{closed}/close", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var unpublished = await CreateAndPublishJobAsync(employer, references, "Unpublished Position", "Taken down", "Temporary", "OnSite",
            references.CityAId, [references.SkillBId], null, null);
        (await employer.PostAsync($"{EmployerJobsEndpoint}/{unpublished}/unpublish", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await factory.CreateClient().GetAsync(SearchEndpoint);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(json);
        page.GetProperty("totalCount").GetInt32().Should().Be(2);
        page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("title").GetString())
            .Should().Contain(["Senior Backend Engineer", "Product Designer"])
            .And.NotContain(["Private Draft Position", "Closed Position", "Unpublished Position"]);

        foreach (var id in new[] { draft, closed, unpublished })
            (await factory.CreateClient().GetAsync($"{SearchEndpoint}/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var detailsResponse = await factory.CreateClient().GetAsync($"{SearchEndpoint}/{backend}");
        detailsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await detailsResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        details.GetProperty("title").GetString().Should().Be("Senior Backend Engineer");
        details.GetProperty("employer").GetProperty("companyName").GetString().Should().StartWith("Public Company");
        details.TryGetProperty("employerProfileId", out _).Should().BeFalse();
        details.TryGetProperty("userId", out _).Should().BeFalse();
        details.GetProperty("employer").TryGetProperty("contactEmail", out _).Should().BeFalse();
        details.GetProperty("employer").TryGetProperty("contactPhone", out _).Should().BeFalse();
        details.GetProperty("employer").TryGetProperty("address", out _).Should().BeFalse();
        (await factory.CreateClient().GetAsync($"{SearchEndpoint}/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var anonymousClient = factory.CreateClient();
        anonymousClient.DefaultRequestHeaders.Authorization.Should().BeNull();
        (await anonymousClient.GetAsync($"{SearchEndpoint}/{design}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublicSearch_KeywordCategoryAndAllSelectedSkillsFilterAtDatabase()
    {
        var references = await SeedReferencesAsync();
        var employer = await CreateEmployerAsync();
        await CreateAndPublishJobAsync(employer, references, "Backend Engineer", "Build financial ledger systems",
            "FullTime", "OnSite", references.CityAId, [references.SkillAId, references.SkillBId], 100m, 300m);
        await CreateAndPublishJobAsync(employer, references, "Experience Designer", "Design inclusive products",
            "PartTime", "Remote", references.CityBId, [references.SkillAId], 200m, 400m);

        (await SearchAsync("q=Backend")).GetProperty("totalCount").GetInt32().Should().Be(1);
        (await SearchAsync("q=financial%20ledger")).GetProperty("totalCount").GetInt32().Should().Be(1);
        (await SearchAsync("q=no-such-role")).GetProperty("totalCount").GetInt32().Should().Be(0);
        (await SearchAsync($"categoryId={references.CategoryAId}")).GetProperty("totalCount").GetInt32().Should().Be(1);
        (await SearchAsync($"skillIds={references.SkillAId}")).GetProperty("totalCount").GetInt32().Should().Be(2);
        (await SearchAsync($"skillIds={references.SkillAId}&skillIds={references.SkillBId}")).GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task PublicSearch_FiltersHierarchicalLocationsAndRejectsInvalidCombinations()
    {
        var references = await SeedReferencesAsync();
        var employer = await CreateEmployerAsync();
        await CreateAndPublishJobAsync(employer, references, "First Location Job", "First city",
            "FullTime", "OnSite", references.CityAId, [references.SkillAId], null, null);
        await CreateAndPublishJobAsync(employer, references, "Second Location Job", "Second city",
            "FullTime", "OnSite", references.CityBId, [references.SkillAId], null, null);

        (await SearchAsync($"countryId={references.CountryAId}")).GetProperty("totalCount").GetInt32().Should().Be(1);
        (await SearchAsync($"provinceId={references.ProvinceAId}")).GetProperty("totalCount").GetInt32().Should().Be(1);
        (await SearchAsync($"cityId={references.CityAId}")).GetProperty("totalCount").GetInt32().Should().Be(1);
        (await SearchAsync($"countryId={references.CountryAId}&provinceId={references.ProvinceAId}&cityId={references.CityAId}"))
            .GetProperty("totalCount").GetInt32().Should().Be(1);
        (await factory.CreateClient().GetAsync($"{SearchEndpoint}?countryId={references.CountryAId}&cityId={references.CityBId}"))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await factory.CreateClient().GetAsync($"{SearchEndpoint}?provinceId={Guid.NewGuid()}"))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await factory.CreateClient().GetAsync($"{SearchEndpoint}?countryId={Guid.NewGuid()}"))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await factory.CreateClient().GetAsync($"{SearchEndpoint}?cityId={Guid.NewGuid()}"))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PublicSearch_FiltersEnumsSalarySortsAndPaginates()
    {
        var references = await SeedReferencesAsync();
        var employer = await CreateEmployerAsync();
        var jobs = new[]
        {
            ("FullTime", "OnSite", 100m, 200m),
            ("PartTime", "Hybrid", 200m, 300m),
            ("Contract", "Remote", 300m, 400m),
            ("Internship", "OnSite", 400m, 500m),
            ("Temporary", "Hybrid", 500m, 600m)
        };
        foreach (var (employment, arrangement, min, max) in jobs)
        {
            await CreateAndPublishJobAsync(employer, references, $"Listing {employment}", "Common searchable description",
                employment, arrangement, references.CityAId, [references.SkillAId], min, max);
            await Task.Delay(5);
        }

        foreach (var employment in jobs.Select(job => job.Item1))
            (await SearchAsync($"employmentType={employment}")).GetProperty("totalCount").GetInt32().Should().Be(1);
        foreach (var arrangement in new[] { "OnSite", "Hybrid", "Remote" })
            (await SearchAsync($"workArrangement={arrangement}")).GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);

        var firstPage = await SearchAsync("page=1&pageSize=2&sort=Oldest");
        var secondPage = await SearchAsync("page=2&pageSize=2&sort=Oldest");
        firstPage.GetProperty("totalCount").GetInt32().Should().Be(5);
        firstPage.GetProperty("totalPages").GetInt32().Should().Be(3);
        firstPage.GetProperty("items").GetArrayLength().Should().Be(2);
        secondPage.GetProperty("page").GetInt32().Should().Be(2);
        secondPage.GetProperty("items").GetArrayLength().Should().Be(2);
        (await SearchAsync("sort=Newest")).GetProperty("items").GetArrayLength().Should().Be(5);

        var ascending = await SearchAsync("sort=SalaryAscending&salaryCurrency=IRR");
        ascending.GetProperty("items")[0].GetProperty("salaryMinimum").GetDecimal().Should().Be(100m);
        var descending = await SearchAsync("sort=SalaryDescending&salaryCurrency=IRR");
        descending.GetProperty("items")[0].GetProperty("salaryMaximum").GetDecimal().Should().Be(600m);
        (await SearchAsync("salaryMinimum=250&salaryCurrency=IRR")).GetProperty("totalCount").GetInt32().Should().Be(4);
        (await SearchAsync("salaryMaximum=250&salaryCurrency=IRR")).GetProperty("totalCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task PublicSearch_RejectsInvalidEnumsReferencesPaginationSortAndSalary()
    {
        await factory.InitializeDatabaseAsync();
        var invalidQueries = new[]
        {
            $"categoryId={Guid.NewGuid()}",
            $"skillIds={Guid.NewGuid()}",
            "workArrangement=Teleport",
            "employmentType=Volunteer",
            "page=0",
            "pageSize=101",
            "page=2147483647&pageSize=100",
            "sort=TitleDescending",
            "salaryMinimum=-1&salaryCurrency=IRR",
            "salaryMinimum=200&salaryMaximum=100&salaryCurrency=IRR",
            "salaryMinimum=100",
            "sort=SalaryAscending"
        };

        foreach (var query in invalidQueries)
            (await factory.CreateClient().GetAsync($"{SearchEndpoint}?{query}")).StatusCode.Should().Be(HttpStatusCode.BadRequest, query);
    }

    private async Task<JsonElement> SearchAsync(string query)
    {
        var response = await factory.CreateClient().GetAsync($"{SearchEndpoint}?{query}");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private async Task<ReferenceIds> SeedReferencesAsync()
    {
        await factory.InitializeDatabaseAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReferenceDataDbContext>();
        var countryA = Country.Create($"Country A {Guid.NewGuid():N}", UniqueCountryCode());
        var countryB = Country.Create($"Country B {Guid.NewGuid():N}", UniqueCountryCode());
        db.Countries.AddRange(countryA, countryB);
        var provinceA = Province.Create(countryA.Id, $"Province A {Guid.NewGuid():N}");
        var provinceB = Province.Create(countryB.Id, $"Province B {Guid.NewGuid():N}");
        db.Provinces.AddRange(provinceA, provinceB);
        var cityA = City.Create(provinceA.Id, $"City A {Guid.NewGuid():N}");
        var cityB = City.Create(provinceB.Id, $"City B {Guid.NewGuid():N}");
        var categoryA = JobCategory.Create($"Category A {Guid.NewGuid():N}", null);
        var categoryB = JobCategory.Create($"Category B {Guid.NewGuid():N}", null);
        var skillA = Skill.Create($"Skill A {Guid.NewGuid():N}", null);
        var skillB = Skill.Create($"Skill B {Guid.NewGuid():N}", null);
        db.Cities.AddRange(cityA, cityB);
        db.JobCategories.AddRange(categoryA, categoryB);
        db.Skills.AddRange(skillA, skillB);
        await db.SaveChangesAsync();
        return new ReferenceIds(countryA.Id, provinceA.Id, cityA.Id, countryB.Id, provinceB.Id, cityB.Id,
            categoryA.Id, categoryB.Id, skillA.Id, skillB.Id);
    }

    private async Task<HttpClient> CreateEmployerAsync()
    {
        var client = await CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync(EmployerProfileEndpoint, new
        {
            companyName = $"Public Company {Guid.NewGuid():N}",
            industry = "Technology",
            companySize = "11-50",
            city = "Tehran",
            province = "Tehran",
            companyDescription = "A public company profile description.",
            websiteUrl = "https://company.example",
            contactEmail = "private@example.test",
            contactPhone = "+989120000000",
            address = "Private address"
        }, json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return client;
    }

    private async Task<Guid> CreateAndPublishJobAsync(
        HttpClient employer,
        ReferenceIds references,
        string title,
        string description,
        string employmentType,
        string workArrangement,
        Guid cityId,
        Guid[] skillIds,
        decimal? salaryMinimum,
        decimal? salaryMaximum)
    {
        var id = await CreateJobAsync(employer, references, title, description, employmentType, workArrangement,
            cityId, skillIds, salaryMinimum, salaryMaximum);
        (await employer.PostAsync($"{EmployerJobsEndpoint}/{id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        return id;
    }

    private async Task<Guid> CreateJobAsync(
        HttpClient employer,
        ReferenceIds references,
        string title,
        string description,
        string employmentType,
        string workArrangement,
        Guid cityId,
        Guid[] skillIds,
        decimal? salaryMinimum,
        decimal? salaryMaximum)
    {
        var hasSalary = salaryMinimum.HasValue || salaryMaximum.HasValue;
        var response = await employer.PostAsJsonAsync(EmployerJobsEndpoint, new
        {
            title,
            description,
            categoryId = title.Contains("Designer", StringComparison.Ordinal) ? references.CategoryBId : references.CategoryAId,
            skillIds,
            employmentType,
            workArrangement,
            cityId,
            salaryMinimum,
            salaryMaximum,
            salaryCurrency = hasSalary ? "IRR" : null,
            salaryPeriod = hasSalary ? "Monthly" : null,
            closingAt = (DateTimeOffset?)null
        }, json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(json);
        return created.GetProperty("id").GetGuid();
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        await factory.InitializeDatabaseAsync();
        var number = Interlocked.Increment(ref userCounter);
        var email = $"public-search-{number}@test.ir";
        var client = factory.CreateClient();
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            firstName = "Search",
            lastName = "Employer",
            email,
            phoneNumber = $"0977{number:D7}",
            password = "Passw0rd!",
            role = "Employer"
        }, json);
        registration.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = email, password = "Passw0rd!" }, json);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static string UniqueCountryCode() => new string(Guid.NewGuid().ToByteArray()
        .Take(3).Select(value => (char)('A' + value % 26)).ToArray());

    private sealed record ReferenceIds(
        Guid CountryAId, Guid ProvinceAId, Guid CityAId,
        Guid CountryBId, Guid ProvinceBId, Guid CityBId,
        Guid CategoryAId, Guid CategoryBId, Guid SkillAId, Guid SkillBId);
}
