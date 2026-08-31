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

    public async Task<(IEnumerable<StaffMember> Items, int TotalCount)> QueryAsync(
        int page,
        int pageSize,
        string? sortBy,
        bool sortDescending)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 200);

        var query = ApplySorting(_db.StaffMembers.AsNoTracking(), sortBy, sortDescending);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync();

        return (items, totalCount);
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

    private static IQueryable<StaffMember> ApplySorting(IQueryable<StaffMember> query, string? sortBy, bool sortDescending)
    {
        var sortField = string.IsNullOrWhiteSpace(sortBy) ? "lastName" : sortBy.Trim().ToLowerInvariant();

        return (sortField, sortDescending) switch
        {
            ("firstname", false) => query.OrderBy(s => s.FirstName).ThenBy(s => s.LastName),
            ("firstname", true) => query.OrderByDescending(s => s.FirstName).ThenByDescending(s => s.LastName),
            ("email", false) => query.OrderBy(s => s.Email),
            ("email", true) => query.OrderByDescending(s => s.Email),
            ("role", false) => query.OrderBy(s => s.Role),
            ("role", true) => query.OrderByDescending(s => s.Role),
            ("isavailable", false) => query.OrderBy(s => s.IsAvailable),
            ("isavailable", true) => query.OrderByDescending(s => s.IsAvailable),
            ("lastname", true) => query.OrderByDescending(s => s.LastName).ThenByDescending(s => s.FirstName),
            _ => query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
        };
    }
}
