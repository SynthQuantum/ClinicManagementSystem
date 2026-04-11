using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models.Entities;

public class PerformanceSample : BaseEntity
{
    [Required, MaxLength(16)]
    public string Method { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string Path { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public double ElapsedMilliseconds { get; set; }

    public DateTime RequestTimestampUtc { get; set; }
}
