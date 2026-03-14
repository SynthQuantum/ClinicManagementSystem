using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

public class AppointmentService : IAppointmentService
{
    private readonly ClinicDbContext _db;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(ClinicDbContext db, ILogger<AppointmentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Appointment>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all appointments");
        return await _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.StaffMember)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetByDateAsync(DateTime date)
    {
        _logger.LogInformation("Fetching appointments for date {Date}", date.Date);
        return await _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.StaffMember)
            .Where(a => a.AppointmentDate.Date == date.Date)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetByPatientAsync(Guid patientId)
    {
        _logger.LogInformation("Fetching appointments for patient {PatientId}", patientId);
        return await _db.Appointments
            .AsNoTracking()
            .Include(a => a.StaffMember)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetByStaffAsync(Guid staffMemberId)
    {
        _logger.LogInformation("Fetching appointments for staff {StaffId}", staffMemberId);
        return await _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Where(a => a.StaffMemberId == staffMemberId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();
    }

    public async Task<Appointment?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching appointment {AppointmentId}", id);
        return await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.StaffMember)
            .Include(a => a.Notifications)
            .Include(a => a.VisitRecords)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Appointment> CreateAsync(Appointment appointment)
    {
        _logger.LogInformation("Creating appointment for patient {PatientId} on {Date}", appointment.PatientId, appointment.AppointmentDate);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Appointment created with id {AppointmentId}", appointment.Id);
        return appointment;
    }

    public async Task<Appointment> UpdateAsync(Appointment appointment)
    {
        _logger.LogInformation("Updating appointment {AppointmentId}", appointment.Id);
        _db.Appointments.Update(appointment);
        await _db.SaveChangesAsync();
        return appointment;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, AppointmentStatus status)
    {
        var appointment = await _db.Appointments.FindAsync(id);
        if (appointment is null)
        {
            _logger.LogWarning("Appointment {AppointmentId} not found for status update", id);
            return false;
        }

        _logger.LogInformation("Updating appointment {AppointmentId} status to {Status}", id, status);
        appointment.Status = status;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var appointment = await _db.Appointments.FindAsync(id);
        if (appointment is null)
        {
            _logger.LogWarning("Appointment {AppointmentId} not found for deletion", id);
            return false;
        }

        appointment.IsDeleted = true;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Appointment {AppointmentId} soft-deleted", id);
        return true;
    }
}
