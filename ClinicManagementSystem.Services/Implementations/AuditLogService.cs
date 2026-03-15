using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

public class AuditLogService : IAuditLogService
{
    private readonly ClinicDbContext _db;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(ClinicDbContext db, ILogger<AuditLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync()
    {
        _logger.LogInformation("Fetching audit logs");
        return await _db.AuditLogs.AsNoTracking().OrderByDescending(a => a.CreatedAt).ToListAsync();
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching audit log {AuditLogId}", id);
        return await _db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<AuditLog> CreateAsync(AuditLog log)
    {
        _logger.LogInformation("Creating audit log for entity {EntityName}", log.EntityName);
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
        return log;
    }
}
