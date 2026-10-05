using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace IranJob.BuildingBlocks.Infrastructure.CurrentService;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public string? Email =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public bool IsInRole(string role) =>
        httpContextAccessor.HttpContext?.User.IsInRole(role) == true;
}
