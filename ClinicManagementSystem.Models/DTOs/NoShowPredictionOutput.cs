namespace ClinicManagementSystem.Models.DTOs;

public class NoShowPredictionOutput
{
    public bool WillNoShow { get; set; }
    public decimal Probability { get; set; }
    public decimal Score { get; set; }
    public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High
    public string Recommendation { get; set; } = string.Empty;
}
