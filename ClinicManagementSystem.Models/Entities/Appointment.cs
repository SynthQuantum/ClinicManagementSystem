using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Models.Entities;

public class Appointment : BaseEntity
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid StaffMemberId { get; set; }

    public AppointmentType AppointmentType { get; set; } = AppointmentType.General;

    [Required]
    public DateTime AppointmentDate { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    [MaxLength(500)]
    public string? Reason { get; set; }

    public string? Notes { get; set; }

    public bool ReminderSent { get; set; } = false;

    // AI / Prediction fields
    public bool IsPredictedNoShow { get; set; } = false;

    [Column(TypeName = "decimal(5,4)")]
    public decimal? NoShowProbability { get; set; }

    public int? PredictedDurationMinutes { get; set; }

    // Navigation
    [ForeignKey(nameof(PatientId))]
    public Patient? Patient { get; set; }

    [ForeignKey(nameof(StaffMemberId))]
    public StaffMember? StaffMember { get; set; }

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<PredictionResult> PredictionResults { get; set; } = new List<PredictionResult>();
    public ICollection<VisitRecord> VisitRecords { get; set; } = new List<VisitRecord>();
}
