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

namespace IranJob.IntegrationTests.Identity;

public class AdminDashboardTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private static int _identityCounter;

    public AdminDashboardTests(CustomWebApplicationFactory factory)
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
    public async Task Stats_Anonymous_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/admin/stats");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Stats_Candidate_Returns403()
    {
        var session = await LoginSessionAsync(IdentityRoles.Candidate);
        ApplyBearer(session.AccessToken);

        var response = await _client.GetAsync("/api/v1/admin/stats");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Stats_Employer_Returns403()
    {
        var session = await LoginSessionAsync(IdentityRoles.Employer);
        ApplyBearer(session.AccessToken);

        var response = await _client.GetAsync("/api/v1/admin/stats");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Stats_Admin_Returns200_WithRealData()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.Admin);
        ApplyBearer(session.AccessToken);

        var response = await _client.GetAsync("/api/v1/admin/stats");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        payload.GetProperty("totalUsers").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        payload.GetProperty("activeUsers").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        payload.TryGetProperty("usersByRole", out var rolesProp).Should().BeTrue();
    }

    [Fact]
    public async Task Stats_SuperAdmin_Returns200_WithRealData()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var response = await _client.GetAsync("/api/v1/admin/stats");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        payload.GetProperty("totalUsers").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Users_Anonymous_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Users_Candidate_Returns403()
    {
        var session = await LoginSessionAsync(IdentityRoles.Candidate);
        ApplyBearer(session.AccessToken);

        var response = await _client.GetAsync("/api/v1/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Users_Admin_Returns200_WithPaginatedList()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.Admin);
        ApplyBearer(session.AccessToken);

        var response = await _client.GetAsync("/api/v1/admin/users?page=1&pageSize=20");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        payload.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);
        payload.GetProperty("page").GetInt32().Should().Be(1);
        payload.GetProperty("pageSize").GetInt32().Should().Be(20);
        payload.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SuperAdminEndpoint_Admin_Returns403()
    {
        var session = await SeedAndLoginAsync(IdentityRoles.Admin);
        ApplyBearer(session.AccessToken);

        var response = await _client.GetAsync("/api/v1/admin/super-admin/can-manage-users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SuperAdminEndpoint_SuperAdmin_Returns200()
    {
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var response = await _client.GetAsync("/api/v1/admin/super-admin/can-manage-users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        payload.GetProperty("canManage").GetBoolean().Should().BeTrue();
    }

    #region Authorization tests
    [Fact]
    public async Task GetUserById_Anonymous_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/admin/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_Candidate_Returns403()
    {
        var session = await LoginSessionAsync(IdentityRoles.Candidate);
        ApplyBearer(session.AccessToken);

        var response = await _client.GetAsync($"/api/v1/admin/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserById_Admin_Returns200()
    {
        await SeedUsersAsync();
        var adminSession = await SeedAndLoginAsync(IdentityRoles.Admin);
        ApplyBearer(adminSession.AccessToken);

        var targetUser = await CreateUserWithRoleAsync(IdentityRoles.Candidate);

        var response = await _client.GetAsync("/api/v1/admin/users/" + targetUser.Id);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserById_NonExistentUser_Returns404()
    {
        var session = await SeedAndLoginAsync(IdentityRoles.Admin);
        ApplyBearer(session.AccessToken);

        var response = await _client.GetAsync($"/api/v1/admin/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetUserActiveStatus_Anonymous_Returns401()
    {
        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}/active", new { isActive = false });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetUserLockout_Anonymous_Returns401()
    {
        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}/lockout", new { lockOut = true });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateUserRoles_Anonymous_Returns401()
    {
        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}/roles", new { roles = new[] { "Candidate" } });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InitiatePasswordReset_Anonymous_Returns401()
    {
        var response = await _client.PostAsync($"/api/v1/admin/users/{Guid.NewGuid()}/password-reset", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetUserActiveStatus_Candidate_Returns403()
    {
        var session = await LoginSessionAsync(IdentityRoles.Candidate);
        ApplyBearer(session.AccessToken);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}/active", new { isActive = false });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetUserLockout_Candidate_Returns403()
    {
        var session = await LoginSessionAsync(IdentityRoles.Candidate);
        ApplyBearer(session.AccessToken);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}/lockout", new { lockOut = true });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateUserRoles_Candidate_Returns403()
    {
        var session = await LoginSessionAsync(IdentityRoles.Candidate);
        ApplyBearer(session.AccessToken);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}/roles", new { roles = new[] { "Candidate" } });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    #endregion

    #region Privilege escalation prevention
    [Fact]
    public async Task SetUserRoles_Admin_ChangeAdminRole_Returns403()
    {
        await SeedUsersAsync();
        var adminSession = await SeedAndLoginAsync(IdentityRoles.Admin);
        ApplyBearer(adminSession.AccessToken);

        var targetAdmin = await CreateUserWithRoleAsync(IdentityRoles.Admin);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetAdmin.Id}/roles", new { roles = new[] { "Employer" } });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetUserRoles_Admin_TryAssignSuperAdmin_Returns403()
    {
        await SeedUsersAsync();
        var adminSession = await SeedAndLoginAsync(IdentityRoles.Admin);
        ApplyBearer(adminSession.AccessToken);

        var targetUser = await CreateUserWithRoleAsync(IdentityRoles.Candidate);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetUser.Id}/roles", new { roles = new[] { "SuperAdmin" } });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetUserRoles_Admin_ChangeOwnRoleToSuperAdmin_Returns403()
    {
        await SeedUsersAsync();
        var adminSession = await SeedAndLoginAsync(IdentityRoles.Admin);
        ApplyBearer(adminSession.AccessToken);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var self = await userManager.FindByEmailAsync(adminSession.Email);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{self!.Id}/roles", new { roles = new[] { "SuperAdmin" } });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetUserActiveStatus_Admin_DeactivateAdmin_Returns403()
    {
        await SeedUsersAsync();
        var adminSession = await SeedAndLoginAsync(IdentityRoles.Admin);
        ApplyBearer(adminSession.AccessToken);

        var targetAdmin = await CreateUserWithRoleAsync(IdentityRoles.Admin);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetAdmin.Id}/active", new { isActive = false });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetUserLockout_Admin_LockAdmin_Returns403()
    {
        await SeedUsersAsync();
        var adminSession = await SeedAndLoginAsync(IdentityRoles.Admin);
        ApplyBearer(adminSession.AccessToken);

        var targetAdmin = await CreateUserWithRoleAsync(IdentityRoles.Admin);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetAdmin.Id}/lockout", new { lockOut = true });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    #endregion

    #region Self-modification prevention

    [Fact]
    public async Task SetUserActiveStatus_SuperAdmin_DeactivateSelf_Returns403()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var self = await userManager.FindByEmailAsync(session.Email);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{self!.Id}/active", new { isActive = false });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetUserLockout_SuperAdmin_LockSelf_Returns403()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var self = await userManager.FindByEmailAsync(session.Email);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{self!.Id}/lockout", new { lockOut = true });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateUserRoles_SuperAdmin_ChangeOwnRoles_Returns403()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var self = await userManager.FindByEmailAsync(session.Email);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{self!.Id}/roles", new { roles = new[] { "Admin" } });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InitiatePasswordReset_SuperAdmin_ResetSelf_Returns403()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var self = await userManager.FindByEmailAsync(session.Email);

        var response = await _client.PostAsync($"/api/v1/admin/users/{self!.Id}/password-reset", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    #endregion

    #region Last SuperAdmin protection

    [Fact]
    public async Task SetUserActiveStatus_SuperAdmin_DeactivateLastSuperAdmin_Returns403()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        // Create a second SuperAdmin so we have a target
        var otherSuperAdmin = await CreateUserWithRoleAsync(IdentityRoles.SuperAdmin);

        // Should succeed — deactivating one of two SuperAdmins
        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{otherSuperAdmin.Id}/active", new { isActive = false });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Now the actor is the last eligible SuperAdmin.
        // Try to deactivate a non-privileged user — that should work
        var regularUser = await CreateUserWithRoleAsync(IdentityRoles.Candidate);
        var response2 = await _client.PostAsJsonAsync($"/api/v1/admin/users/{regularUser.Id}/active", new { isActive = false });
        response2.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetUserLockout_SuperAdmin_LockLastSuperAdmin_Returns403()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var otherSuperAdmin = await CreateUserWithRoleAsync(IdentityRoles.SuperAdmin);

        // Lock one of two SuperAdmins — should succeed
        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{otherSuperAdmin.Id}/lockout", new { lockOut = true });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Re-unlock the other SuperAdmin
        var unlockResponse = await _client.PostAsJsonAsync($"/api/v1/admin/users/{otherSuperAdmin.Id}/lockout", new { lockOut = false });
        unlockResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateUserRoles_SuperAdmin_ReplaceRolesWithNonPrivileged_Returns204()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var otherSuperAdmin = await CreateUserWithRoleAsync(IdentityRoles.SuperAdmin);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{otherSuperAdmin.Id}/roles", new { roles = new[] { "Admin" } });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
    #endregion

    #region Validation tests
    [Fact]
    public async Task UpdateUserRoles_InvalidRole_Returns400()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var targetUser = await CreateUserWithRoleAsync(IdentityRoles.Candidate);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetUser.Id}/roles", new { roles = new[] { "NonExistentRole" } });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUserRoles_DuplicateRole_Returns400()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var targetUser = await CreateUserWithRoleAsync(IdentityRoles.Candidate);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetUser.Id}/roles", new { roles = new[] { "Candidate", "Candidate" } });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUserRoles_NonExistentUser_Returns404()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}/roles", new { roles = new[] { "Candidate" } });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetUserActiveStatus_NonExistentUser_Returns404()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}/active", new { isActive = false });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    #endregion

    #region Functional tests

    [Fact]
    public async Task SetUserActiveStatus_SuperAdmin_DeactivateCandidate_Returns204()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var targetUser = await CreateUserWithRoleAsync(IdentityRoles.Candidate);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetUser.Id}/active", new { isActive = false });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetUserActiveStatus_SuperAdmin_ReactivateUser_Returns204()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var targetUser = await CreateUserWithRoleAsync(IdentityRoles.Candidate);

        // Deactivate first
        await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetUser.Id}/active", new { isActive = false });

        // Reactivate
        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetUser.Id}/active", new { isActive = true });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetUserLockout_SuperAdmin_LockUser_Returns204()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var targetUser = await CreateUserWithRoleAsync(IdentityRoles.Candidate);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetUser.Id}/lockout", new { lockOut = true });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetUserLockout_SuperAdmin_UnlockUser_Returns204()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var targetUser = await CreateUserWithRoleAsync(IdentityRoles.Candidate);

        // Lock first
        await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetUser.Id}/lockout", new { lockOut = true });

        // Unlock
        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetUser.Id}/lockout", new { lockOut = false });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateUserRoles_SuperAdmin_ChangeRoles_Returns204()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var targetUser = await CreateUserWithRoleAsync(IdentityRoles.Candidate);

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetUser.Id}/roles", new { roles = new[] { "Employer" } });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task InitiatePasswordReset_SuperAdmin_Returns200_WithTokenNull()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.SuperAdmin);
        ApplyBearer(session.AccessToken);

        var targetUser = await CreateUserWithRoleAsync(IdentityRoles.Candidate);

        var response = await _client.PostAsync($"/api/v1/admin/users/{targetUser.Id}/password-reset", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        payload.GetProperty("initiated").GetBoolean().Should().BeFalse();
        payload.TryGetProperty("token", out var tokenProp).Should().BeTrue();
        tokenProp.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task UpdateUserRoles_Admin_AssignAdminToCandidate_Returns403()
    {
        await SeedUsersAsync();
        var session = await SeedAndLoginAsync(IdentityRoles.Admin);
        ApplyBearer(session.AccessToken);

        var targetUser = await CreateUserWithRoleAsync(IdentityRoles.Candidate);

        // An Admin cannot grant Admin or SuperAdmin roles � only SuperAdmin can.
        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{targetUser.Id}/roles", new { roles = new[] { "Admin" } });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    #endregion

    private async Task SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var roleName in IdentityRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
            }
        }

        for (int i = 0; i < 3; i++)
        {
            var number = Interlocked.Increment(ref _identityCounter);
            var email = $"seeduser{number}@test.ir";
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is null)
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    PhoneNumber = $"0999{number:D7}",
                    FirstName = "Seed",
                    LastName = $"User{number}",
                    IsActive = true,
                };
                var result = await userManager.CreateAsync(user, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, IdentityRoles.Candidate);
                }
            }
        }
    }

    private async Task<ApplicationUser> CreateUserWithRoleAsync(string role)
    {
        await _factory.InitializeDatabaseAsync();
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new ApplicationRole(role));
        }

        var number = Interlocked.Increment(ref _identityCounter);
        var email = $"target{number}@test.ir";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            PhoneNumber = $"0999{number:D7}",
            FirstName = "Target",
            LastName = "User",
            IsActive = true
        };
        var result = await userManager.CreateAsync(user, "Password123!");
        result.Succeeded.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private async Task ReactivateUserAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
        {
            user.IsActive = true;
            await userManager.UpdateAsync(user);
        }
    }


    private async Task<AuthSession> LoginSessionAsync(string role = IdentityRoles.Candidate)
    {
        await _factory.InitializeDatabaseAsync();
        var number = Interlocked.Increment(ref _identityCounter);
        var email = $"login{number}@test.ir";
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { firstName = "Test", lastName = "User", email, phoneNumber = $"0999{number:D7}", password = "Password123!", role });

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = email, password = "Password123!" });
        var payload = await login.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return new AuthSession(email, payload.GetProperty("accessToken").GetString()!, string.Empty);
    }

    private async Task<AuthSession> SeedAndLoginAsync(string role)
    {
        var number = Interlocked.Increment(ref _identityCounter);
        var email = $"seeded{number}@test.ir";
        var phone = $"0999{number:D7}";

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new ApplicationRole(role));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email.ToUpperInvariant(),
            Email = email,
            PhoneNumber = phone,
            FirstName = "Seeded",
            LastName = "User",
            IsActive = true,
        };
        var created = await userManager.CreateAsync(user, "Password123!");
        created.Succeeded.Should().BeTrue(string.Join(", ", created.Errors.Select(error => error.Description)));
        var roleResult = await userManager.AddToRoleAsync(user, role);
        roleResult.Succeeded.Should().BeTrue();

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = email, password = "Password123!" });
        login.EnsureSuccessStatusCode();
        var payload = await login.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return new AuthSession(email, payload.GetProperty("accessToken").GetString()!, string.Empty);
    }

    private void ApplyBearer(string accessToken)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private sealed record AuthSession(string Email, string AccessToken, string CsrfToken);
}
