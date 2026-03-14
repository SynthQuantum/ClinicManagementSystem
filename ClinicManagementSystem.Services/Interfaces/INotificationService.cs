using ClinicManagementSystem.Models.Entities;

namespace ClinicManagementSystem.Services.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<Notification>> GetPendingAsync();
    Task<bool> SendAsync(Guid notificationId);
    Task<Notification> CreateAsync(Notification notification);
}
