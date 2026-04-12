using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Models.DTOs;

/// <summary>
/// Request DTO for creating a new application user. Using a DTO prevents overposting
/// attacks that could manipulate Identity internals (PasswordHash, SecurityStamp, etc.).
/// </summary>
public class CreateUserRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(256), EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password for the new account. Must satisfy the Identity password policy
    /// (min 8 chars, digit, uppercase, non-alphanumeric character).
    /// </summary>
    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Receptionist;

    public bool IsActive { get; set; } = true;
}
