namespace ClinicManagementSystem.Models.Enums;

public enum UserRole
{
    Admin,
    Doctor,
    Nurse,
    Receptionist,
    Patient
}

public enum Gender
{
    Male,
    Female,
    Other,
    PreferNotToSay
}

public enum AppointmentStatus
{
    Scheduled,
    Confirmed,
    InProgress,
    Completed,
    Cancelled,
    NoShow,
    Rescheduled
}

public enum AppointmentType
{
    General,
    FollowUp,
    Consultation,
    Emergency,
    Checkup,
    Procedure,
    Dental,
    Vaccination
}

public enum NotificationType
{
    AppointmentReminder,
    AppointmentConfirmation,
    AppointmentCancellation,
    AppointmentRescheduled,
    NoShowAlert,
    SystemAlert
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed,
    Cancelled
}
