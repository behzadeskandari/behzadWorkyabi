using FluentAssertions;
using IranJob.Modules.Identity.Application.Abstractions;
using IranJob.Modules.Identity.Domain.Entities;
using IranJob.Modules.Identity.Infrastructure.Configuration;
using IranJob.Modules.Identity.Infrastructure.Persistence;
using IranJob.Modules.Identity.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IranJob.UnitTests.Identity;

public sealed class RefreshTokenServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IdentityDbContext _dbContext;
    private readonly RefreshTokenService _service;
    private readonly RecordingAuditService _audit = new();
    private readonly Guid _userId = Guid.NewGuid();

    public RefreshTokenServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new IdentityDbContext(options);
        _dbContext.Database.EnsureCreated();
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = _userId,
            UserName = "user@example.com",
            NormalizedUserName = "USER@EXAMPLE.COM",
            Email = "user@example.com",
            NormalizedEmail = "USER@EXAMPLE.COM",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "09120000000",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        _dbContext.SaveChanges();

        _service = new RefreshTokenService(
            _dbContext,
            Options.Create(new IdentitySecurityOptions { RefreshTokenExpirationDays = 7 }),
            _audit);
    }

    [Fact]
    public async Task ValidRefresh_RotatesToken()
    {
        var created = await _service.CreateAsync(_userId, "127.0.0.1");
        var rotated = await _service.RotateAsync(created.RawToken, "127.0.0.1");

        rotated.Should().NotBeNull();
        rotated!.Value.RawToken.Should().NotBe(created.RawToken);
        created.Entity.IsRevoked.Should().BeTrue();
        created.Entity.RevocationReason.Should().Be("Rotated");
    }

    [Fact]
    public async Task ExpiredRefresh_IsRejected()
    {
        var created = await _service.CreateAsync(_userId, "127.0.0.1");
        created.Entity.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await _dbContext.SaveChangesAsync();

        var rotated = await _service.RotateAsync(created.RawToken, "127.0.0.1");

        rotated.Should().BeNull();
    }

    [Fact]
    public async Task RevokedRefresh_CannotBeUsed()
    {
        var created = await _service.CreateAsync(_userId, "127.0.0.1");
        await _service.RevokeAsync(created.RawToken, "127.0.0.1", "Logout");

        var rotated = await _service.RotateAsync(created.RawToken, "127.0.0.1");

        rotated.Should().BeNull();
        _audit.Events.Should().Contain("RefreshTokenReuseDetected");
    }

    [Fact]
    public async Task OldToken_CannotBeReused_AfterRotation()
    {
        var created = await _service.CreateAsync(_userId, "127.0.0.1");
        var rotated = await _service.RotateAsync(created.RawToken, "127.0.0.1");
        rotated.Should().NotBeNull();

        var reused = await _service.RotateAsync(created.RawToken, "127.0.0.1");

        reused.Should().BeNull();
        _audit.Events.Should().Contain("RefreshTokenReuseDetected");
    }

    [Fact]
    public async Task ReuseDetection_RevokesReplacementFamily()
    {
        var first = await _service.CreateAsync(_userId, "127.0.0.1");
        var second = await _service.RotateAsync(first.RawToken, "127.0.0.1");
        second.Should().NotBeNull();

        await _service.RotateAsync(first.RawToken, "10.0.0.1");

        var replacement = await _dbContext.RefreshTokens.SingleAsync(token => token.Id == second!.Value.Token.Id);
        replacement.IsRevoked.Should().BeTrue();
        replacement.RevocationReason.Should().Be("Refresh token reuse detected");
    }

    [Fact]
    public async Task Logout_RevokesToken_AndPreventsRefresh()
    {
        var created = await _service.CreateAsync(_userId, "127.0.0.1");
        var revoked = await _service.RevokeAsync(created.RawToken, "127.0.0.1", "Logout");

        revoked.Should().BeTrue();
        (await _service.RotateAsync(created.RawToken, "127.0.0.1")).Should().BeNull();
    }

    [Fact]
    public async Task RawToken_IsNotPersisted()
    {
        var created = await _service.CreateAsync(_userId, "127.0.0.1");
        var stored = await _dbContext.RefreshTokens.SingleAsync(token => token.Id == created.Entity.Id);

        stored.TokenHash.Should().NotBe(created.RawToken);
        stored.TokenHash.Should().Be(RefreshTokenService.HashToken(created.RawToken));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private sealed class RecordingAuditService : IAuthenticationAuditService
    {
        public List<string> Events { get; } = [];

        public Task RecordAsync(
            string eventType,
            Guid? userId = null,
            string? metadata = null,
            CancellationToken cancellationToken = default)
        {
            Events.Add(eventType);
            return Task.CompletedTask;
        }
    }
}
