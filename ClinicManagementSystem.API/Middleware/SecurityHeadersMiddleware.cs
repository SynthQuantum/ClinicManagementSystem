namespace ClinicManagementSystem.API.Middleware;

/// <summary>
/// Adds defensive HTTP security headers to every non-Swagger response.
/// These headers reduce exposure to common browser-based attacks (clickjacking,
/// MIME-sniffing, cross-site leaks) and are recommended by OWASP.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip injecting headers for Swagger UI / OpenAPI JSON to avoid interfering
        // with the interactive documentation rendering.
        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            var headers = context.Response.Headers;

            // Prevent MIME type sniffing (OWASP A05 - Security Misconfiguration)
            headers["X-Content-Type-Options"] = "nosniff";

            // Block the page from being embedded in iframes (clickjacking defence)
            headers["X-Frame-Options"] = "DENY";

            // Legacy XSS filter support (kept for older browsers)
            headers["X-XSS-Protection"] = "1; mode=block";

            // Control how much referrer information is included with requests
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Restrict access to browser features not required by the API
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            // Strict Content Security Policy — API responses are JSON, so a restrictive
            // default-src is appropriate. Adjust if Swagger UI is served in production.
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        }

        await _next(context);
    }
}
