using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Interfaces;
using ClinicManagementSystem.Services.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicManagementSystem.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly ClinicDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly NotificationReminderOptions _options;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ClinicDbContext db,
        IEmailSender emailSender,
        ISmsSender smsSender,
        IOptions<NotificationReminderOptions> options,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<Notification>> GetPendingAsync(int take = 200, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching pending notifications. Take={Take}", take);
        return await _db.Notifications
            .AsNoTracking()
            .Include(n => n.Appointment)
            .Where(n => n.Status == NotificationStatus.Pending && n.ScheduledFor <= DateTime.UtcNow)
            .OrderBy(n => n.ScheduledFor)
            .Take(Math.Clamp(take, 1, 1000))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Notification>> GetHistoryAsync(NotificationStatus? status = null, int take = 100, CancellationToken cancellationToken = default)
    {
        var query = _db.Notifications
            .AsNoTracking()
            .Include(n => n.Appointment)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(n => n.Status == status.Value);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationDashboardSummary> GetSummaryAsync(int recentTake = 25, CancellationToken cancellationToken = default)
    {
        var boundedTake = Math.Clamp(recentTake, 1, 200);

        var pendingCount = await _db.Notifications.CountAsync(n => n.Status == NotificationStatus.Pending, cancellationToken);
        var sentCount = await _db.Notifications.CountAsync(n => n.Status == NotificationStatus.Sent, cancellationToken);
        var failedCount = await _db.Notifications.CountAsync(n => n.Status == NotificationStatus.Failed, cancellationToken);
        var cancelledCount = await _db.Notifications.CountAsync(n => n.Status == NotificationStatus.Cancelled, cancellationToken);

        var recent = await _db.Notifications
            .AsNoTracking()
            .Include(n => n.Appointment)
            .OrderByDescending(n => n.CreatedAt)
            .Take(boundedTake)
            .ToListAsync(cancellationToken);

        return new NotificationDashboardSummary
        {
            PendingCount = pendingCount,
            SentCount = sentCount,
            FailedCount = failedCount,
            CancelledCount = cancelledCount,
            RecentNotifications = recent
        };
    }

    public async Task<bool> SendAsync(Guid notificationId)
    {
        var notification = await _db.Notifications
            .Include(n => n.Appointment)
            .FirstOrDefaultAsync(n => n.Id == notificationId);

        if (notification is null)
        {
            _logger.LogWarning("Notification {NotificationId} not found for sending", notificationId);
            return false;
        }

        if (notification.Status == NotificationStatus.Sent)
        {
            _logger.LogInformation("Notification {NotificationId} already sent. Skipping.", notificationId);
            return true;
        }

        _logger.LogInformation(
            "Sending notification {NotificationId}. Type={Type} Recipient={Recipient} ScheduledFor={ScheduledFor}",
            notification.Id,
            notification.NotificationType,
            notification.Recipient,
            notification.ScheduledFor);

        var delivery = await DeliverAsync(notification);
        if (delivery.Success)
        {
            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
            notification.FailureReason = null;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Notification {NotificationId} sent successfully", notification.Id);
            return true;
        }

        notification.Status = NotificationStatus.Failed;
        notification.FailureReason = delivery.FailureReason ?? "Unknown delivery failure.";
        notification.SentAt = null;
        await _db.SaveChangesAsync();

        _logger.LogWarning(
            "Notification {NotificationId} delivery failed. Reason={Reason}",
            notification.Id,
            notification.FailureReason);

        return false;
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        if (notification.ScheduledFor == default)
        {
            notification.ScheduledFor = DateTime.UtcNow;
        }

        notification.Status = NotificationStatus.Pending;
        notification.SentAt = null;
        notification.FailureReason = null;

        _logger.LogInformation(
            "Creating notification for appointment {AppointmentId}. Recipient={Recipient} ScheduledFor={ScheduledFor}",
            notification.AppointmentId,
            notification.Recipient,
            notification.ScheduledFor);

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
        return notification;
    }

    public async Task<NotificationProcessingResult> ProcessRemindersAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Reminder processing is disabled via configuration.");
            return new NotificationProcessingResult { ProcessedAtUtc = DateTime.UtcNow };
        }

        _logger.LogInformation("Reminder processing started.");

        var created = 0;
        var duplicates = 0;
        var now = DateTime.UtcNow;

        var appointments = await GetUpcomingAppointmentsAsync(now, cancellationToken);
        foreach (var appointment in appointments)
        {
            var firstReminderAt = BuildReminderTimestamp(appointment, _options.FirstReminderHoursBefore);
            var firstResult = await EnsureReminderNotificationsForWindowAsync(appointment, firstReminderAt, cancellationToken);
            created += firstResult.Created;
            duplicates += firstResult.Duplicates;

            if (_options.EnableSecondReminder)
            {
                var secondReminderAt = BuildReminderTimestamp(appointment, _options.SecondReminderHoursBefore);
                var secondResult = await EnsureReminderNotificationsForWindowAsync(appointment, secondReminderAt, cancellationToken);
                created += secondResult.Created;
                duplicates += secondResult.Duplicates;
            }
        }

        var due = await _db.Notifications
            .Where(n => n.Status == NotificationStatus.Pending && n.ScheduledFor <= now)
            .OrderBy(n => n.ScheduledFor)
            .Take(Math.Clamp(_options.ProcessingBatchSize, 1, 1000))
            .Select(n => n.Id)
            .ToListAsync(cancellationToken);

        var sent = 0;
        var failed = 0;
        foreach (var notificationId in due)
        {
            var succeeded = await SendAsync(notificationId);
            if (succeeded)
            {
                sent++;
            }
            else
            {
                failed++;
            }
        }

        var result = new NotificationProcessingResult
        {
            CreatedReminderCount = created,
            DuplicateReminderCount = duplicates,
            DuePendingCount = due.Count,
            SentCount = sent,
            FailedCount = failed,
            ProcessedAtUtc = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Reminder processing completed. Created={Created}, Duplicates={Duplicates}, Due={Due}, Sent={Sent}, Failed={Failed}",
            result.CreatedReminderCount,
            result.DuplicateReminderCount,
            result.DuePendingCount,
            result.SentCount,
            result.FailedCount);

        return result;
    }

    private async Task<(int Created, int Duplicates)> EnsureReminderNotificationsForWindowAsync(
        Appointment appointment,
        DateTime scheduledFor,
        CancellationToken cancellationToken)
    {
        var created = 0;
        var duplicates = 0;

        var recipients = BuildRecipients(appointment.Patient);
        foreach (var recipient in recipients)
        {
            var exists = await _db.Notifications
                .AnyAsync(n => n.AppointmentId == appointment.Id
                               && n.NotificationType == NotificationType.AppointmentReminder
                               && n.Recipient == recipient
                               && n.ScheduledFor == scheduledFor,
                    cancellationToken);

            if (exists)
            {
                duplicates++;
                continue;
            }

            var notification = new Notification
            {
                AppointmentId = appointment.Id,
                NotificationType = NotificationType.AppointmentReminder,
                Status = NotificationStatus.Pending,
                Message = BuildReminderMessage(appointment, scheduledFor),
                ScheduledFor = scheduledFor,
                Recipient = recipient
            };

            _db.Notifications.Add(notification);
            created++;

            _logger.LogInformation(
                "Created reminder notification for appointment {AppointmentId} recipient {Recipient} at {ScheduledFor}",
                appointment.Id,
                recipient,
                scheduledFor);
        }

        if (created > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return (created, duplicates);
    }

    private async Task<List<Appointment>> GetUpcomingAppointmentsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var windowEnd = now.AddDays(Math.Max(1, _options.AppointmentLookAheadDays));

        return await _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Where(a => (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed)
                        && a.AppointmentDate >= now.Date
                        && a.AppointmentDate <= windowEnd.Date)
            .ToListAsync(cancellationToken);
    }

    private async Task<ClinicManagementSystem.Services.Notifications.NotificationDeliveryResult> DeliverAsync(Notification notification)
    {
        if (notification.Recipient.Contains('@'))
        {
            return await _emailSender.SendAsync(
                notification.Recipient,
                "Clinic appointment reminder",
                notification.Message);
        }

        return await _smsSender.SendAsync(notification.Recipient, notification.Message);
    }

    private static DateTime BuildReminderTimestamp(Appointment appointment, int hoursBefore)
    {
        var appointmentStart = appointment.AppointmentDate.Date + appointment.StartTime;
        return appointmentStart.AddHours(-hoursBefore);
    }

    private static string BuildReminderMessage(Appointment appointment, DateTime reminderWindow)
    {
        var appointmentStart = appointment.AppointmentDate.Date + appointment.StartTime;
        return $"Reminder ({reminderWindow:yyyy-MM-dd HH:mm} UTC): appointment on {appointmentStart:yyyy-MM-dd HH:mm} UTC. Please confirm attendance.";
    }

    private static IEnumerable<string> BuildRecipients(Patient? patient)
    {
        if (patient is null)
        {
            return [];
        }

        var recipients = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(patient.Email))
        {
            recipients.Add(patient.Email.Trim());
        }

        if (!string.IsNullOrWhiteSpace(patient.PhoneNumber))
        {
            recipients.Add(patient.PhoneNumber.Trim());
        }

        return recipients;
    }
}
