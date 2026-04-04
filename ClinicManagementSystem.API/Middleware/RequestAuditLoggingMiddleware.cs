using System.Security.Claims;

namespace ClinicManagementSystem.API.Middleware;

public class RequestAuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestAuditLoggingMiddleware> _logger;

    public RequestAuditLoggingMiddleware(RequestDelegate next, ILogger<RequestAuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        var started = DateTime.UtcNow;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub")
            ?? "anonymous";

        try
        {
            await _next(context);

            var success = context.Response.StatusCode is >= 200 and < 400;
            _logger.LogInformation(
                "Request audit: {Method} {Path} by {UserId} at {Timestamp} finished with {StatusCode} Success={Success}",
                context.Request.Method,
                context.Request.Path,
                userId,
                started,
                context.Response.StatusCode,
                success);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Request audit: {Method} {Path} by {UserId} at {Timestamp} failed with unhandled exception",
                context.Request.Method,
                context.Request.Path,
                userId,
                started);
            throw;
        }
    }
}
