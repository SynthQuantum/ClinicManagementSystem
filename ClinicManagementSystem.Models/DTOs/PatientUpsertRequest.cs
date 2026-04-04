using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models.DTOs;

public class PatientUpsertRequest : IValidatableObject
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    public Models.Enums.Gender Gender { get; set; }

    [MaxLength(20)]
    [RegularExpression(@"^\+?[0-9\-\s\(\)]{7,20}$", ErrorMessage = "Phone number format is invalid.")]
    public string? PhoneNumber { get; set; }

    [MaxLength(256), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(10)]
    public string? BloodType { get; set; }

    [MaxLength(200)]
    public string? InsuranceProvider { get; set; }

    [MaxLength(100)]
    public string? InsurancePolicyNumber { get; set; }

    public DateTime? InsuranceExpiryDate { get; set; }

    [MaxLength(200)]
    public string? EmergencyContactName { get; set; }

    [MaxLength(20)]
    [RegularExpression(@"^\+?[0-9\-\s\(\)]{7,20}$", ErrorMessage = "Emergency contact phone format is invalid.")]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(100)]
    public string? EmergencyContactRelationship { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth.Date > DateTime.UtcNow.Date)
        {
            yield return new ValidationResult("Date of birth cannot be in the future.", [nameof(DateOfBirth)]);
        }

        if (InsuranceExpiryDate.HasValue && InsuranceExpiryDate.Value.Date < DateTime.UtcNow.Date.AddYears(-1))
        {
            yield return new ValidationResult("Insurance expiry date appears invalid.", [nameof(InsuranceExpiryDate)]);
        }

        if (!string.IsNullOrWhiteSpace(InsurancePolicyNumber) && string.IsNullOrWhiteSpace(InsuranceProvider))
        {
            yield return new ValidationResult("Insurance provider is required when a policy number is provided.", [nameof(InsuranceProvider)]);
        }
    }
}
