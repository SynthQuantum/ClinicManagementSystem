using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Models.Entities;

public class AppUser : BaseEntity
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(256), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public UserRole Role { get; set; } = UserRole.Receptionist;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";
}
