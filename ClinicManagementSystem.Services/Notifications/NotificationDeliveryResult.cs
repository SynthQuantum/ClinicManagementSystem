namespace ClinicManagementSystem.Services.Notifications;

public class NotificationDeliveryResult
{
    public bool Success { get; set; }

    public string? FailureReason { get; set; }

    public static NotificationDeliveryResult Ok() => new() { Success = true };

    public static NotificationDeliveryResult Fail(string reason) => new() { Success = false, FailureReason = reason };
}
