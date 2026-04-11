using ClinicManagementSystem.Models.Entities;

namespace ClinicManagementSystem.Models.DTOs;

public class NotificationDashboardSummary
{
    public int PendingCount { get; set; }

    public int SentCount { get; set; }

    public int FailedCount { get; set; }

    public int CancelledCount { get; set; }

    public IReadOnlyList<Notification> RecentNotifications { get; set; } = [];
}
