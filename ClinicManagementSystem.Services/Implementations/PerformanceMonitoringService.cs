using System.Collections.Concurrent;
using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using ClinicManagementSystem.Services.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicManagementSystem.Services.Implementations;

public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PerformanceMonitoringOptions _options;
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly ConcurrentQueue<PerformanceSample> _recentSamples = new();
    private readonly ConcurrentQueue<PerformanceSample> _pendingPersistence = new();

    public PerformanceMonitoringService(
        IServiceScopeFactory scopeFactory,
        IOptions<PerformanceMonitoringOptions> options,
        ILogger<PerformanceMonitoringService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => _options.Enabled;

    public void CaptureSample(PerformanceSample sample)
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            var memoryCopy = Clone(sample);
            _recentSamples.Enqueue(memoryCopy);
            TrimInMemoryQueue();

            if (_options.PersistToDatabase)
            {
                _pendingPersistence.Enqueue(Clone(sample));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture performance sample for {Method} {Path}", sample.Method, sample.Path);
        }
    }

    public async Task FlushPendingSamplesAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !_options.PersistToDatabase || _pendingPersistence.IsEmpty)
        {
            return;
        }

        var batch = new List<PerformanceSample>();
        while (_pendingPersistence.TryDequeue(out var sample))
        {
            batch.Add(sample);
            if (batch.Count >= 250)
            {
                break;
            }
        }

        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            db.PerformanceSamples.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist {Count} performance samples. Returning them to queue.", batch.Count);
            foreach (var sample in batch)
            {
                _pendingPersistence.Enqueue(sample);
            }
        }
    }

    public async Task<PerformanceSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return new PerformanceSummary
            {
                MonitoringEnabled = false,
                PersistingToDatabase = _options.PersistToDatabase,
                GeneratedAtUtc = DateTime.UtcNow
            };
        }

        var samples = await LoadSamplesAsync(cancellationToken);
        if (samples.Count == 0)
        {
            return new PerformanceSummary
            {
                MonitoringEnabled = true,
                PersistingToDatabase = _options.PersistToDatabase,
                GeneratedAtUtc = DateTime.UtcNow
            };
        }

        var endpointSummaries = samples
            .GroupBy(s => new { s.Method, s.Path })
            .Select(group => new PerformanceEndpointSummary
            {
                Method = group.Key.Method,
                Path = group.Key.Path,
                RequestCount = group.Count(),
                AverageLatencyMs = Math.Round(group.Average(s => s.ElapsedMilliseconds), 2),
                P95LatencyMs = Math.Round(CalculateP95(group.Select(s => s.ElapsedMilliseconds)), 2),
                ErrorRate = Math.Round(group.Count(s => s.StatusCode >= 400) * 100d / group.Count(), 2)
            })
            .OrderByDescending(s => s.RequestCount)
            .ToList();

        return new PerformanceSummary
        {
            MonitoringEnabled = true,
            PersistingToDatabase = _options.PersistToDatabase,
            TotalRequestCount = samples.Count,
            AverageLatencyMs = Math.Round(samples.Average(s => s.ElapsedMilliseconds), 2),
            P95LatencyMs = Math.Round(CalculateP95(samples.Select(s => s.ElapsedMilliseconds)), 2),
            ErrorRate = Math.Round(samples.Count(s => s.StatusCode >= 400) * 100d / samples.Count, 2),
            GeneratedAtUtc = DateTime.UtcNow,
            EndpointSummaries = endpointSummaries,
            SlowestEndpoints = endpointSummaries
                .OrderByDescending(s => s.AverageLatencyMs)
                .ThenByDescending(s => s.P95LatencyMs)
                .Take(Math.Max(1, _options.SlowEndpointCount))
                .ToList(),
            RecentFailedRequests = samples
                .Where(s => s.StatusCode >= 400)
                .OrderByDescending(s => s.RequestTimestampUtc)
                .Take(Math.Max(1, _options.RecentFailedRequestCount))
                .Select(s => new RecentFailedRequestSummary
                {
                    Method = s.Method,
                    Path = s.Path,
                    StatusCode = s.StatusCode,
                    ElapsedMilliseconds = Math.Round(s.ElapsedMilliseconds, 2),
                    TimestampUtc = s.RequestTimestampUtc
                })
                .ToList()
        };
    }

    public async Task ResetSamplesAsync(CancellationToken cancellationToken = default)
    {
        while (_recentSamples.TryDequeue(out _)) { }
        while (_pendingPersistence.TryDequeue(out _)) { }

        if (!_options.PersistToDatabase)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        var rows = await db.PerformanceSamples.IgnoreQueryFilters().ToListAsync(cancellationToken);
        db.PerformanceSamples.RemoveRange(rows);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<PerformanceSample>> LoadSamplesAsync(CancellationToken cancellationToken)
    {
        var lookbackStart = DateTime.UtcNow.AddHours(-Math.Max(1, _options.SummaryLookbackHours));

        if (_options.PersistToDatabase)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            var persisted = await db.PerformanceSamples
                .AsNoTracking()
                .Where(s => s.RequestTimestampUtc >= lookbackStart)
                .OrderByDescending(s => s.RequestTimestampUtc)
                .Take(Math.Max(1, _options.MaxSummarySamples))
                .ToListAsync(cancellationToken);

            if (persisted.Count > 0)
            {
                return persisted;
            }
        }

        return _recentSamples
            .Where(s => s.RequestTimestampUtc >= lookbackStart)
            .OrderByDescending(s => s.RequestTimestampUtc)
            .Take(Math.Max(1, _options.MaxSummarySamples))
            .ToList();
    }

    private void TrimInMemoryQueue()
    {
        while (_recentSamples.Count > Math.Max(100, _options.MaxInMemorySamples) && _recentSamples.TryDequeue(out _))
        {
        }
    }

    private static double CalculateP95(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(v => v).ToList();
        if (ordered.Count == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling(ordered.Count * 0.95) - 1;
        index = Math.Clamp(index, 0, ordered.Count - 1);
        return ordered[index];
    }

    private static PerformanceSample Clone(PerformanceSample sample)
    {
        return new PerformanceSample
        {
            Method = sample.Method,
            Path = sample.Path,
            StatusCode = sample.StatusCode,
            ElapsedMilliseconds = sample.ElapsedMilliseconds,
            RequestTimestampUtc = sample.RequestTimestampUtc
        };
    }
}
