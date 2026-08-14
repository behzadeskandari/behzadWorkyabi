using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using IranJob.BuildingBlocks.Infrastructure.Logging;

namespace IranJob.BuildingBlocks.Infrastructure.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var correlationId = context.Items[CorrelationIdConstants.ItemKey]?.ToString() ?? "unknown";

        logger.LogInformation(
            "HTTP {Method} {Path} started. CorrelationId={CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms. CorrelationId={CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }
}
