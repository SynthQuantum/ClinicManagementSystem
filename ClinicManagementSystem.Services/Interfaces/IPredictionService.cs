using ClinicManagementSystem.Models.DTOs;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IPredictionService
{
    Task<NoShowPredictionOutput> PredictNoShowAsync(NoShowPredictionInput input);
    Task<NoShowDatasetGenerationResult> GenerateNoShowDatasetAsync(int rowCount = 1200, CancellationToken cancellationToken = default);
    Task<NoShowTrainingResult> TrainNoShowModelAsync(string? datasetPath = null, CancellationToken cancellationToken = default);
}
