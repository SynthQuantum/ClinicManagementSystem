using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models.Entities;

public class PredictionResult : BaseEntity
{
    [Required]
    public Guid AppointmentId { get; set; }

    [Required, MaxLength(200)]
    public string ModelName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string PredictionType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,4)")]
    public decimal ProbabilityScore { get; set; }

    public int? PredictedDurationMinutes { get; set; }

    [MaxLength(200)]
    public string? PredictedLabel { get; set; }

    public string? InputFeaturesJson { get; set; }

    public string? OutputJson { get; set; }

    // Navigation
    [ForeignKey(nameof(AppointmentId))]
    public Appointment? Appointment { get; set; }
}
