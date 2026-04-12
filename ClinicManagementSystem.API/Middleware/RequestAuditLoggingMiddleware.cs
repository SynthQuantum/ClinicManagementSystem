using System.Security.Claims;
using ClinicManagementSystem.API.Extensions;

namespace ClinicManagementSystem.API.Middleware;

/// <summary>
/// Middleware that logs every API request with caller identity, IP address, HTTP details,
/// and outcome. Security-relevant events (401, 403, 4xx/5xx) are emitted at Warning/Error
/// level so they can be routed to alerting pipelines (e.g. Application Insights, Seq).
/// </summary>
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
        var ipAddress = context.GetClientIpAddress();
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub")
            ?? "anonymous";

        try
        {
            await _next(context);

            var statusCode = context.Response.StatusCode;
            var role = context.User.FindFirstValue(ClaimTypes.Role) ?? "none";
            var success = statusCode is >= 200 and < 400;

            if (statusCode == 401)
            {
                // Unauthenticated — may indicate token expiry, missing token, or scraping
                _logger.LogWarning(
                    "SECURITY: Unauthenticated request. Method={Method} Path={Path} IP={IpAddress} StatusCode=401",
                    context.Request.Method, context.Request.Path, ipAddress);
            }
            else if (statusCode == 403)
            {
                // Authenticated but unauthorised — role violation or privilege escalation attempt
                _logger.LogWarning(
                    "SECURITY: Forbidden access attempt. Method={Method} Path={Path} UserId={UserId} Role={Role} IP={IpAddress} StatusCode=403",
                    context.Request.Method, context.Request.Path, userId, role, ipAddress);
            }
            else if (statusCode >= 500)
            {
                _logger.LogError(
                    "SECURITY: Server error during request. Method={Method} Path={Path} UserId={UserId} IP={IpAddress} StatusCode={StatusCode}",
                    context.Request.Method, context.Request.Path, userId, ipAddress, statusCode);
            }
            else
            {
                _logger.LogInformation(
                    "Request audit: {Method} {Path} UserId={UserId} Role={Role} IP={IpAddress} Timestamp={Timestamp} StatusCode={StatusCode} Success={Success}",
                    context.Request.Method, context.Request.Path, userId, role, ipAddress, started, statusCode, success);
            }
        }
        catch (Exception ex)
        {
            var role = context.User.FindFirstValue(ClaimTypes.Role) ?? "none";
            _logger.LogWarning(
                ex,
                "Request audit: {Method} {Path} UserId={UserId} Role={Role} IP={IpAddress} Timestamp={Timestamp} failed with unhandled exception",
                context.Request.Method, context.Request.Path, userId, role, ipAddress, started);
            throw;
        }
    }
}

