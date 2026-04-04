using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Models.DTOs;

public class AppointmentUpsertRequest : IValidatableObject
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

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public bool ReminderSent { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AppointmentDate == default)
        {
            yield return new ValidationResult("Appointment date is required.", [nameof(AppointmentDate)]);
        }

        if (StartTime >= EndTime)
        {
            yield return new ValidationResult("Start time must be earlier than end time.", [nameof(StartTime), nameof(EndTime)]);
        }

        if (StartTime < TimeSpan.Zero || EndTime > TimeSpan.FromHours(24))
        {
            yield return new ValidationResult("Appointment time must be within a valid 24-hour range.", [nameof(StartTime), nameof(EndTime)]);
        }

        if (PatientId == Guid.Empty)
        {
            yield return new ValidationResult("Patient is required.", [nameof(PatientId)]);
        }

        if (StaffMemberId == Guid.Empty)
        {
            yield return new ValidationResult("Staff member is required.", [nameof(StaffMemberId)]);
        }
    }
}
