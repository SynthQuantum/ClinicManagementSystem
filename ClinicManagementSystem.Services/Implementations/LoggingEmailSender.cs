using System.Net.Mail;
using ClinicManagementSystem.Services.Interfaces;
using ClinicManagementSystem.Services.Notifications;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<NotificationDeliveryResult> SendAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return Task.FromResult(NotificationDeliveryResult.Fail("Email recipient is empty."));
        }

        try
        {
            _ = new MailAddress(recipientEmail);
        }
        catch
        {
            return Task.FromResult(NotificationDeliveryResult.Fail("Recipient email format is invalid."));
        }

        _logger.LogInformation("[MockEmail] To={Recipient} Subject={Subject} Body={Body}", recipientEmail, subject, body);
        return Task.FromResult(NotificationDeliveryResult.Ok());
    }
}
