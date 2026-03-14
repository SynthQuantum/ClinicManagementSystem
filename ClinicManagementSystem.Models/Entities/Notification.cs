using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Models.Entities;

public class Notification : BaseEntity
{
    [Required]
    public Guid AppointmentId { get; set; }

    public NotificationType NotificationType { get; set; }

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public DateTime ScheduledFor { get; set; }

    public DateTime? SentAt { get; set; }

    [Required, MaxLength(256)]
    public string Recipient { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? FailureReason { get; set; }

    // Navigation
    [ForeignKey(nameof(AppointmentId))]
    public Appointment? Appointment { get; set; }
}
