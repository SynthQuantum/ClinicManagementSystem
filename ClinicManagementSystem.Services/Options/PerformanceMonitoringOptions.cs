namespace ClinicManagementSystem.Services.Options;

public class PerformanceMonitoringOptions
{
    public const string SectionName = "PerformanceMonitoring";

    public bool Enabled { get; set; } = true;

    public bool PersistToDatabase { get; set; } = true;

    public int FlushIntervalSeconds { get; set; } = 30;

    public int MaxInMemorySamples { get; set; } = 2000;

    public int MaxSummarySamples { get; set; } = 5000;

    public int SummaryLookbackHours { get; set; } = 24;

    public int SlowEndpointCount { get; set; } = 5;

    public int RecentFailedRequestCount { get; set; } = 10;
}
