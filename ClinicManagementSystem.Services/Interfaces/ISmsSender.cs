using ClinicManagementSystem.Services.Notifications;

namespace ClinicManagementSystem.Services.Interfaces;

public interface ISmsSender
{
    Task<NotificationDeliveryResult> SendAsync(string recipientPhone, string message, CancellationToken cancellationToken = default);
}
