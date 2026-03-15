using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

public class ClinicSettingsService : IClinicSettingsService
{
    private readonly ClinicDbContext _db;
    private readonly ILogger<ClinicSettingsService> _logger;

    public ClinicSettingsService(ClinicDbContext db, ILogger<ClinicSettingsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ClinicSettings?> GetCurrentAsync()
    {
        _logger.LogInformation("Fetching clinic settings");
        return await _db.ClinicSettings.AsNoTracking().OrderByDescending(s => s.UpdatedAt).FirstOrDefaultAsync();
    }

    public async Task<ClinicSettings> UpsertAsync(ClinicSettings settings)
    {
        var existing = await _db.ClinicSettings.FirstOrDefaultAsync();
        if (existing is null)
        {
            _logger.LogInformation("Creating clinic settings");
            _db.ClinicSettings.Add(settings);
            await _db.SaveChangesAsync();
            return settings;
        }

        _logger.LogInformation("Updating clinic settings {SettingsId}", existing.Id);
        existing.ClinicName = settings.ClinicName;
        existing.Address = settings.Address;
        existing.PhoneNumber = settings.PhoneNumber;
        existing.Email = settings.Email;
        existing.OpeningTime = settings.OpeningTime;
        existing.ClosingTime = settings.ClosingTime;
        existing.DefaultAppointmentDurationMinutes = settings.DefaultAppointmentDurationMinutes;

        await _db.SaveChangesAsync();
        return existing;
    }
}
