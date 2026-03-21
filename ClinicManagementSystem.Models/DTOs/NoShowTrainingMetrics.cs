namespace ClinicManagementSystem.Models.DTOs;

public class NoShowTrainingMetrics
{
    public double Accuracy { get; set; }

    public double Precision { get; set; }

    public double Recall { get; set; }

    public double F1Score { get; set; }

    public double Auc { get; set; }
}
