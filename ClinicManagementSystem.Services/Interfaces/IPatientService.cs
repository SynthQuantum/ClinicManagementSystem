using ClinicManagementSystem.Models.Entities;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IPatientService
{
    Task<IEnumerable<Patient>> GetAllAsync();
    Task<(IEnumerable<Patient> Items, int TotalCount)> QueryAsync(
        string? searchTerm,
        int page,
        int pageSize,
        string? sortBy,
        bool sortDescending);
    Task<Patient?> GetByIdAsync(Guid id);
    Task<Patient> CreateAsync(Patient patient);
    Task<Patient> UpdateAsync(Patient patient);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<Patient>> SearchAsync(string searchTerm);
}
