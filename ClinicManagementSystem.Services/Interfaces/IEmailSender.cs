using ClinicManagementSystem.Services.Notifications;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IEmailSender
{
    Task<NotificationDeliveryResult> SendAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken = default);
}
