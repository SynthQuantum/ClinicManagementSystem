using ClinicManagementSystem.Services.Interfaces;
using ClinicManagementSystem.Services.Options;
using Microsoft.Extensions.Options;

namespace ClinicManagementSystem.API.BackgroundServices;

public class PerformanceSampleFlushHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PerformanceMonitoringOptions _options;
    private readonly ILogger<PerformanceSampleFlushHostedService> _logger;

    public PerformanceSampleFlushHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<PerformanceMonitoringOptions> options,
        ILogger<PerformanceSampleFlushHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.PersistToDatabase)
        {
            _logger.LogInformation("Performance sample flusher is disabled by configuration.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, _options.FlushIntervalSeconds)));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var monitoringService = scope.ServiceProvider.GetRequiredService<IPerformanceMonitoringService>();
                await monitoringService.FlushPendingSamplesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Performance sample flusher stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Performance sample flush cycle failed.");
            }
        }
    }
}
