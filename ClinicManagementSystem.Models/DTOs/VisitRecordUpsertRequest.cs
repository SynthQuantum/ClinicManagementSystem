using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models.DTOs;

/// <summary>
/// Request DTO for creating or updating a visit record.
/// Uses a DTO to prevent overposting of BaseEntity fields (Id, CreatedAt, IsDeleted, etc.)
/// and navigation properties that carry PHI.
/// </summary>
public class VisitRecordUpsertRequest
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
}
