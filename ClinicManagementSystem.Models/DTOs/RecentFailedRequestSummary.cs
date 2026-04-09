namespace ClinicManagementSystem.Models.DTOs;

public class RecentFailedRequestSummary
{
    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public double ElapsedMilliseconds { get; set; }

    public DateTime TimestampUtc { get; set; }
}
