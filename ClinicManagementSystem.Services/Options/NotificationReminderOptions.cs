namespace ClinicManagementSystem.Services.Options;

public class NotificationReminderOptions
{
    public const string SectionName = "NotificationReminders";

    public bool Enabled { get; set; } = true;

    public bool EnableSecondReminder { get; set; } = true;

    public int FirstReminderHoursBefore { get; set; } = 24;

    public int SecondReminderHoursBefore { get; set; } = 2;

    public int AppointmentLookAheadDays { get; set; } = 30;

    public int ProcessingBatchSize { get; set; } = 100;

    public int ProcessorIntervalSeconds { get; set; } = 120;
}
