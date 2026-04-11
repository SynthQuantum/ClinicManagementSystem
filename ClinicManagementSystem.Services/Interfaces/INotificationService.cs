using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Models.DTOs;

namespace ClinicManagementSystem.Services.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<Notification>> GetPendingAsync(int take = 200, CancellationToken cancellationToken = default);
    Task<IEnumerable<Notification>> GetHistoryAsync(NotificationStatus? status = null, int take = 100, CancellationToken cancellationToken = default);
    Task<NotificationDashboardSummary> GetSummaryAsync(int recentTake = 25, CancellationToken cancellationToken = default);
    Task<bool> SendAsync(Guid notificationId);
    Task<Notification> CreateAsync(Notification notification);
    Task<NotificationProcessingResult> ProcessRemindersAsync(CancellationToken cancellationToken = default);
}
