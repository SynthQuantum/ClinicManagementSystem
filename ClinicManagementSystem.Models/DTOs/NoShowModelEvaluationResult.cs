namespace ClinicManagementSystem.Models.DTOs;

public class NoShowModelEvaluationResult
{
    public double Accuracy { get; set; }

    public double Precision { get; set; }

    public double Recall { get; set; }

    public double F1Score { get; set; }

    public double Auc { get; set; }

    public NoShowConfusionMatrixCounts? ConfusionMatrix { get; set; }

    public int TrainRowCount { get; set; }

    public int TestRowCount { get; set; }

    public string ModelPath { get; set; } = string.Empty;

    public string DatasetPath { get; set; } = string.Empty;

    public DateTime TrainingTimestampUtc { get; set; }
}
