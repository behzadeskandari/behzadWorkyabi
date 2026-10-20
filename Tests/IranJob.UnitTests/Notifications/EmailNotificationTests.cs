using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.Identity.Domain.Entities;
using IranJob.Modules.Identity.Infrastructure.Persistence;
using IranJob.Modules.Identity.Infrastructure.Services;
using IranJob.Modules.Notifications.Application.Contracts;
using IranJob.Modules.Notifications.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IranJob.UnitTests.Notifications;

/// <summary>
/// Covers the outbound-email plumbing added for application notifications:
/// the SMTP service is a safe no-op when disabled or mis-configured, and the
/// Identity email resolver returns the recipient's address (or null).
/// </summary>
public class EmailNotificationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IdentityDbContext> _options;

    public EmailNotificationTests()
    {
        // Shared-cache in-memory SQLite so the schema lives for the test lifetime.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new IdentityDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private static SmtpEmailService CreateSmtpService(EmailOptions options) =>
        new(Options.Create(options), NullLogger<SmtpEmailService>.Instance);

    [Fact]
    public async Task DisabledEmail_SkipsWithoutThrowingOrTouchingNetwork()
    {
        // Enabled=false, no host — must be a silent no-op (this is the default
        // configuration used by tests and local dev).
        var service = CreateSmtpService(new EmailOptions { Enabled = false });

        var act = async () => await service.SendAsync(
            new EmailMessage("someone@example.com", "Subject", "Body"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnabledEmail_MissingHostOrFrom_SkipsWithoutThrowing()
    {
        // Enabled but Host/FromAddress empty — must warn-and-skip, not attempt
        // a connection that would fail.
        var service = CreateSmtpService(new EmailOptions { Enabled = true, Host = "", FromAddress = "" });

        var act = async () => await service.SendAsync(
            new EmailMessage("someone@example.com", "Subject", "Body"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task NullMessage_ThrowsArgumentNull()
    {
        var service = CreateSmtpService(new EmailOptions { Enabled = false });

        var act = async () => await service.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UserEmailResolver_ReturnsEmail_ForExistingUser()
    {
        var userId = Guid.NewGuid();
        await using (var seed = new IdentityDbContext(_options))
        {
            seed.Users.Add(new ApplicationUser
            {
                Id = userId,
                UserName = "resolver@example.com",
                Email = "resolver@example.com",
                NormalizedEmail = "RESOLVER@EXAMPLE.COM",
                FirstName = "Test",
                LastName = "User"
            });
            await seed.SaveChangesAsync();
        }

        await using var context = new IdentityDbContext(_options);
        var resolver = new UserEmailResolver(context);

        var email = await resolver.ResolveEmailAsync(userId);

        email.Should().Be("resolver@example.com");
    }

    [Fact]
    public async Task UserEmailResolver_ReturnsNull_ForUnknownOrEmptyUser()
    {
        await using var context = new IdentityDbContext(_options);
        var resolver = new UserEmailResolver(context);

        (await resolver.ResolveEmailAsync(Guid.NewGuid())).Should().BeNull();
        (await resolver.ResolveEmailAsync(Guid.Empty)).Should().BeNull();
    }

    [Fact]
    public async Task EnabledEmail_Delivers_Message_To_Loopback_Smtp_Test_Transport()
    {
        // Real delivery over an actual SMTP conversation with a loopback test
        // transport: the message is only asserted as delivered once the server
        // has received and accepted the DATA payload.
        using var server = new SmtpTestServer();
        var service = CreateSmtpService(new EmailOptions
        {
            Enabled = true,
            Host = "127.0.0.1",
            Port = server.Port,
            UseSsl = false,
            FromAddress = "noreply@iranjob.test",
            FromDisplayName = "IranJob"
        });

        // SendAsync returns after the transport accepted the message (250 after DATA).
        await service.SendAsync(new EmailMessage(
            "candidate@example.com",
            "IranJob - application accepted",
            "Your application was accepted.\r\nSign in to view details."));

        server.Messages.Should().ContainSingle();
        // Envelope accepted for the expected sender and recipient.
        server.Protocol.Should().Contain(command => command.StartsWith("MAIL FROM:<noreply@iranjob.test>"));
        server.Protocol.Should().Contain(command => command.StartsWith("RCPT TO:<candidate@example.com>"));
        // Headers and body arrived in the accepted DATA payload.
        var received = server.Messages[0];
        received.Should().Contain("From: \"IranJob\" <noreply@iranjob.test>");
        received.Should().Contain("To: candidate@example.com");
        received.Should().Contain("Subject: IranJob - application accepted");
        received.Should().Contain("Your application was accepted.");
        received.Should().Contain("Sign in to view details.");
    }

    [Fact]
    public async Task EnabledEmail_UnavailableTransport_Logs_Without_Throwing()
    {
        // Bind and immediately release a loopback port so nothing listens on it:
        // the SMTP connect must be refused, and the service must swallow the
        // transport failure (the originating business transaction already committed).
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        var service = CreateSmtpService(new EmailOptions
        {
            Enabled = true,
            Host = "127.0.0.1",
            Port = port,
            UseSsl = false,
            FromAddress = "noreply@iranjob.test"
        });

        var act = async () => await service.SendAsync(
            new EmailMessage("someone@example.com", "Subject", "Body"));

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Minimal, real SMTP server on an ephemeral loopback port: speaks the SMTP
    /// dialogue (EHLO / MAIL FROM / RCPT TO / DATA / QUIT) and captures accepted
    /// message payloads. Used as the local SMTP test substitute for
    /// <see cref="SmtpEmailService"/>.
    /// </summary>
    private sealed class SmtpTestServer : IDisposable
    {
        private readonly TcpListener listener;
        private readonly CancellationTokenSource stop = new();

        public SmtpTestServer()
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            _ = Task.Run(AcceptLoopAsync);
        }

        public int Port { get; }

        public List<string> Messages { get; } = new();

        public List<string> Protocol { get; } = new();

        private async Task AcceptLoopAsync()
        {
            while (!stop.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(stop.Token);
                }
                catch (Exception)
                {
                    return;
                }

                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using var _ = client;
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII);
            using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

            await writer.WriteLineAsync("220 localhost SMTP test server");

            var inData = false;
            var data = new StringBuilder();
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (inData)
                {
                    if (line == ".")
                    {
                        inData = false;
                        Messages.Add(data.ToString());
                        await writer.WriteLineAsync("250 OK");
                    }
                    else
                    {
                        data.AppendLine(line);
                    }

                    continue;
                }

                Protocol.Add(line);

                if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("250-localhost");
                    await writer.WriteLineAsync("250 OK");
                }
                else if (line.StartsWith("MAIL FROM", StringComparison.OrdinalIgnoreCase)
                         || line.StartsWith("RCPT TO", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("250 OK");
                }
                else if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
                {
                    inData = true;
                    data.Clear();
                    await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                }
                else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("221 Bye");
                    return;
                }
                else
                {
                    await writer.WriteLineAsync("250 OK");
                }
            }
        }

        public void Dispose()
        {
            stop.Cancel();
            listener.Stop();
            stop.Dispose();
        }
    }
}

