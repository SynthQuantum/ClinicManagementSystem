namespace ClinicManagementSystem.Models.DTOs;

public class PerformanceEndpointSummary
{
    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public int RequestCount { get; set; }

    public double AverageLatencyMs { get; set; }

    public double P95LatencyMs { get; set; }

    public double ErrorRate { get; set; }
}
