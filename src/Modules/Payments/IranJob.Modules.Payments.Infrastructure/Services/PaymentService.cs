using IranJob.BuildingBlocks.Infrastructure.Events;
using IranJob.Modules.Payments.Application.Abstractions;
using IranJob.Modules.Payments.Application.Contracts;
using IranJob.Modules.Payments.Domain.Entities;
using IranJob.Modules.Payments.Domain.Enums;
using IranJob.Modules.Payments.Infrastructure.Configuration;
using IranJob.Modules.Payments.Infrastructure.Persistence;
using IranJob.Modules.Payments.Infrastructure.Integrations.ZarinPal;
using IranJob.SharedKernel.Events;
using IranJob.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebUtilities;

namespace IranJob.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Orchestrates ZarinPal payment creation, verification, and domain-event dispatch.
/// Fee == 0 (default/test path) completes the payment in-process; the gated
/// path defers terminal state until the payer returns from the gateway.
/// </summary>
public sealed class PaymentService(
    PaymentDbContext dbContext,
    IZarinPalClient zarinPal,
    IEventDispatcher dispatcher,
    IOptions<PaymentOptions> options,
    ILogger<PaymentService> logger) : IPaymentService
{

    private readonly PaymentOptions _options = options.Value;
    public async Task<PaymentInitiationResponse> InitiateForJobPublishAsync(Guid employerProfileId, Guid jobPostingId, CancellationToken cancellationToken)
    {
        var prior = await dbContext.Payments
            .Where(payment => payment.EmployerProfileId == employerProfileId
                && payment.RelatedJobPostingId == jobPostingId
                && (payment.Status == PaymentStatus.Pending || payment.Status == PaymentStatus.Paid))
            .FirstOrDefaultAsync(cancellationToken);
        if (prior is not null)
        {
            if (prior.Status == PaymentStatus.Paid)
            {
                await RetryPublicationAsync(prior, cancellationToken);
                return CompletedInitiation(prior, jobPostingId);
            }
            if (!string.IsNullOrWhiteSpace(prior.Authority))
                return GatedInitiation(prior, jobPostingId, prior.Authority);
        }

        var fee = _options.PublishFeeAmount;
        var payment = Payment.Create(employerProfileId, fee, ParseCurrency(_options.PublishFeeCurrency),
            jobPostingId, _options.PublishFeeDescription, _options.ZarinPal.CallbackUrl);
        var callbackUrl = string.IsNullOrWhiteSpace(_options.ZarinPal.CallbackUrl)
            ? null
            : QueryHelpers.AddQueryString(_options.ZarinPal.CallbackUrl, "paymentId", payment.Id.ToString("D"));
        payment.SetCallbackUrl(callbackUrl);

        if (fee <= 0)
        {
            payment.Complete();
            await SaveAndDispatchAsync(payment, cancellationToken);
            return CompletedInitiation(payment, jobPostingId);
        }

        var result = await zarinPal.RequestAsync(
            new ZarinPalRequest(_options.ZarinPal.MerchantId, fee, _options.PublishFeeDescription,
                callbackUrl ?? string.Empty, _options.PublishFeeCurrency), cancellationToken);

        if (result.Code == 100 && !string.IsNullOrWhiteSpace(result.Authority))
        {
            payment.SetAuthority(result.Authority);
            await SaveAndDispatchAsync(payment, cancellationToken);
            logger.LogInformation("Payment {PaymentId} initiated for job {JobId}.",
                payment.Id, jobPostingId);
            return GatedInitiation(payment, jobPostingId, result.Authority);
        }

        // Gateway rejected the request — the payment must NOT be marked completed.
        // B1: The job must remain in Draft until a verified successful payment exists.
        payment.Fail($"Gateway error: {result.Message} (code {result.Code})");
        await SaveAndDispatchAsync(payment, cancellationToken);
        return FailedInitiation(payment, jobPostingId);
    }

    private PaymentInitiationResponse FailedInitiation(Payment payment, Guid? jobPostingId) =>
        new(payment.Id, payment.Authority, string.Empty, payment.Amount,
            payment.Currency.ToString(), payment.Status.ToString(),
            payment.PaymentMethod.ToString(), jobPostingId, false);


    public async Task<PaymentVerifyResponse> VerifyByCallbackAsync(string authority, string zarinStatus, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authority))
            throw new NotFoundException("Payment authority was not provided.");

        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(payment => payment.Authority == authority, cancellationToken)
            ?? throw new NotFoundException("Payment was not found.");

        if (payment.Status == PaymentStatus.Paid)
        {
            await RetryPublicationAsync(payment, cancellationToken);
            return ToVerifyResponse(payment, "Payment was already verified.");
        }
        if (payment.Status == PaymentStatus.Failed)
            return ToVerifyResponse(payment, "Payment has already failed.");

        // Provider query parameters are hints only. A non-success return never
        // changes persisted state; successful-looking returns require server verification.
        if (!IsSuccessfulReturnStatus(zarinStatus))
            return ToVerifyResponse(payment, "The provider did not confirm success; payment remains pending.");

        var verification = await zarinPal.VerifyAsync(
            new ZarinPalVerifyRequest(_options.ZarinPal.MerchantId, payment.Amount, authority, payment.Currency.ToString()),
            cancellationToken);

        if (verification.Code == 100 || verification.Code == 101)
        {
            try
            {
                payment.Complete(verification.ReferenceId);
            }
            catch (DomainException)
            {
                await dbContext.Entry(payment).ReloadAsync(cancellationToken);
                if (payment.Status != PaymentStatus.Paid)
                    throw;
            }

            await SaveAndDispatchWithRetryAsync(payment, cancellationToken);
            return ToVerifyResponse(payment, verification.Message);
        }

        // Verification failures may be transient or indicate an early callback.
        // Keep the payment retryable until the provider confirms a terminal result.
        logger.LogWarning("Provider verification did not confirm payment {PaymentId}; provider code {Code}.", payment.Id, verification.Code);
        return ToVerifyResponse(payment, "Payment is not verified yet and can be checked again.");
    }

    private async Task SaveAndDispatchWithRetryAsync(Payment payment, CancellationToken ct)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await dbContext.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < 2)
            {
                await dbContext.Entry(payment).ReloadAsync(ct);
            }
        }
        await DispatchEventsAsync(payment, ct);
    }


    public async Task<PaymentResponse?> GetForEmployerAsync(Guid paymentId, Guid employerProfileId, CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments.SingleOrDefaultAsync(
            item => item.Id == paymentId && item.EmployerProfileId == employerProfileId, cancellationToken);
        if (payment is null)
            return null;
        if (payment.Status == PaymentStatus.Paid && payment.PublicationStatus == PaymentPublicationStatus.Pending)
            await RetryPublicationAsync(payment, cancellationToken);
        return Map(payment);
    }

    public Task<bool> HasCompletedPaymentForJobAsync(Guid jobPostingId, CancellationToken cancellationToken) =>
        dbContext.Payments.AnyAsync(payment => payment.RelatedJobPostingId == jobPostingId && payment.Status == PaymentStatus.Paid, cancellationToken);

    public async Task<(IReadOnlyList<PaymentListResponse> Items, int TotalCount)> GetAllPaymentsAsync(
        int page,
        int pageSize,
        string? search,
        string? status,
        string? paymentMethod,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = dbContext.Payments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();
            query = query.Where(p =>
                p.Id.ToString().Contains(searchTerm) ||
                (p.Description != null && p.Description.Contains(searchTerm)) ||
                (p.Authority != null && p.Authority.Contains(searchTerm)) ||
                p.EmployerProfileId.ToString().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status.ToString() == status);
        }

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            query = query.Where(p => p.PaymentMethod.ToString() == paymentMethod);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PaymentListResponse(
                p.Id,
                p.Amount,
                p.Currency.ToString(),
                p.Status.ToString(),
                p.PaymentMethod.ToString(),
                p.Authority,
                p.CreatedAt,
                p.PaidAt,
                p.FailedAt,
                p.FailureReason,
                p.RelatedJobPostingId,
                p.EmployerProfileId))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<PaymentSummaryResponse> GetPaymentSummaryAsync(CancellationToken cancellationToken)
    {
        var payments = await dbContext.Payments.AsNoTracking().ToListAsync(cancellationToken);

        return new PaymentSummaryResponse(
            Total: payments.Count,
            Successful: payments.Count(p => p.Status == PaymentStatus.Paid),
            Pending: payments.Count(p => p.Status == PaymentStatus.Pending),
            Failed: payments.Count(p => p.Status == PaymentStatus.Failed),
            TotalAmount: payments.Sum(p => p.Amount),
            SuccessfulAmount: payments.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount));
    }

    public async Task<PaymentDetailResponse?> GetPaymentDetailAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
        if (payment is null)
            return null;

        return new PaymentDetailResponse(
            payment.Id,
            payment.Amount,
            payment.Currency.ToString(),
            payment.Status.ToString(),
            payment.PaymentMethod.ToString(),
            payment.Authority,
            payment.Description,
            payment.CallbackUrl,
            payment.RelatedJobPostingId,
            payment.EmployerProfileId,
            payment.CreatedAt,
            payment.PaidAt,
            payment.FailedAt,
            payment.FailureReason,
            payment.PublicationStatus.ToString());
    }

    private static Currency ParseCurrency(string value) => Enum.TryParse<Currency>(value, ignoreCase: true, out var c)
        ? c : throw new DomainException($"Unsupported payment currency '{value}'.");

    private async Task SaveAndDispatchAsync(Payment payment, CancellationToken cancellationToken)
    {
        await dbContext.Payments.AddAsync(payment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DispatchEventsAsync(payment, cancellationToken);
    }

    private PaymentInitiationResponse GatedInitiation(Payment payment, Guid? jobPostingId, string authority) =>
        new(payment.Id, authority, BuildRedirectUrl(_options, payment.PaymentMethod, authority),
            payment.Amount, payment.Currency.ToString(), payment.Status.ToString(),
            payment.PaymentMethod.ToString(), jobPostingId, false);

    private PaymentInitiationResponse CompletedInitiation(Payment payment, Guid? jobPostingId) =>
        new(payment.Id, payment.Authority, BuildRedirectUrl(_options, payment.PaymentMethod, payment.Authority),
            payment.Amount, payment.Currency.ToString(), payment.Status.ToString(),
            payment.PaymentMethod.ToString(), jobPostingId, true);

    private static string BuildRedirectUrl(PaymentOptions options, PaymentMethod method, string authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
            return string.Empty;
        var template = method == PaymentMethod.ZarinPal && options.ZarinPal.Sandbox
            ? options.ZarinPal.StartPaySandboxUrl
            : options.ZarinPal.StartPayUrl;
        return string.Format(template, authority);
    }

    private static PaymentResponse Map(Payment payment) => new(
        payment.Id, payment.EmployerProfileId, payment.Amount, payment.Currency.ToString(),
        payment.Status.ToString(), payment.PublicationStatus.ToString(), payment.PaymentMethod.ToString(), payment.Authority,
        payment.RelatedJobPostingId, payment.CreatedAt, payment.PaidAt, payment.FailureReason);

    private static bool IsSuccessfulReturnStatus(string status) =>
        string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase) || status == "1";

    private static PaymentVerifyResponse ToVerifyResponse(Payment payment, string? message) =>
        new(payment.Id, payment.Status.ToString(), payment.PublicationStatus.ToString(), payment.RelatedJobPostingId, message);

    private async Task RetryPublicationAsync(Payment payment, CancellationToken ct)
    {
        if (payment.PublicationStatus != PaymentPublicationStatus.Pending || !payment.RelatedJobPostingId.HasValue)
            return;

        var completionEvent = new IranJob.Modules.Payments.Domain.Events.PaymentCompleted(
            payment.Id, payment.EmployerProfileId, payment.RelatedJobPostingId, null);
        try
        {
            await dispatcher.DispatchAsync(completionEvent, ct);
            payment.MarkPublicationCompleted();
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception exception) when (exception is DomainException or NotFoundException)
        {
            payment.MarkPublicationBlocked();
            await dbContext.SaveChangesAsync(ct);
            logger.LogWarning(exception, "Paid payment {PaymentId} cannot publish job {JobId} under its current lifecycle state.",
                payment.Id, payment.RelatedJobPostingId);
        }
    }

    private async Task DispatchEventsAsync(Payment payment, CancellationToken cancellationToken)
    {
        var events = payment.DomainEvents.ToArray();
        payment.ClearDomainEvents();
        foreach (IDomainEvent domainEvent in events)
        {
            if (domainEvent is IranJob.Modules.Payments.Domain.Events.PaymentCompleted)
            {
                await RetryPublicationAsync(payment, cancellationToken);
                continue;
            }
            await dispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
    }
}

