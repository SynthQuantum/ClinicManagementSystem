using ClinicManagementSystem.API.Middleware;
using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicManagementSystem.API.Tests;

public class PerformanceMonitoringMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldCaptureRequestSample()
    {
        var fakeService = new FakePerformanceMonitoringService();
        var services = new ServiceCollection()
            .AddSingleton<IPerformanceMonitoringService>(fakeService)
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";

        var middleware = new PerformanceMonitoringMiddleware(
            async httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                await Task.CompletedTask;
            },
            NullLogger<PerformanceMonitoringMiddleware>.Instance);

        await middleware.InvokeAsync(context, fakeService);

        fakeService.Captured.Should().ContainSingle();
        fakeService.Captured[0].Method.Should().Be("GET");
        fakeService.Captured[0].Path.Should().Be("/api/test");
        fakeService.Captured[0].StatusCode.Should().Be(204);
        fakeService.Captured[0].ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(0);
    }

    private sealed class FakePerformanceMonitoringService : IPerformanceMonitoringService
    {
        public bool IsEnabled => true;
        public List<PerformanceSample> Captured { get; } = [];
        public void CaptureSample(PerformanceSample sample) => Captured.Add(sample);
        public Task FlushPendingSamplesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<PerformanceSummary> GetSummaryAsync(CancellationToken cancellationToken = default) => Task.FromResult(new PerformanceSummary());
        public Task ResetSamplesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
