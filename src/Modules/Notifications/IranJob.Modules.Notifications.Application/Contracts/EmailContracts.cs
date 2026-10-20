namespace IranJob.Modules.Notifications.Application.Contracts;

/// <summary>
/// A single outbound email to deliver to one recipient. Built by event handlers
/// (or services) and passed to <c>IEmailService</c>. The subject/body are plain
/// text by default; set <see cref="IsHtml"/> to <see langword="true"/> for HTML
/// bodies.
/// </summary>
public sealed record EmailMessage(
    string To,
    string Subject,
    string Body,
    bool IsHtml = false);
