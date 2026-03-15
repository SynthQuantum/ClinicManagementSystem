using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Models.Entities;

public class StaffMember : BaseEntity
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(256), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public UserRole Role { get; set; } = UserRole.Doctor;

    [MaxLength(200)]
    public string? Specialty { get; set; }

    public bool IsAvailable { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";

    // Navigation
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<VisitRecord> VisitRecords { get; set; } = new List<VisitRecord>();
}
