using Asp.Versioning;
using IranJob.BuildingBlocks.Infrastructure.CurrentService;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.Messages.Application.Abstractions;
using IranJob.Modules.Messages.Application.Contracts;
using IranJob.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IranJob.Modules.Messages.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/messages")]
[Authorize(Roles = IdentityRoles.Candidate + "," + IdentityRoles.Employer + "," + IdentityRoles.Recruiter + "," + IdentityRoles.Admin + "," + IdentityRoles.SuperAdmin)]
public sealed class MessagesController(IConversationService service, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("conversations")]
    public Task<IReadOnlyList<ConversationListItem>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        service.ListConversationsAsync(UserId, page, pageSize, ct);

    [HttpPost("conversations")]
    public async Task<ActionResult<ConversationListItem>> Create(CreateConversationRequest request, CancellationToken ct)
    {
        var result = await service.CreateConversationAsync(request, UserId, ct);
        return Ok(result);
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<ActionResult<ConversationListItem>> Get(Guid id, CancellationToken ct) =>
        await service.GetConversationAsync(id, UserId, ct) is { } conversation ? Ok(conversation) : NotFound();

    [HttpGet("conversations/{id:guid}/messages")]
    public Task<IReadOnlyList<MessageDto>> History(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        service.GetMessagesAsync(id, UserId, page, pageSize, ct);

    [HttpPost("send")]
    public Task<MessageDto> Send(SendMessageRequest request, CancellationToken ct) => service.SendMessageAsync(request, UserId, ct);

    [HttpPost("conversations/{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct) { await service.MarkAsReadAsync(id, UserId, ct); return NoContent(); }

    [HttpGet("unread")]
    public Task<UnreadSummary> Unread(CancellationToken ct) => service.GetUnreadSummaryAsync(UserId, ct);

    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedException();
}
