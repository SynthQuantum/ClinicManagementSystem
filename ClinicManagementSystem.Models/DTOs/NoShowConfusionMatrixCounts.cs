namespace ClinicManagementSystem.Models.DTOs;

public class NoShowConfusionMatrixCounts
{
    public int TruePositives { get; set; }

    public int TrueNegatives { get; set; }

    public int FalsePositives { get; set; }

    public int FalseNegatives { get; set; }
}
