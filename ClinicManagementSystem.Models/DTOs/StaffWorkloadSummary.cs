namespace ClinicManagementSystem.Models.DTOs;

public class StaffWorkloadSummary
{
    public Guid StaffMemberId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int NoShows { get; set; }
}
