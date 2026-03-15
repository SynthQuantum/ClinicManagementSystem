namespace ClinicManagementSystem.Models.DTOs;

public class DashboardSummary
{
    public int TotalPatients { get; set; }
    public int TotalAppointments { get; set; }
    public int TodayAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int NoShowAppointments { get; set; }
    public decimal NoShowRate => TotalAppointments > 0
        ? Math.Round((decimal)NoShowAppointments / TotalAppointments * 100, 2)
        : 0;
}
