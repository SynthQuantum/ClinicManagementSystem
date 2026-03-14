using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models.Entities;

public class VisitRecord : BaseEntity
{
    [Required]
    public Guid PatientId { get; set; }

    public Guid? AppointmentId { get; set; }

    [Required]
    public Guid StaffMemberId { get; set; }

    [Required]
    public DateTime VisitDate { get; set; }

    [MaxLength(1000)]
    public string? Diagnosis { get; set; }

    [MaxLength(2000)]
    public string? Treatment { get; set; }

    [MaxLength(2000)]
    public string? Prescription { get; set; }

    public string? Notes { get; set; }

    // Navigation
    [ForeignKey(nameof(PatientId))]
    public Patient? Patient { get; set; }

    [ForeignKey(nameof(AppointmentId))]
    public Appointment? Appointment { get; set; }

    [ForeignKey(nameof(StaffMemberId))]
    public StaffMember? StaffMember { get; set; }
}
