using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.Identity.Domain.Entities;
using IranJob.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace IranJob.IntegrationTests;

public sealed class ReferenceDataTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string CategoriesEndpoint = "/api/v1/job-categories";
    private const string SkillsEndpoint = "/api/v1/skills";
    private const string LocationsEndpoint = "/api/v1/locations";
    private readonly CustomWebApplicationFactory factory;
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private static int identityCounter;

    public ReferenceDataTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Categories_ArePubliclyReadable_AndUnknownIdReturnsNotFound()
    {
        await factory.InitializeDatabaseAsync();
        var client = factory.CreateClient();

        (await client.GetAsync(CategoriesEndpoint)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"{CategoriesEndpoint}/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CategoryMutation_RequiresAdminRole()
    {
        await factory.InitializeDatabaseAsync();
        var anonymousClient = factory.CreateClient();
        var input = new { name = UniqueName("Category"), description = "Test category" };

        (await anonymousClient.PostAsJsonAsync(CategoriesEndpoint, input, json)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var candidateClient = await CreateAuthenticatedClientAsync("Candidate");
        (await candidateClient.PostAsJsonAsync(CategoriesEndpoint, input, json)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Categories_AdminCanCreateAndDuplicateNamesAreRejected()
    {
        var client = await CreateAuthenticatedClientAsync(IdentityRoles.Admin);
        var name = UniqueName("Category");
        var response = await client.PostAsJsonAsync(CategoriesEndpoint, new { name, description = "Engineering" }, json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(json);
        created.GetProperty("name").GetString().Should().Be(name);
        var duplicate = await client.PostAsJsonAsync(CategoriesEndpoint, new { name = name.ToUpperInvariant(), description = (string?)null }, json);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Categories_RejectInvalidInput_AndDeactivationHidesItFromPublicReads()
    {
        var client = await CreateAuthenticatedClientAsync(IdentityRoles.SuperAdmin);
        var invalid = await client.PostAsJsonAsync(CategoriesEndpoint, new { name = " ", description = (string?)null }, json);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var name = UniqueName("InactiveCategory");
        var create = await client.PostAsJsonAsync(CategoriesEndpoint, new { name, description = (string?)null }, json);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>(json);
        var id = created.GetProperty("id").GetGuid();

        (await client.DeleteAsync($"{CategoriesEndpoint}/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await factory.CreateClient().GetAsync($"{CategoriesEndpoint}/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Skills_ArePubliclyReadableAndAdminManaged()
    {
        await factory.InitializeDatabaseAsync();
        var anonymousClient = factory.CreateClient();
        (await anonymousClient.GetAsync(SkillsEndpoint)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await anonymousClient.GetAsync($"{SkillsEndpoint}/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await anonymousClient.PostAsJsonAsync(SkillsEndpoint, new { name = UniqueName("AnonymousSkill"), description = (string?)null }, json))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var admin = await CreateAuthenticatedClientAsync(IdentityRoles.Admin);
        var name = UniqueName("Skill");
        var response = await admin.PostAsJsonAsync(SkillsEndpoint, new { name, description = "A test skill" }, json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(json);
        var skillId = created.GetProperty("id").GetGuid();

        var updatedName = UniqueName("UpdatedSkill");
        var update = await admin.PutAsJsonAsync($"{SkillsEndpoint}/{skillId}", new { name = updatedName, description = "Updated" }, json);
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>(json);
        updated.GetProperty("name").GetString().Should().Be(updatedName);

        var duplicate = await admin.PostAsJsonAsync(SkillsEndpoint, new { name = updatedName, description = (string?)null }, json);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var invalid = await admin.PostAsJsonAsync(SkillsEndpoint, new { name = "", description = (string?)null }, json);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Locations_RequireAValidCountryProvinceHierarchy()
    {
        var admin = await CreateAuthenticatedClientAsync(IdentityRoles.Admin);
        var anonymousCountryWrite = await factory.CreateClient().PostAsJsonAsync(
            $"{LocationsEndpoint}/countries", new { name = UniqueName("AnonymousCountry"), code = UniqueCode() }, json);
        anonymousCountryWrite.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var countryName = UniqueName("Country");
        var countryResponse = await admin.PostAsJsonAsync($"{LocationsEndpoint}/countries", new { name = countryName, code = UniqueCode() }, json);
        countryResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var country = await countryResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        var countryId = country.GetProperty("id").GetGuid();

        var invalidProvince = await admin.PostAsJsonAsync($"{LocationsEndpoint}/provinces", new { countryId = Guid.NewGuid(), name = UniqueName("Province") }, json);
        invalidProvince.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var provinceResponse = await admin.PostAsJsonAsync($"{LocationsEndpoint}/provinces", new { countryId, name = UniqueName("Province") }, json);
        provinceResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var province = await provinceResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        var provinceId = province.GetProperty("id").GetGuid();

        var cityResponse = await admin.PostAsJsonAsync($"{LocationsEndpoint}/cities", new { provinceId, name = UniqueName("City") }, json);
        cityResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var city = await cityResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        var cityId = city.GetProperty("id").GetGuid();

        (await factory.CreateClient().GetAsync($"{LocationsEndpoint}/{cityId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await factory.CreateClient().GetAsync($"{LocationsEndpoint}/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.DeleteAsync($"{LocationsEndpoint}/countries/{countryId}")).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Locations_PublicQueriesFilterByParentAndCountryCodeIsUnique()
    {
        var admin = await CreateAuthenticatedClientAsync(IdentityRoles.SuperAdmin);
        var code = UniqueCode();
        var countryResponse = await admin.PostAsJsonAsync($"{LocationsEndpoint}/countries", new { name = UniqueName("Country"), code }, json);
        countryResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var country = await countryResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        var countryId = country.GetProperty("id").GetGuid();

        var duplicateCode = await admin.PostAsJsonAsync($"{LocationsEndpoint}/countries", new { name = UniqueName("OtherCountry"), code = code.ToLowerInvariant() }, json);
        duplicateCode.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var provinceResponse = await admin.PostAsJsonAsync($"{LocationsEndpoint}/provinces", new { countryId, name = UniqueName("Province") }, json);
        var province = await provinceResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        var provinceId = province.GetProperty("id").GetGuid();
        var cityResponse = await admin.PostAsJsonAsync($"{LocationsEndpoint}/cities", new { provinceId, name = UniqueName("City") }, json);
        cityResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var city = await cityResponse.Content.ReadFromJsonAsync<JsonElement>(json);
        var cityId = city.GetProperty("id").GetGuid();
        var cityUpdate = await admin.PutAsJsonAsync($"{LocationsEndpoint}/cities/{cityId}", new { provinceId, name = UniqueName("UpdatedCity") }, json);
        cityUpdate.StatusCode.Should().Be(HttpStatusCode.OK);

        (await factory.CreateClient().GetAsync($"{LocationsEndpoint}/countries")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await factory.CreateClient().GetAsync($"{LocationsEndpoint}/provinces?countryId={countryId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await factory.CreateClient().GetAsync($"{LocationsEndpoint}/cities?provinceId={provinceId}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string role)
    {
        await factory.InitializeDatabaseAsync();
        var number = Interlocked.Increment(ref identityCounter);
        var email = $"reference-data-{number}@test.ir";
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                PhoneNumber = $"0999{number:D7}",
                FirstName = "Reference",
                LastName = "Data"
            };
            (await userManager.CreateAsync(user, "Passw0rd!")).Succeeded.Should().BeTrue();
            (await userManager.AddToRoleAsync(user, role)).Succeeded.Should().BeTrue();
        }

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = email, password = "Passw0rd!" }, json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";
    private static string UniqueCode() => new string(Guid.NewGuid().ToByteArray()
        .Take(3).Select(value => (char)('A' + value % 26)).ToArray());
}
