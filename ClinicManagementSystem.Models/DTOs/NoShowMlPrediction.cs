namespace ClinicManagementSystem.Models.DTOs;

public class NoShowMlPrediction
{
    public bool PredictedLabel { get; set; }

    public float Score { get; set; }

    public float Probability { get; set; }
}
