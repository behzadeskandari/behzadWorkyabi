using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IranJob.Api.Configuration;

public static class FrontendCors
{
    private static readonly string[] LocalOrigins = ["http://localhost:4200"];

    public static string[] GetAllowedOrigins(IConfiguration configuration, string environmentName)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0 && environmentName is "Development" or "Testing")
            return LocalOrigins;

        if (origins.Length == 0 || origins.Any(origin =>
                !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || uri.Scheme is not ("http" or "https")
                || uri.AbsolutePath != "/"
                || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment)
                || origin.Contains('*')
                || (environmentName == "Production" && uri.Scheme != Uri.UriSchemeHttps)))
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must contain valid, explicit browser origins; Production requires HTTPS origins.");
        }

        return origins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static IServiceCollection AddFrontendCors(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
    {
        var origins = GetAllowedOrigins(configuration, environmentName);
        services.AddCors(options => options.AddPolicy("Frontend", policy =>
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithExposedHeaders("X-CSRF-TOKEN", "X-Correlation-ID")));
        return services;
    }
}
