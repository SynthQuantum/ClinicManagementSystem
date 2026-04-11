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

    public async Task<IEnumerable<Appointment>> SearchAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? patientId = null,
        Guid? staffMemberId = null,
        AppointmentStatus? status = null,
        bool highRiskOnly = false)
    {
        var fromDateValue = fromDate?.Date;
        var toDateValue = toDate?.Date;
        if (fromDateValue.HasValue && toDateValue.HasValue && fromDateValue > toDateValue)
        {
            (fromDateValue, toDateValue) = (toDateValue, fromDateValue);
        }

        _logger.LogInformation(
            "Searching appointments with filters: From={FromDate}, To={ToDate}, PatientId={PatientId}, StaffId={StaffId}, Status={Status}, HighRiskOnly={HighRiskOnly}",
            fromDateValue,
            toDateValue,
            patientId,
            staffMemberId,
            status,
            highRiskOnly);

        var query = _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.StaffMember)
            .AsQueryable();

        if (fromDateValue.HasValue)
        {
            query = query.Where(a => a.AppointmentDate.Date >= fromDateValue.Value);
        }

        if (toDateValue.HasValue)
        {
            query = query.Where(a => a.AppointmentDate.Date <= toDateValue.Value);
        }

        if (patientId.HasValue)
        {
            query = query.Where(a => a.PatientId == patientId.Value);
        }

        if (staffMemberId.HasValue)
        {
            query = query.Where(a => a.StaffMemberId == staffMemberId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (highRiskOnly)
        {
            query = query.Where(a => a.IsPredictedNoShow || (a.NoShowProbability ?? 0m) >= 0.70m);
        }

        return await query
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
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
        await ValidateAppointmentAsync(appointment);
        await EnsureNoSchedulingConflictAsync(appointment);

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Appointment created with id {AppointmentId}", appointment.Id);
        return appointment;
    }

    public async Task<Appointment> UpdateAsync(Appointment appointment)
    {
        _logger.LogInformation("Updating appointment {AppointmentId}", appointment.Id);
        await ValidateAppointmentAsync(appointment);
        await EnsureNoSchedulingConflictAsync(appointment);

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

    private async Task ValidateAppointmentAsync(Appointment appointment)
    {
        if (appointment.StartTime >= appointment.EndTime)
        {
            throw new ArgumentException("Appointment start time must be earlier than end time.");
        }

        var patientExists = await _db.Patients.AnyAsync(p => p.Id == appointment.PatientId);
        if (!patientExists)
        {
            throw new ArgumentException("Selected patient does not exist.");
        }

        var staffExists = await _db.StaffMembers.AnyAsync(s => s.Id == appointment.StaffMemberId);
        if (!staffExists)
        {
            throw new ArgumentException("Selected staff member does not exist.");
        }
    }

    private async Task EnsureNoSchedulingConflictAsync(Appointment appointment)
    {
        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return;
        }

        var day = appointment.AppointmentDate.Date;
        var appointmentId = appointment.Id;
        var conflict = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate.Date == day
                        && a.Status != AppointmentStatus.Cancelled
                        && a.Id != appointmentId
                        && (a.StaffMemberId == appointment.StaffMemberId || a.PatientId == appointment.PatientId)
                        && appointment.StartTime < a.EndTime
                        && appointment.EndTime > a.StartTime)
            .OrderBy(a => a.StartTime)
            .Select(a => new
            {
                a.Id,
                a.PatientId,
                a.StaffMemberId,
                a.StartTime,
                a.EndTime
            })
            .FirstOrDefaultAsync();

        if (conflict is null)
        {
            return;
        }

        var conflictType = conflict.StaffMemberId == appointment.StaffMemberId
            ? "staff member"
            : "patient";

        throw new InvalidOperationException(
            $"Scheduling conflict: selected {conflictType} already has an appointment between {conflict.StartTime:hh\\:mm} and {conflict.EndTime:hh\\:mm}.");
    }
}
