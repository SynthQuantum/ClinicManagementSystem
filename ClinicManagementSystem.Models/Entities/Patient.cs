using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Models.Entities;

public class Patient : BaseEntity
{
    // Demographics
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(256), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(10)]
    public string? BloodType { get; set; }

    // Insurance Info
    [MaxLength(200)]
    public string? InsuranceProvider { get; set; }

    [MaxLength(100)]
    public string? InsurancePolicyNumber { get; set; }

    public DateTime? InsuranceExpiryDate { get; set; }

    // Emergency Contact
    [MaxLength(200)]
    public string? EmergencyContactName { get; set; }

    [MaxLength(20)]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(100)]
    public string? EmergencyContactRelationship { get; set; }

    // Notes
    public string? Notes { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    public int Age => (int)((DateTime.UtcNow - DateOfBirth).TotalDays / 365.25);

    public bool HasInsurance => !string.IsNullOrWhiteSpace(InsuranceProvider);

    // Navigation
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<VisitRecord> VisitRecords { get; set; } = new List<VisitRecord>();
}
