using ClinicManagementSystem.Models.DTOs;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync();
    Task<IEnumerable<AppointmentTrendPoint>> GetAppointmentTrendAsync(int days = 30);
    Task<IEnumerable<StaffWorkloadSummary>> GetStaffWorkloadAsync();
}
