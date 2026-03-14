using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

public class StaffService : IStaffService
{
    private readonly ClinicDbContext _db;
    private readonly ILogger<StaffService> _logger;

    public StaffService(ClinicDbContext db, ILogger<StaffService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<StaffMember>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all staff members");
        return await _db.StaffMembers.AsNoTracking().OrderBy(s => s.LastName).ToListAsync();
    }

    public async Task<StaffMember?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching staff member {StaffId}", id);
        return await _db.StaffMembers
            .Include(s => s.Appointments)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<StaffMember> CreateAsync(StaffMember staff)
    {
        _logger.LogInformation("Creating staff member {FullName}", staff.FullName);
        _db.StaffMembers.Add(staff);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Staff member created with id {StaffId}", staff.Id);
        return staff;
    }

    public async Task<StaffMember> UpdateAsync(StaffMember staff)
    {
        _logger.LogInformation("Updating staff member {StaffId}", staff.Id);
        _db.StaffMembers.Update(staff);
        await _db.SaveChangesAsync();
        return staff;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var staff = await _db.StaffMembers.FindAsync(id);
        if (staff is null)
        {
            _logger.LogWarning("Staff member {StaffId} not found for deletion", id);
            return false;
        }

        staff.IsDeleted = true;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Staff member {StaffId} soft-deleted", id);
        return true;
    }
}
