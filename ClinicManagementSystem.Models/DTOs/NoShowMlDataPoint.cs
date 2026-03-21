namespace ClinicManagementSystem.Models.DTOs;

public class NoShowMlDataPoint
{
    public float PatientAge { get; set; }

    public float PreviousNoShows { get; set; }

    public float PreviousCompletedVisits { get; set; }

    public float DaysBetweenBookingAndAppointment { get; set; }

    public string DayOfWeek { get; set; } = string.Empty;

    public string AppointmentType { get; set; } = string.Empty;

    public bool ReminderSent { get; set; }

    public bool HasInsurance { get; set; }

    public bool Label { get; set; }

    public float ExampleWeight { get; set; }
}
