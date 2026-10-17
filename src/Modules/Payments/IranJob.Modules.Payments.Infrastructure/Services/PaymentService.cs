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
        var fee = _options.PublishFeeAmount;
        var payment = Payment.Create(employerProfileId, fee, ParseCurrency(_options.PublishFeeCurrency),
            jobPostingId, _options.PublishFeeDescription, _options.ZarinPal.CallbackUrl);

        if (fee <= 0)
        {
            payment.Complete();
            await SaveAndDispatchAsync(payment, cancellationToken);
            return CompletedInitiation(payment, jobPostingId);
        }

        var result = await zarinPal.RequestAsync(
            new ZarinPalRequest(_options.ZarinPal.MerchantId, fee, _options.PublishFeeDescription,
                _options.ZarinPal.CallbackUrl, _options.PublishFeeCurrency), cancellationToken);

        if (result.Code == 100 && !string.IsNullOrWhiteSpace(result.Authority))
        {
            payment.SetAuthority(result.Authority);
            await SaveAndDispatchAsync(payment, cancellationToken);
            logger.LogInformation("Payment {PaymentId} initiated for job {JobId}; authority {Authority}.",
                payment.Id, jobPostingId, payment.Authority);
            return GatedInitiation(payment, jobPostingId, result.Authority);
        }

        // Gateway rejected the request â€” the payment must NOT be marked completed.
        // B1: The job must remain in Draft until a verified successful payment exists.
        payment.Fail($"Gateway error: {result.Message} (code {result.Code})");
        await SaveAndDispatchAsync(payment, cancellationToken);
        return FailedInitiation(payment, jobPostingId);
    }

    private PaymentInitiationResponse FailedInitiation(Payment payment, Guid? jobPostingId) =>
        new(payment.Id, payment.Authority, string.Empty, payment.Amount,
            payment.Currency.ToString(), payment.Status.ToString(),
            payment.PaymentMethod.ToString(), jobPostingId, false);


    public async Task<PaymentInitiationResponse> InitiateAsync(Guid employerProfileId, CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = Payment.Create(employerProfileId, request.Amount,
            ParseCurrency(request.Currency), request.JobPostingId,
            request.Description ?? string.Empty, request.CallbackUrl);

        var result = await zarinPal.RequestAsync(
            new ZarinPalRequest(_options.ZarinPal.MerchantId, request.Amount, request.Description ?? string.Empty,
                request.CallbackUrl ?? _options.ZarinPal.CallbackUrl, request.Currency), cancellationToken);

        if (result.Code == 100 && !string.IsNullOrWhiteSpace(result.Authority))
        {
            payment.SetAuthority(result.Authority);
        }
        else
        {
            payment.Fail($"Gateway error: {result.Message} (code {result.Code})");
        }

        await SaveAndDispatchAsync(payment, cancellationToken);
        var isGated = result.Code == 100 && !string.IsNullOrWhiteSpace(result.Authority);
        return isGated
            ? GatedInitiation(payment, request.JobPostingId, result.Authority)
            : FailedInitiation(payment, request.JobPostingId);
    }

    public async Task<PaymentVerifyResponse> VerifyByCallbackAsync(string authority, string zarinStatus, Guid employerProfileId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authority))
            throw new NotFoundException("Payment authority was not provided.");

        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(payment => payment.Authority == authority, cancellationToken)
            ?? throw new NotFoundException("Payment was not found.");

        // B10: Employers cannot access or manipulate another employer's transactions.
        if (payment.EmployerProfileId != employerProfileId)
            throw new ForbiddenException("You are not authorized to verify this payment.");

        // B8: Repeated callbacks must not complete a payment or grant an entitlement twice.
        if (payment.Status == PaymentStatus.Paid)
            return new PaymentVerifyResponse(payment.Id, payment.Status.ToString(), payment.RelatedJobPostingId, "Already verified");
        if (payment.Status == PaymentStatus.Failed)
            return new PaymentVerifyResponse(payment.Id, payment.Status.ToString(), payment.RelatedJobPostingId, "Already failed");

        var canceled = zarinStatus is "0" or "cancel" or "-1"
            || string.Equals(zarinStatus, "canceled", StringComparison.OrdinalIgnoreCase);
        if (canceled)
        {
            payment.Fail("Payment was canceled by the user.");
            await SaveAndDispatchWithRetryAsync(payment, cancellationToken);
            return new PaymentVerifyResponse(payment.Id, payment.Status.ToString(), payment.RelatedJobPostingId, "Canceled");
        }

        var verification = await zarinPal.VerifyAsync(
            new ZarinPalVerifyRequest(_options.ZarinPal.MerchantId, payment.Amount, authority, payment.Currency.ToString()),
            cancellationToken);

        if (verification.Code == 100 || verification.Code == 101)
        {
            try
            {
                payment.Complete(verification.ReferenceId);
                await SaveAndDispatchWithRetryAsync(payment, cancellationToken);
                return new PaymentVerifyResponse(payment.Id, payment.Status.ToString(), payment.RelatedJobPostingId, verification.Message);
            }
            catch (DomainException)
            {
                // B9: A concurrent callback already transitioned this payment to Paid.
                // Reload the authoritative state and return it without double-processing.
                await dbContext.Entry(payment).ReloadAsync(cancellationToken);
                return new PaymentVerifyResponse(payment.Id, payment.Status.ToString(), payment.RelatedJobPostingId, "Already verified");
            }
        }

        payment.Fail($"Verification failed: {verification.Message} (code {verification.Code})");
        await SaveAndDispatchWithRetryAsync(payment, cancellationToken);
        return new PaymentVerifyResponse(payment.Id, payment.Status.ToString(), payment.RelatedJobPostingId, verification.Message);
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


    public async Task<PaymentResponse?> GetAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments.AsNoTracking().FirstOrDefaultAsync(payment => payment.Id == paymentId, cancellationToken);
        return payment is null ? null : Map(payment);
    }

    public Task<bool> HasCompletedPaymentForJobAsync(Guid jobPostingId, CancellationToken cancellationToken) =>
        dbContext.Payments.AnyAsync(payment => payment.RelatedJobPostingId == jobPostingId && payment.Status == PaymentStatus.Paid, cancellationToken);

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
        payment.Status.ToString(), payment.PaymentMethod.ToString(), payment.Authority,
        payment.RelatedJobPostingId, payment.CreatedAt, payment.PaidAt, payment.FailureReason);

    private async Task DispatchEventsAsync(Payment payment, CancellationToken cancellationToken)
    {
        var events = payment.DomainEvents.ToArray();
        payment.ClearDomainEvents();
        foreach (IDomainEvent domainEvent in events)
            await dispatcher.DispatchAsync(domainEvent, cancellationToken);
    }
}

