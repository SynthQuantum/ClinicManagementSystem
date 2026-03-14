using ClinicManagementSystem.Models.Entities;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IAuditLogService
{
    Task<IEnumerable<AuditLog>> GetAllAsync();
    Task<AuditLog?> GetByIdAsync(Guid id);
    Task<AuditLog> CreateAsync(AuditLog log);
}
