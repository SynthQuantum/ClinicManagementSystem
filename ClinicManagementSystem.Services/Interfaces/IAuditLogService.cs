using ClinicManagementSystem.Models.Entities;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IAuditLogService
{
    Task<IEnumerable<AuditLog>> GetAllAsync(int take = 500);
    Task<AuditLog?> GetByIdAsync(Guid id);
    Task<AuditLog> CreateAsync(AuditLog log);

    /// <summary>Returns audit entries performed by a specific user, newest first.</summary>
    Task<IEnumerable<AuditLog>> GetByUserAsync(Guid userId, int take = 200);

    /// <summary>Returns audit entries for a specific entity type and optionally a specific entity ID.</summary>
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, Guid? entityId = null, int take = 200);

    /// <summary>Returns audit entries within a UTC date range.</summary>
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, int maxResults = 500);

    /// <summary>Returns authentication and access-failure events (LoginFailed, AccountLockedOut, Unauthorized).</summary>
    Task<IEnumerable<AuditLog>> GetSecurityEventsAsync(int take = 100);
}
