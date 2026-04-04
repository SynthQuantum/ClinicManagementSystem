using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace ClinicManagementSystem.Models.Entities;

public class AppUser : IdentityUser<Guid>
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Receptionist;

    public bool IsActive { get; set; } = true;

    // Timestamps and soft-delete (replicated from BaseEntity since IdentityUser<Guid> is the base)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    public string FullName => $"{FirstName} {LastName}";
}
