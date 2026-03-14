using ClinicManagementSystem.Models.DTOs;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IPredictionService
{
    Task<NoShowPredictionOutput> PredictNoShowAsync(NoShowPredictionInput input);
}
