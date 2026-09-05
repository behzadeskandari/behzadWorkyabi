using System.Security.Cryptography;
using IranJob.Modules.Identity.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace IranJob.Modules.Identity.Infrastructure.Security;

public static class RefreshTokenCookieHelper
{
    public const string CsrfHeaderName = "X-CSRF-TOKEN";

    public static void SetRefreshTokenCookie(
        HttpResponse response,
        string refreshToken,
        IdentitySecurityOptions options,
        IHostEnvironment environment)
    {
        var csrfToken = GenerateCsrfToken();
        var cookieOptions = CreateCookieOptions(options, environment, httpOnly: true);
        response.Cookies.Append(options.RefreshTokenCookieName, refreshToken, cookieOptions);

        var csrfCookieOptions = CreateCookieOptions(options, environment, httpOnly: false);
        response.Cookies.Append(options.CsrfCookieName, csrfToken, csrfCookieOptions);
        response.Headers[options.CsrfHeaderName] = csrfToken;
    }

    public static void ClearRefreshTokenCookies(
        HttpResponse response,
        IdentitySecurityOptions options,
        IHostEnvironment environment)
    {
        var cookieOptions = CreateCookieOptions(options, environment, httpOnly: true);
        response.Cookies.Delete(options.RefreshTokenCookieName, cookieOptions);

        var csrfCookieOptions = CreateCookieOptions(options, environment, httpOnly: false);
        response.Cookies.Delete(options.CsrfCookieName, csrfCookieOptions);
    }

    public static string? GetRefreshTokenFromRequest(
        HttpRequest request,
        IdentitySecurityOptions options)
    {
        return request.Cookies[options.RefreshTokenCookieName];
    }

    public static bool ValidateCsrf(HttpRequest request, IdentitySecurityOptions options)
    {
        if (!request.Cookies.TryGetValue(options.CsrfCookieName, out var cookieToken)
            || string.IsNullOrWhiteSpace(cookieToken))
        {
            return false;
        }

        if (!request.Headers.TryGetValue(options.CsrfHeaderName, out var headerValues))
        {
            return false;
        }

        var headerToken = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(headerToken) || headerToken.Length != cookieToken.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(cookieToken),
            System.Text.Encoding.UTF8.GetBytes(headerToken));
    }

    private static CookieOptions CreateCookieOptions(
        IdentitySecurityOptions options,
        IHostEnvironment environment,
        bool httpOnly)
    {
        return new CookieOptions
        {
            HttpOnly = httpOnly,
            Secure = !environment.IsDevelopment() && !environment.IsEnvironment("Testing"),
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(options.RefreshTokenExpirationDays)
        };
    }

    private static string GenerateCsrfToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
