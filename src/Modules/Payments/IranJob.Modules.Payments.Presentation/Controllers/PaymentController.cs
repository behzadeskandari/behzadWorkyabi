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
/// Employer-facing payment lifecycle: initiation (ZarinPal request) and
/// callback verification (ZarinPal verification). Publish fee initiation is
/// normally triggered from the job-publishing endpoint; this controller also
/// exposes a generic initiation endpoint for future payment use-cases.
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

    [HttpPost("initiate")]
    [ProducesResponseType(typeof(PaymentInitiationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentInitiationResponse>> Initiate(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var employerProfileId = await ResolveEmployerProfileIdAsync(cancellationToken)
            ?? throw new NotFoundException("An employer profile is required to make a payment.");

        var response = await paymentService.InitiateAsync(employerProfileId, request, cancellationToken);
        return Ok(response);
    }

    // GET endpoint so ZarinPal's browser redirect (or the Angular callback page)
    // can complete verification without a POST body.
    [HttpGet("verify")]
    [ProducesResponseType(typeof(PaymentVerifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentVerifyResponse>> Verify(
        [FromQuery] string authority,
        [FromQuery] string status,
        CancellationToken cancellationToken)
    {
        var employerProfileId = await ResolveEmployerProfileIdAsync(cancellationToken)
            ?? throw new NotFoundException("An employer profile is required to verify this payment.");

        var response = await paymentService.VerifyByCallbackAsync(
            authority, status ?? string.Empty, employerProfileId, cancellationToken);
        return Ok(response);
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
