using ClinicManagementSystem.Services.Interfaces;
using ClinicManagementSystem.Services.Options;
using Microsoft.Extensions.Options;

namespace ClinicManagementSystem.API.BackgroundServices;

public class ReminderProcessingHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationReminderOptions _options;
    private readonly ILogger<ReminderProcessingHostedService> _logger;

    public ReminderProcessingHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationReminderOptions> options,
        ILogger<ReminderProcessingHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Reminder background processor is disabled by configuration.");
            return;
        }

        _logger.LogInformation("Reminder background processor started. IntervalSeconds={Interval}", _options.ProcessorIntervalSeconds);

        await ProcessOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(15, _options.ProcessorIntervalSeconds)));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessOnceAsync(stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var result = await notificationService.ProcessRemindersAsync(cancellationToken);

            _logger.LogInformation(
                "Reminder background cycle completed. Created={Created}, Due={Due}, Sent={Sent}, Failed={Failed}",
                result.CreatedReminderCount,
                result.DuePendingCount,
                result.SentCount,
                result.FailedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Reminder background processor cancellation requested.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reminder background cycle failed.");
        }
    }
}
