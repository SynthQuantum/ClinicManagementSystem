using ClinicManagementSystem.Models.Entities;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IClinicSettingsService
{
    Task<ClinicSettings?> GetCurrentAsync();
    Task<ClinicSettings> UpsertAsync(ClinicSettings settings);
}
