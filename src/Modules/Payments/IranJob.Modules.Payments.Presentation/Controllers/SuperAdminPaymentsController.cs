using Asp.Versioning;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.Payments.Application.Contracts;
using IranJob.Modules.Payments.Domain.Entities;
using IranJob.Modules.Payments.Domain.Enums;
using IranJob.Modules.Payments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.Payments.Presentation.Controllers;

/// <summary>
/// Super Admin payment management endpoints. Provides read-only access to
/// payment records, payment summary statistics, and individual payment detail.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/super-admin/payments")]
[Authorize(Policy = PolicyNames.SuperAdminOnly)]
public sealed class SuperAdminPaymentsController(
    PaymentDbContext dbContext) : ControllerBase
{

    [HttpGet("{paymentId:guid}")]
    [ProducesResponseType(typeof(PaymentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDetailResponse>> GetByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment is null)
            return NotFound(new ErrorResponse("Payment not found."));

        return Ok(ToDetailResponse(payment));
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(PaymentSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaymentSummaryResponse>> GetSummaryAsync(
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken = default)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
            return BadRequest(new ErrorResponse("fromDate must not be later than toDate."));

        var query = dbContext.Payments
            .AsNoTracking()
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(p => p.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= toDate.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var successful = await query.CountAsync(p => p.Status == PaymentStatus.Paid, cancellationToken);
        var pending = await query.CountAsync(p => p.Status == PaymentStatus.Pending, cancellationToken);
        var failed = await query.CountAsync(p => p.Status == PaymentStatus.Failed, cancellationToken);

        var totalAmount = await query.SumAsync(p => (long?)p.Amount, cancellationToken) ?? 0L;
        var successfulAmount = await query.Where(p => p.Status == PaymentStatus.Paid)
            .SumAsync(p => (long?)p.Amount, cancellationToken) ?? 0L;

        return Ok(new PaymentSummaryResponse(total, successful, pending, failed, totalAmount, successfulAmount));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedPaymentResponse>> GetAllPaymentsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? paymentMethod = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = dbContext.Payments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Authority.Contains(term) ||
                p.Description.Contains(term) ||
                p.EmployerProfileId.ToString().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status.ToString() == status);

        if (!string.IsNullOrWhiteSpace(paymentMethod))
            query = query.Where(p => p.PaymentMethod.ToString() == paymentMethod);

        if (fromDate.HasValue)
            query = query.Where(p => p.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(p => p.CreatedAt <= toDate.Value);

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

        return Ok(new PaginatedPaymentResponse(items, page, pageSize, totalCount));
    }

    private static PaymentDetailResponse ToDetailResponse(Payment p)
    {
        return new PaymentDetailResponse(
            p.Id,
            p.Amount,
            p.Currency.ToString(),
            p.Status.ToString(),
            p.PaymentMethod.ToString(),
            p.Authority,
            p.Description,
            p.CallbackUrl,
            p.RelatedJobPostingId,
            p.EmployerProfileId,
            p.CreatedAt,
            p.PaidAt,
            p.FailedAt,
            p.FailureReason,
            p.PublicationStatus.ToString());
    }
}
