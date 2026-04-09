using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Implementations;
using ClinicManagementSystem.Services.Interfaces;
using ClinicManagementSystem.Services.Options;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClinicManagementSystem.Services.Tests;

public class PerformanceMonitoringServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ShouldCalculateAverageP95AndErrorRate()
    {
        using var provider = BuildProvider(Guid.NewGuid().ToString());
        var sut = CreateService(provider, enabled: true, persistToDatabase: false);

        sut.CaptureSample(new PerformanceSample { Method = "GET", Path = "/api/patients", StatusCode = 200, ElapsedMilliseconds = 10, RequestTimestampUtc = DateTime.UtcNow.AddMinutes(-5) });
        sut.CaptureSample(new PerformanceSample { Method = "GET", Path = "/api/patients", StatusCode = 500, ElapsedMilliseconds = 100, RequestTimestampUtc = DateTime.UtcNow.AddMinutes(-4) });
        sut.CaptureSample(new PerformanceSample { Method = "POST", Path = "/api/appointments", StatusCode = 201, ElapsedMilliseconds = 50, RequestTimestampUtc = DateTime.UtcNow.AddMinutes(-3) });
        sut.CaptureSample(new PerformanceSample { Method = "POST", Path = "/api/appointments", StatusCode = 400, ElapsedMilliseconds = 80, RequestTimestampUtc = DateTime.UtcNow.AddMinutes(-2) });
        sut.CaptureSample(new PerformanceSample { Method = "GET", Path = "/api/dashboard/summary", StatusCode = 200, ElapsedMilliseconds = 30, RequestTimestampUtc = DateTime.UtcNow.AddMinutes(-1) });

        var summary = await sut.GetSummaryAsync();

        summary.TotalRequestCount.Should().Be(5);
        summary.AverageLatencyMs.Should().BeApproximately(54, 0.1);
        summary.P95LatencyMs.Should().Be(100);
        summary.ErrorRate.Should().Be(40);
        summary.SlowestEndpoints.Should().NotBeEmpty();
        summary.RecentFailedRequests.Should().HaveCount(2);
    }

    [Fact]
    public async Task FlushPendingSamplesAsync_ShouldPersistSamples_WhenEnabled()
    {
        using var provider = BuildProvider(Guid.NewGuid().ToString());
        var sut = CreateService(provider, enabled: true, persistToDatabase: true);

        sut.CaptureSample(new PerformanceSample
        {
            Method = "GET",
            Path = "/api/test",
            StatusCode = 200,
            ElapsedMilliseconds = 25,
            RequestTimestampUtc = DateTime.UtcNow
        });

        await sut.FlushPendingSamplesAsync();

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        (await db.PerformanceSamples.CountAsync()).Should().Be(1);
    }

    private static ServiceProvider BuildProvider(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ClinicDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static IPerformanceMonitoringService CreateService(ServiceProvider provider, bool enabled, bool persistToDatabase)
    {
        return new PerformanceMonitoringService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new PerformanceMonitoringOptions
            {
                Enabled = enabled,
                PersistToDatabase = persistToDatabase,
                FlushIntervalSeconds = 5,
                MaxInMemorySamples = 100,
                MaxSummarySamples = 100,
                SummaryLookbackHours = 24,
                SlowEndpointCount = 5,
                RecentFailedRequestCount = 10
            }),
            NullLogger<PerformanceMonitoringService>.Instance);
    }
}
