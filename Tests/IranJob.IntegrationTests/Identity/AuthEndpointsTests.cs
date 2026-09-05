using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.Identity.Domain.Entities;
using IranJob.Modules.Identity.Infrastructure.Persistence;
using IranJob.Modules.Identity.Presentation.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace IranJob.IntegrationTests.Identity;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Register_ValidCandidate_Returns204()
    {
        var response = await RegisterAsync(IdentityRoles.Candidate);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Register_ValidEmployer_Returns204()
    {
        var response = await RegisterAsync(IdentityRoles.Employer);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = UniqueEmail();
        var first = NewRegister(IdentityRoles.Candidate, email: email);
        var second = NewRegister(IdentityRoles.Employer, email: email);

        (await _client.PostAsJsonAsync("/api/v1/auth/register", first)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", second);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_DuplicatePhone_Returns409()
    {
        var phone = UniquePhone();
        var first = NewRegister(IdentityRoles.Candidate, phone: phone);
        var second = NewRegister(IdentityRoles.Employer, phone: phone);

        (await _client.PostAsJsonAsync("/api/v1/auth/register", first)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", second);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var request = NewRegister(IdentityRoles.Candidate, email: "not-an-email");
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_InvalidPhone_Returns400()
    {
        var request = NewRegister(IdentityRoles.Candidate, phone: "123456789");
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var request = NewRegister(IdentityRoles.Candidate, password: "weak");
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var request = NewRegister("Manager");
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(IdentityRoles.Admin)]
    [InlineData(IdentityRoles.SuperAdmin)]
    [InlineData(IdentityRoles.Recruiter)]
    public async Task Register_PrivilegedRole_Returns400(string role)
    {
        var response = await RegisterAsync(role);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidEmail_Returns200()
    {
        var email = UniqueEmail();
        await RegisterAsync(IdentityRoles.Candidate, email: email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(email, "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();
        payload.User.Email.Should().Be(email);
        payload.User.Roles.Should().Contain(IdentityRoles.Candidate);
        response.Headers.Contains("X-CSRF-TOKEN").Should().BeTrue();
    }

    [Fact]
    public async Task Login_ValidPhone_Returns200()
    {
        var phone = UniquePhone();
        var email = UniqueEmail();
        await RegisterAsync(IdentityRoles.Candidate, email: email, phone: phone);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(phone, "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        var email = UniqueEmail();
        await RegisterAsync(IdentityRoles.Candidate, email: email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(email, "WrongPassword123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonexistentAccount_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequestDto("missing@example.com", "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_InactiveAccount_Returns401()
    {
        var email = UniqueEmail();
        await RegisterAsync(IdentityRoles.Candidate, email: email);

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email);
            user!.IsActive = false;
            await users.UpdateAsync(user);
        }

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(email, "Password123!"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_LockedAccount_Returns401()
    {
        var email = UniqueEmail();
        await RegisterAsync(IdentityRoles.Candidate, email: email);

        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(email, "WrongPassword123!"));
        }

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(email, "Password123!"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_Anonymous_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_Authenticated_ReturnsProfile()
    {
        var session = await LoginSessionAsync();
        ApplyBearer(session.AccessToken);

        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>(JsonOptions);
        profile!.Email.Should().Be(session.Email);
        profile.Roles.Should().Contain(IdentityRoles.Candidate);
    }

    [Fact]
    public async Task Refresh_RotatesCookieAndIssuesNewAccessToken()
    {
        var session = await LoginSessionAsync();
        ApplyCsrf(session.CsrfToken);

        var response = await _client.PostAsync("/api/v1/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        payload!.AccessToken.Should().NotBe(session.AccessToken);
    }

    [Fact]
    public async Task Refresh_ReusingRotatedFamily_Returns401()
    {
        var session = await LoginSessionAsync();
        ApplyCsrf(session.CsrfToken);

        var first = await _client.PostAsync("/api/v1/auth/refresh", null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        _client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        ApplyCsrf(session.CsrfToken);
        var reused = await _client.PostAsync("/api/v1/auth/refresh", null);
        reused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var session = await LoginSessionAsync();
        ApplyCsrf(session.CsrfToken);

        var logout = await _client.PostAsync("/api/v1/auth/logout", null);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refresh = await _client.PostAsync("/api/v1/auth/refresh", null);
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminPing_Anonymous_Returns401()
    {
        (await _client.GetAsync("/api/v1/admin/ping")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminPing_Candidate_Returns403()
    {
        var session = await LoginSessionAsync(IdentityRoles.Candidate);
        ApplyBearer(session.AccessToken);
        (await _client.GetAsync("/api/v1/admin/ping")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminPing_Employer_Returns403()
    {
        var session = await LoginSessionAsync(IdentityRoles.Employer);
        ApplyBearer(session.AccessToken);
        (await _client.GetAsync("/api/v1/admin/ping")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminPing_Recruiter_Returns403()
    {
        var session = await SeedAndLoginAsync(IdentityRoles.Recruiter);
        ApplyBearer(session.AccessToken);
        (await _client.GetAsync("/api/v1/admin/ping")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminPing_Admin_Returns200()
    {
        var session = await SeedAndLoginAsync(IdentityRoles.Admin);
        ApplyBearer(session.AccessToken);
        (await _client.GetAsync("/api/v1/admin/ping")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminPing_SuperAdmin_Returns200()
    {
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);
        (await _client.GetAsync("/api/v1/admin/ping")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpResponseMessage> RegisterAsync(
        string role,
        string? email = null,
        string? phone = null,
        string password = "Password123!")
    {
        return await _client.PostAsJsonAsync("/api/v1/auth/register", NewRegister(role, email, phone, password));
    }

    private static RegisterRequestDto NewRegister(
        string role,
        string? email = null,
        string? phone = null,
        string password = "Password123!") =>
        new("Test", "User", email ?? UniqueEmail(), phone ?? UniquePhone(), password, role);

    private async Task<AuthSession> LoginSessionAsync(string role = IdentityRoles.Candidate)
    {
        var email = UniqueEmail();
        var register = await RegisterAsync(role, email: email);
        register.EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(email, "Password123!"));
        login.EnsureSuccessStatusCode();
        var payload = await login.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        login.Headers.TryGetValues("X-CSRF-TOKEN", out var csrfValues);
        return new AuthSession(email, payload!.AccessToken, csrfValues!.First());
    }

    private async Task<AuthSession> SeedAndLoginAsync(string role)
    {
        var email = UniqueEmail();
        var phone = UniquePhone();

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email.ToUpperInvariant(),
                Email = email,
                PhoneNumber = phone,
                FirstName = "Seeded",
                LastName = "User",
                IsActive = true
            };

            var created = await users.CreateAsync(user, "Password123!");
            created.Succeeded.Should().BeTrue(string.Join(", ", created.Errors.Select(error => error.Description)));
            var roleResult = await users.AddToRoleAsync(user, role);
            roleResult.Succeeded.Should().BeTrue();
        }

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(email, "Password123!"));
        login.EnsureSuccessStatusCode();
        var payload = await login.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        login.Headers.TryGetValues("X-CSRF-TOKEN", out var csrfValues);
        return new AuthSession(email, payload!.AccessToken, csrfValues!.First());
    }

    private void ApplyBearer(string accessToken)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private void ApplyCsrf(string csrfToken)
    {
        _client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        _client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfToken);
    }

    private static string UniqueEmail() => $"user{Guid.NewGuid():N}@example.com";

    private static string UniquePhone()
    {
        var suffix = Math.Abs(Guid.NewGuid().GetHashCode() % 1_000_000_000).ToString("D9");
        return $"09{suffix}";
    }

    private sealed record AuthSession(string Email, string AccessToken, string CsrfToken);
}
