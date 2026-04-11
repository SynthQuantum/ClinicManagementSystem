namespace ClinicManagementSystem.Models.DTOs;

public class NoShowTrainingResult
{
    public string DatasetPath { get; set; } = string.Empty;

    public string ModelPath { get; set; } = string.Empty;

    public int TrainRowCount { get; set; }

    public int TestRowCount { get; set; }

    public NoShowTrainingMetrics Metrics { get; set; } = new();

    public NoShowModelEvaluationResult Evaluation { get; set; } = new();
}
