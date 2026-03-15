using ClinicManagementSystem.Models.Entities;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IPredictionResultService
{
    Task<IEnumerable<PredictionResult>> GetByAppointmentAsync(Guid appointmentId);
    Task<PredictionResult> CreateAsync(PredictionResult result);
}
