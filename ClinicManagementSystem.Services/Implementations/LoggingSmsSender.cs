using System.Text.RegularExpressions;
using ClinicManagementSystem.Services.Interfaces;
using ClinicManagementSystem.Services.Notifications;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

public class LoggingSmsSender : ISmsSender
{
    private static readonly Regex PhonePattern = new("^[0-9+][0-9\\-\\s]{6,20}$", RegexOptions.Compiled);
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger)
    {
        _logger = logger;
    }

    public Task<NotificationDeliveryResult> SendAsync(string recipientPhone, string message, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(recipientPhone))
        {
            return Task.FromResult(NotificationDeliveryResult.Fail("SMS recipient is empty."));
        }

        if (!PhonePattern.IsMatch(recipientPhone))
        {
            return Task.FromResult(NotificationDeliveryResult.Fail("Recipient phone format is invalid."));
        }

        _logger.LogInformation("[MockSms] To={Recipient} Message={Message}", recipientPhone, message);
        return Task.FromResult(NotificationDeliveryResult.Ok());
    }
}
