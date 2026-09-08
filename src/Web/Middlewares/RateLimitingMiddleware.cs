using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OnlineCompiler.Web.Middlewares;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _clients = new();
    private const int RequestLimit = 120; // 120 requests per minute
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/hubs") || path.StartsWith("/healthz") || path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        var clientEntry = _clients.AddOrUpdate(ip,
            _ => (1, now),
            (_, current) =>
            {
                if (now - current.WindowStart > Window)
                {
                    return (1, now);
                }
                return (current.Count + 1, current.WindowStart);
            });

        if (clientEntry.Count > RequestLimit)
        {
            _logger.LogWarning("Rate limit exceeded for IP {Ip}", ip);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsync("{\"error\":\"Too many requests. Please try again later.\"}");
            return;
        }

        await _next(context);
    }
}
