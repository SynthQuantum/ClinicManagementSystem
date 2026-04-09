namespace ClinicManagementSystem.Models.DTOs;

public class NotificationProcessingResult
{
    public int CreatedReminderCount { get; set; }

    public int DuplicateReminderCount { get; set; }

    public int DuePendingCount { get; set; }

    public int SentCount { get; set; }

    public int FailedCount { get; set; }

    public DateTime ProcessedAtUtc { get; set; }
}
