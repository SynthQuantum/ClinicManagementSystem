using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

public class PredictionResultService : IPredictionResultService
{
    private readonly ClinicDbContext _db;
    private readonly ILogger<PredictionResultService> _logger;

    public PredictionResultService(ClinicDbContext db, ILogger<PredictionResultService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<PredictionResult>> GetByAppointmentAsync(Guid appointmentId)
    {
        _logger.LogInformation("Fetching prediction results for appointment {AppointmentId}", appointmentId);
        return await _db.PredictionResults
            .AsNoTracking()
            .Where(r => r.AppointmentId == appointmentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<PredictionResult> CreateAsync(PredictionResult result)
    {
        _logger.LogInformation("Creating prediction result for appointment {AppointmentId}", result.AppointmentId);
        _db.PredictionResults.Add(result);
        await _db.SaveChangesAsync();
        return result;
    }
}
