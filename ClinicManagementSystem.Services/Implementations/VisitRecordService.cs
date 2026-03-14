using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

public class VisitRecordService : IVisitRecordService
{
    private readonly ClinicDbContext _db;
    private readonly ILogger<VisitRecordService> _logger;

    public VisitRecordService(ClinicDbContext db, ILogger<VisitRecordService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<VisitRecord>> GetByPatientAsync(Guid patientId)
    {
        _logger.LogInformation("Fetching visit records for patient {PatientId}", patientId);
        return await _db.VisitRecords
            .AsNoTracking()
            .Include(v => v.StaffMember)
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.VisitDate)
            .ToListAsync();
    }

    public async Task<VisitRecord?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching visit record {VisitId}", id);
        return await _db.VisitRecords
            .Include(v => v.Patient)
            .Include(v => v.StaffMember)
            .Include(v => v.Appointment)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<VisitRecord> CreateAsync(VisitRecord record)
    {
        _logger.LogInformation("Creating visit record for patient {PatientId}", record.PatientId);
        _db.VisitRecords.Add(record);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Visit record created with id {VisitId}", record.Id);
        return record;
    }

    public async Task<VisitRecord> UpdateAsync(VisitRecord record)
    {
        _logger.LogInformation("Updating visit record {VisitId}", record.Id);
        _db.VisitRecords.Update(record);
        await _db.SaveChangesAsync();
        return record;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var record = await _db.VisitRecords.FindAsync(id);
        if (record is null)
        {
            _logger.LogWarning("Visit record {VisitId} not found for deletion", id);
            return false;
        }

        record.IsDeleted = true;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Visit record {VisitId} soft-deleted", id);
        return true;
    }
}
