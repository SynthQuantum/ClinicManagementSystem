using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Models.DTOs;

public class NoShowPredictionInput
{
    public int PatientAge { get; set; }
    public int DaysBetweenBookingAndAppointment { get; set; }
    public int PreviousNoShowCount { get; set; }
    public int PreviousCompletedCount { get; set; }
    public AppointmentType AppointmentType { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool HasInsurance { get; set; }
    public bool HasReminderSent { get; set; }
}
