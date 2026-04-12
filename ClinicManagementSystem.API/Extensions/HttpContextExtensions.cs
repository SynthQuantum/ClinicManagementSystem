namespace ClinicManagementSystem.API.Extensions;

/// <summary>
/// Extension methods on <see cref="HttpContext"/> for security-related helpers.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Resolves the client IP address, honouring the <c>X-Forwarded-For</c> header
    /// set by trusted reverse proxies. Always take the first (leftmost) entry which
    /// represents the original client IP before any intermediate hops.
    /// </summary>
    /// <remarks>
    /// When deploying behind a reverse proxy (nginx, YARP, Azure App Gateway) ensure
    /// <c>app.UseForwardedHeaders()</c> is configured in <c>Program.cs</c> OR that
    /// <c>KnownProxies</c> / <c>KnownNetworks</c> are whitelisted so that
    /// <c>X-Forwarded-For</c> values are trusted rather than spoofed.
    /// </remarks>
    public static string GetClientIpAddress(this HttpContext context)
    {
        // X-Forwarded-For may contain a comma-separated chain: clientIp, proxy1, proxy2 ...
        var xff = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xff))
        {
            var first = xff.Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(first))
                return first;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
