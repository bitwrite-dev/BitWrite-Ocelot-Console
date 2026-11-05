using BitWrite.OcelotConsole.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace BitWrite.OcelotConsole.Api.Middleware;

/// <summary>
/// Middleware that enforces rate limiting on authentication endpoints to prevent
/// brute-force attacks. Tracks failed attempts per client IP address using Redis
/// and returns 429 Too Many Requests when the limit is exceeded.
/// </summary>
public class AuthenticationThrottleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuthenticationThrottleService _throttleService;

    /// <summary>Path segments that indicate an authentication endpoint.</summary>
    private static readonly string[] AuthPathSegments =
    {
        "/auth",
        "/login",
        "/token",
        "/authenticate",
        "/signin"
    };

    public AuthenticationThrottleMiddleware(RequestDelegate next, IAuthenticationThrottleService throttleService)
    {
        _next = next;
        _throttleService = throttleService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Only apply throttling to authentication endpoints.
        if (!IsAuthPath(path))
        {
            await _next(context);
            return;
        }

        var identifier = GetClientIdentifier(context);

        // If the client is locked out, reject immediately.
        if (await _throttleService.IsLockedOutAsync(identifier))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                "{\"error\":\"Too many failed authentication attempts. Please try again later.\"}");
            return;
        }

        var originalBody = context.Response.Body;
        using var newBody = new MemoryStream();
        context.Response.Body = newBody;

        try
        {
            await _next(context);

            // If the response indicates authentication failure, record it.
            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
            {
                await _throttleService.RecordFailureAsync(identifier);
            }
            else if (context.Response.StatusCode == StatusCodes.Status200OK)
            {
                // Successful authentication clears any failure count.
                await _throttleService.RecordSuccessAsync(identifier);
            }
        }
        finally
        {
            // Copy the response body back to the original stream.
            newBody.Seek(0, SeekOrigin.Begin);
            await newBody.CopyToAsync(originalBody);
            context.Response.Body = originalBody;
        }
    }

    /// <summary>Determines whether the request path targets an authentication endpoint.</summary>
    private static bool IsAuthPath(string path)
    {
        var lower = path.ToLowerInvariant();
        return AuthPathSegments.Any(segment => lower.Contains(segment));
    }

    /// <summary>Gets a client identifier for throttling purposes.</summary>
    /// <remarks>
    /// Uses the X-Forwarded-For header if present (for proxy/load-balancer setups),
    /// otherwise falls back to the remote IP address.
    /// </remarks>
    private static string GetClientIdentifier(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            // Take the first IP in the chain (the client closest to us).
            return forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault() ?? "unknown";
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public static class AuthenticationThrottleMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthenticationThrottle(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationThrottleMiddleware>();
    }
}
