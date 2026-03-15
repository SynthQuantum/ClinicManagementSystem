using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly ClinicDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ClinicDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Notification>> GetPendingAsync()
    {
        _logger.LogInformation("Fetching pending notifications");
        return await _db.Notifications
            .AsNoTracking()
            .Include(n => n.Appointment)
            .Where(n => n.Status == NotificationStatus.Pending && n.ScheduledFor <= DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<bool> SendAsync(Guid notificationId)
    {
        var notification = await _db.Notifications.FindAsync(notificationId);
        if (notification is null)
        {
            _logger.LogWarning("Notification {NotificationId} not found for sending", notificationId);
            return false;
        }

        // Placeholder: integrate with email/SMS provider here
        _logger.LogInformation("Sending notification {NotificationId} to {Recipient}", notificationId, notification.Recipient);
        notification.Status = NotificationStatus.Sent;
        notification.SentAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Notification {NotificationId} sent successfully", notificationId);
        return true;
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        _logger.LogInformation("Creating notification for appointment {AppointmentId}", notification.AppointmentId);
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
        return notification;
    }
}
