using ClinicManagementSystem.Models.Entities;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IPatientService
{
    Task<IEnumerable<Patient>> GetAllAsync();
    Task<Patient?> GetByIdAsync(Guid id);
    Task<Patient> CreateAsync(Patient patient);
    Task<Patient> UpdateAsync(Patient patient);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<Patient>> SearchAsync(string searchTerm);
}
