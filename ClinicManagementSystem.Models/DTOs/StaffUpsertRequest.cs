using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Models.DTOs;

public class StaffUpsertRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(256), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    [RegularExpression(@"^\+?[0-9\-\s\(\)]{7,20}$", ErrorMessage = "Phone number format is invalid.")]
    public string? PhoneNumber { get; set; }

    public UserRole Role { get; set; } = UserRole.Doctor;

    [MaxLength(200)]
    public string? Specialty { get; set; }

    public bool IsAvailable { get; set; } = true;
}
