using ClinicManagementSystem.Models.Entities;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IVisitRecordService
{
    Task<IEnumerable<VisitRecord>> GetByPatientAsync(Guid patientId);
    Task<VisitRecord?> GetByIdAsync(Guid id);
    Task<VisitRecord> CreateAsync(VisitRecord record);
    Task<VisitRecord> UpdateAsync(VisitRecord record);
    Task<bool> DeleteAsync(Guid id);
}
