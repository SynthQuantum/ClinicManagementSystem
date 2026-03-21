namespace ClinicManagementSystem.Models.DTOs;

public class NoShowDatasetGenerationResult
{
    public int RequestedRows { get; set; }

    public int GeneratedRows { get; set; }

    public int NoShowCount { get; set; }

    public int ShowCount { get; set; }

    public decimal NoShowRate { get; set; }

    public string DatasetPath { get; set; } = string.Empty;
}
