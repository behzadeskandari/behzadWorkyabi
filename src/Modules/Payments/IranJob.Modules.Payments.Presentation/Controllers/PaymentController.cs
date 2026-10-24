using Asp.Versioning;
using IranJob.BuildingBlocks.Infrastructure.CurrentService;
using IranJob.Modules.EmployerProfile.Infrastructure.Persistence;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.Payments.Application.Abstractions;
using IranJob.Modules.Payments.Application.Contracts;
using IranJob.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IranJob.Modules.Payments.Presentation.Controllers;

/// <summary>
/// Employer-facing payment status and anonymous provider callback verification.
/// Payment initiation is performed through the owner-checked job-publishing endpoint.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employers/payments")]
[Authorize(Roles = IdentityRoles.Employer)]
public sealed class PaymentController(
    IPaymentService paymentService,
    ICurrentUserService currentUser,
    EmployerDbContext employerDbContext) : ControllerBase
{
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly EmployerDbContext _employerDbContext = employerDbContext;

    // GET endpoint so ZarinPal's browser redirect (or the Angular callback page)
    // can complete verification without a POST body.
    [HttpGet("verify")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentVerifyResponse>> Verify(
        [FromQuery] string authority,
        [FromQuery] string status,
        CancellationToken cancellationToken)
    {
        await paymentService.VerifyByCallbackAsync(authority, status ?? string.Empty, cancellationToken);
        return NoContent();
    }

    [HttpGet("{paymentId:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponse>> GetForEmployer(Guid paymentId, CancellationToken cancellationToken)
    {
        var employerProfileId = await ResolveEmployerProfileIdAsync(cancellationToken)
            ?? throw new NotFoundException("An employer profile is required to view a payment.");
        var payment = await paymentService.GetForEmployerAsync(paymentId, employerProfileId, cancellationToken)
            ?? throw new NotFoundException("Payment was not found.");
        return Ok(payment);
    }

    private async Task<Guid?> ResolveEmployerProfileIdAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null || userId == Guid.Empty)
            return null;

        return await _employerDbContext.EmployerProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => profile.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
