using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Models.DTOs;

public class NoShowPredictionInput
{
    [Range(0, 120)]
    public int PatientAge { get; set; }

    [Range(0, 365)]
    public int DaysBetweenBookingAndAppointment { get; set; }

    [Range(0, 100)]
    public int PreviousNoShowCount { get; set; }

    [Range(0, 100)]
    public int PreviousCompletedCount { get; set; }

    public AppointmentType AppointmentType { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool HasInsurance { get; set; }
    public bool HasReminderSent { get; set; }
}
