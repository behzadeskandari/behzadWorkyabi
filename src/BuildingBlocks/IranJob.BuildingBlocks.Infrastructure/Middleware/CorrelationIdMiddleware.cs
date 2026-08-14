using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using IranJob.BuildingBlocks.Infrastructure.Logging;

namespace IranJob.BuildingBlocks.Infrastructure.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers);

        context.Items[CorrelationIdConstants.ItemKey] = correlationId;
        context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(IHeaderDictionary headers)
    {
        if (headers.TryGetValue(CorrelationIdConstants.HeaderName, out StringValues values)
            && !StringValues.IsNullOrEmpty(values))
        {
            return values.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}
