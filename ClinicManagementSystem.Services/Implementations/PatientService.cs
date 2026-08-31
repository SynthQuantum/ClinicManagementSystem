using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

public class PatientService : IPatientService
{
    private readonly ClinicDbContext _db;
    private readonly ILogger<PatientService> _logger;

    public PatientService(ClinicDbContext db, ILogger<PatientService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Patient>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all patients");
        return await _db.Patients.AsNoTracking().OrderBy(p => p.LastName).ToListAsync();
    }

    public async Task<(IEnumerable<Patient> Items, int TotalCount)> QueryAsync(
        string? searchTerm,
        int page,
        int pageSize,
        string? sortBy,
        bool sortDescending)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 200);

        IQueryable<Patient> query = _db.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lower = searchTerm.ToLower();
            query = query.Where(p => p.FirstName.ToLower().Contains(lower)
                                     || p.LastName.ToLower().Contains(lower)
                                     || (p.Email != null && p.Email.ToLower().Contains(lower))
                                     || (p.PhoneNumber != null && p.PhoneNumber.Contains(searchTerm)));
        }

        query = ApplySorting(query, sortBy, sortDescending);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Patient?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching patient {PatientId}", id);
        return await _db.Patients
            .Include(p => p.Appointments)
            .Include(p => p.VisitRecords)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Patient> CreateAsync(Patient patient)
    {
        _logger.LogInformation("Creating patient {FullName}", patient.FullName);
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Patient created with id {PatientId}", patient.Id);
        return patient;
    }

    public async Task<Patient> UpdateAsync(Patient patient)
    {
        _logger.LogInformation("Updating patient {PatientId}", patient.Id);
        _db.Patients.Update(patient);
        await _db.SaveChangesAsync();
        return patient;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient is null)
        {
            _logger.LogWarning("Patient {PatientId} not found for deletion", id);
            return false;
        }

        patient.IsDeleted = true;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Patient {PatientId} soft-deleted", id);
        return true;
    }

    public async Task<IEnumerable<Patient>> SearchAsync(string searchTerm)
    {
        _logger.LogInformation("Searching patients with term: {Term}", searchTerm);
        var lower = searchTerm.ToLower();
        return await _db.Patients.AsNoTracking()
            .Where(p => p.FirstName.ToLower().Contains(lower)
                     || p.LastName.ToLower().Contains(lower)
                     || (p.Email != null && p.Email.ToLower().Contains(lower))
                     || (p.PhoneNumber != null && p.PhoneNumber.Contains(searchTerm)))
            .OrderBy(p => p.LastName)
            .ToListAsync();
    }

    private static IQueryable<Patient> ApplySorting(IQueryable<Patient> query, string? sortBy, bool sortDescending)
    {
        var sortField = string.IsNullOrWhiteSpace(sortBy) ? "lastName" : sortBy.Trim().ToLowerInvariant();

        return (sortField, sortDescending) switch
        {
            ("firstname", false) => query.OrderBy(p => p.FirstName).ThenBy(p => p.LastName),
            ("firstname", true) => query.OrderByDescending(p => p.FirstName).ThenByDescending(p => p.LastName),
            ("dateofbirth", false) => query.OrderBy(p => p.DateOfBirth),
            ("dateofbirth", true) => query.OrderByDescending(p => p.DateOfBirth),
            ("email", false) => query.OrderBy(p => p.Email),
            ("email", true) => query.OrderByDescending(p => p.Email),
            ("createdat", false) => query.OrderBy(p => p.CreatedAt),
            ("createdat", true) => query.OrderByDescending(p => p.CreatedAt),
            ("lastname", true) => query.OrderByDescending(p => p.LastName).ThenByDescending(p => p.FirstName),
            _ => query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
        };
    }
}
