namespace ClinicManagementSystem.Models.DTOs;

public class PerformanceSummary
{
    public bool MonitoringEnabled { get; set; }

    public bool PersistingToDatabase { get; set; }

    public int TotalRequestCount { get; set; }

    public double AverageLatencyMs { get; set; }

    public double P95LatencyMs { get; set; }

    public double ErrorRate { get; set; }

    public DateTime GeneratedAtUtc { get; set; }

    public IReadOnlyList<PerformanceEndpointSummary> EndpointSummaries { get; set; } = [];

    public IReadOnlyList<PerformanceEndpointSummary> SlowestEndpoints { get; set; } = [];

    public IReadOnlyList<RecentFailedRequestSummary> RecentFailedRequests { get; set; } = [];
}
