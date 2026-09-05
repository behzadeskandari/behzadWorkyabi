using Asp.Versioning;
using IranJob.Modules.Identity.Application.Abstractions;
using IranJob.Modules.Identity.Infrastructure.Configuration;
using IranJob.Modules.Identity.Infrastructure.Extensions;
using IranJob.Modules.Identity.Infrastructure.Security;
using IranJob.Modules.Identity.Presentation.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace IranJob.Modules.Identity.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(
    IAuthService authService,
    IOptions<IdentitySecurityOptions> securityOptions,
    IHostEnvironment environment) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        await authService.RegisterAsync(
            new RegisterRequest(
                request.FirstName,
                request.LastName,
                request.Email,
                request.PhoneNumber,
                request.Password,
                request.Role),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(
            new LoginRequest(request.Identifier, request.Password),
            cancellationToken);

        RefreshTokenCookieHelper.SetRefreshTokenCookie(Response, result.RefreshToken, securityOptions.Value, environment);

        return Ok(MapAuthResponse(result.AccessToken, result.ExpiresAt, result.User));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> Refresh(CancellationToken cancellationToken)
    {
        if (!RefreshTokenCookieHelper.ValidateCsrf(Request, securityOptions.Value))
        {
            return Unauthorized();
        }

        var refreshToken = RefreshTokenCookieHelper.GetRefreshTokenFromRequest(Request, securityOptions.Value);
        var result = await authService.RefreshAsync(refreshToken, cancellationToken);

        RefreshTokenCookieHelper.SetRefreshTokenCookie(Response, result.RefreshToken, securityOptions.Value, environment);

        return Ok(MapAuthResponse(result.AccessToken, result.ExpiresAt, result.User));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!RefreshTokenCookieHelper.ValidateCsrf(Request, securityOptions.Value))
        {
            return Unauthorized();
        }

        var refreshToken = RefreshTokenCookieHelper.GetRefreshTokenFromRequest(Request, securityOptions.Value);
        await authService.LogoutAsync(refreshToken, cancellationToken);
        RefreshTokenCookieHelper.ClearRefreshTokenCookies(Response, securityOptions.Value, environment);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var profile = await authService.GetCurrentUserAsync(userId, cancellationToken);
        return Ok(MapUserProfile(profile));
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException();

        return Guid.Parse(claim);
    }

    private static AuthResponseDto MapAuthResponse(string accessToken, DateTimeOffset expiresAt, UserProfileResult user) =>
        new(accessToken, expiresAt, MapUserProfile(user));

    private static UserProfileDto MapUserProfile(UserProfileResult user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.PhoneNumber, user.Roles);
}
