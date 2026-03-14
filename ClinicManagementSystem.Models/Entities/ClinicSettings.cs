using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models.Entities;

public class ClinicSettings : BaseEntity
{
    [Required, MaxLength(300)]
    public string ClinicName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(256), EmailAddress]
    public string? Email { get; set; }

    public TimeSpan OpeningTime { get; set; } = new TimeSpan(8, 0, 0);

    public TimeSpan ClosingTime { get; set; } = new TimeSpan(18, 0, 0);

    public int DefaultAppointmentDurationMinutes { get; set; } = 30;
}
