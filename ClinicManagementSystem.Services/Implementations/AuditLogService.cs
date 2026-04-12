using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

public class AuditLogService : IAuditLogService
{
    // Action types that represent security or authentication events.
    private static readonly HashSet<string> SecurityActionTypes =
    [
        "LoginFailed", "AccountLockedOut", "UnauthorizedAccess", "Logout"
    ];

    private readonly ClinicDbContext _db;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(ClinicDbContext db, ILogger<AuditLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync(int take = 500)
    {
        _logger.LogInformation("Fetching audit logs (take={Take})", take);
        return await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching audit log {AuditLogId}", id);
        return await _db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<AuditLog> CreateAsync(AuditLog log)
    {
        _logger.LogInformation("Creating audit log: Entity={EntityName} Action={ActionType} User={UserId} Outcome={Outcome}",
            log.EntityName, log.ActionType, log.PerformedByUserId, log.Outcome);
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
        return log;
    }

    public async Task<IEnumerable<AuditLog>> GetByUserAsync(Guid userId, int take = 200)
    {
        _logger.LogInformation("Fetching audit logs for user {UserId} (take={Take})", userId, take);
        return await _db.AuditLogs
            .AsNoTracking()
            .Where(a => a.PerformedByUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, Guid? entityId = null, int take = 200)
    {
        _logger.LogInformation("Fetching audit logs for entity {EntityName}/{EntityId} (take={Take})", entityName, entityId, take);
        var query = _db.AuditLogs.AsNoTracking().Where(a => a.EntityName == entityName);
        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId.Value);
        return await query.OrderByDescending(a => a.CreatedAt).Take(take).ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, int maxResults = 500)
    {
        _logger.LogInformation("Fetching audit logs from {From} to {To} (max={Max})", from, to, maxResults);
        return await _db.AuditLogs
            .AsNoTracking()
            .Where(a => a.CreatedAt >= from && a.CreatedAt <= to)
            .OrderByDescending(a => a.CreatedAt)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetSecurityEventsAsync(int take = 100)
    {
        _logger.LogInformation("Fetching security audit events (take={Take})", take);
        return await _db.AuditLogs
            .AsNoTracking()
            .Where(a => SecurityActionTypes.Contains(a.ActionType) || a.Outcome == "Failure")
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();
    }
}
