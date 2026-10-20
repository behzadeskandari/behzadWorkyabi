namespace IranJob.BuildingBlocks.Infrastructure.Configuration;

/// <summary>
/// Outbound email (SMTP) configuration, bound from the "Email" section.
/// Email is opt-in: it is disabled unless <see cref="Enabled"/> is set to true,
/// so environments without an SMTP server (tests, local dev) keep working with
/// a no-op email service.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// Master switch. When false the registered <c>IEmailService</c> logs and
    /// returns without attempting any network I/O.
    /// </summary>
    public bool Enabled { get; set; }

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    /// <summary>Use STARTTLS. Leave false for local/dev SMTP relays.</summary>
    public bool UseSsl { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>From address used on outbound mail.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Display name used on the From address.</summary>
    public string FromDisplayName { get; set; } = "IranJob";
}
