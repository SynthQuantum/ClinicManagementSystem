using System.Diagnostics;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;

namespace ClinicManagementSystem.API.Middleware;

public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

    public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IPerformanceMonitoringService performanceMonitoringService)
    {
        if (!performanceMonitoringService.IsEnabled || context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        var started = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
            CaptureSample(context, performanceMonitoringService, started, stopwatch.Elapsed.TotalMilliseconds, context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            CaptureSample(context, performanceMonitoringService, started, stopwatch.Elapsed.TotalMilliseconds, StatusCodes.Status500InternalServerError);
            _logger.LogWarning(ex, "Performance monitoring captured unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            throw;
        }
    }

    private static void CaptureSample(HttpContext context, IPerformanceMonitoringService performanceMonitoringService, DateTime started, double elapsedMilliseconds, int statusCode)
    {
        performanceMonitoringService.CaptureSample(new PerformanceSample
        {
            Method = context.Request.Method,
            Path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/",
            StatusCode = statusCode,
            ElapsedMilliseconds = elapsedMilliseconds,
            RequestTimestampUtc = started
        });
    }
}
