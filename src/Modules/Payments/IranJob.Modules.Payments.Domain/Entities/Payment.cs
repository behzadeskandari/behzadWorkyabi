using System;
using IranJob.Modules.Payments.Domain.Enums;
using IranJob.Modules.Payments.Domain.Events;
using IranJob.SharedKernel.Exceptions;
using IranJob.SharedKernel.Events;

namespace IranJob.Modules.Payments.Domain.Entities;

/// <summary>
/// Aggregates a single payment made by an employer (typically to publish a job
/// posting). The aggregate is the source of truth for payment state and raises
/// <see cref="PaymentCompleted"/> / <see cref="PaymentFailed"/> domain events
/// after it is persisted, so other modules can react without direct references.
/// </summary>
public sealed class Payment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EmployerProfileId { get; private set; }
    public long Amount { get; private set; }
    public Currency Currency { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public PaymentMethod PaymentMethod { get; private set; } = PaymentMethod.ZarinPal;
    public string Authority { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? CallbackUrl { get; private set; }
    public Guid? RelatedJobPostingId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public byte[] RowVersion { get; private set; } = new byte[8];

    // EF Core
    private Payment() { }

    public static Payment Create(
        Guid employerProfileId,
        long amount,
        Currency currency,
        Guid? jobPostingId,
        string description,
        string? callbackUrl)
    {
        if (employerProfileId == Guid.Empty)
            throw new DomainException("Employer profile id is required.");
        if (amount < 0)
            throw new DomainException("Payment amount cannot be negative.");

        return new Payment
        {
            EmployerProfileId = employerProfileId,
            Amount = amount,
            Currency = currency,
            PaymentMethod = PaymentMethod.ZarinPal,
            RelatedJobPostingId = jobPostingId,
            Description = description ?? string.Empty,
            CallbackUrl = callbackUrl
        };
    }

    public void SetAuthority(string authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
            throw new DomainException("Authority is required for a gateway payment.");
        Authority = authority.Trim();
    }

    public void Complete(string? referenceId = null)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException("Only a pending payment can be completed.");

        Status = PaymentStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new PaymentCompleted(Id, EmployerProfileId, RelatedJobPostingId, referenceId));
    }

    public void Fail(string reason)
    {
        if (Status == PaymentStatus.Paid)
            throw new DomainException("A completed payment cannot be marked as failed.");
        if (Status == PaymentStatus.Failed)
            return;

        Status = PaymentStatus.Failed;
        FailedAt = DateTimeOffset.UtcNow;
        var text = (reason ?? string.Empty).Trim();
        FailureReason = text.Length > 1000 ? text[..1000] : text;
        AddDomainEvent(new PaymentFailed(Id, EmployerProfileId, RelatedJobPostingId));
    }

    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}


