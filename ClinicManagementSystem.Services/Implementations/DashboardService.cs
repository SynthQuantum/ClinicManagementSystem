using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly ClinicDbContext _db;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ClinicDbContext db, ILogger<DashboardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DashboardSummary> GetSummaryAsync()
    {
        _logger.LogInformation("Building dashboard summary");
        var today = DateTime.UtcNow.Date;

        var summary = new DashboardSummary
        {
            TotalPatients = await _db.Patients.CountAsync(),
            TotalAppointments = await _db.Appointments.CountAsync(),
            TodayAppointments = await _db.Appointments.CountAsync(a => a.AppointmentDate.Date == today),
            CompletedAppointments = await _db.Appointments.CountAsync(a => a.Status == AppointmentStatus.Completed),
            CancelledAppointments = await _db.Appointments.CountAsync(a => a.Status == AppointmentStatus.Cancelled),
            NoShowAppointments = await _db.Appointments.CountAsync(a => a.Status == AppointmentStatus.NoShow)
        };

        _logger.LogInformation("Dashboard summary built: {TotalPatients} patients, {TotalAppointments} appointments", summary.TotalPatients, summary.TotalAppointments);
        return summary;
    }

    public async Task<IEnumerable<AppointmentTrendPoint>> GetAppointmentTrendAsync(int days = 30)
    {
        _logger.LogInformation("Fetching appointment trend for last {Days} days", days);
        var from = DateTime.UtcNow.Date.AddDays(-days);

        var raw = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate.Date >= from)
            .GroupBy(a => a.AppointmentDate.Date)
            .Select(g => new AppointmentTrendPoint { Date = g.Key, Count = g.Count() })
            .OrderBy(p => p.Date)
            .ToListAsync();

        // Fill gaps with zeros
        var result = new List<AppointmentTrendPoint>();
        for (int i = 0; i <= days; i++)
        {
            var date = from.AddDays(i);
            var point = raw.FirstOrDefault(p => p.Date == date);
            result.Add(point ?? new AppointmentTrendPoint { Date = date, Count = 0 });
        }

        return result;
    }

    public async Task<IEnumerable<StaffWorkloadSummary>> GetStaffWorkloadAsync()
    {
        _logger.LogInformation("Fetching staff workload summary");
        return await _db.StaffMembers
            .AsNoTracking()
            .Select(s => new StaffWorkloadSummary
            {
                StaffMemberId = s.Id,
                StaffName = s.FirstName + " " + s.LastName,
                TotalAppointments = s.Appointments.Count,
                CompletedAppointments = s.Appointments.Count(a => a.Status == AppointmentStatus.Completed),
                NoShows = s.Appointments.Count(a => a.Status == AppointmentStatus.NoShow)
            })
            .OrderByDescending(s => s.TotalAppointments)
            .ToListAsync();
    }
}
