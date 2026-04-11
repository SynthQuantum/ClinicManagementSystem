using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Implementations;
using ClinicManagementSystem.Services.Options;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClinicManagementSystem.Services.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task ProcessRemindersAsync_ShouldCreateRemindersAndPreventDuplicates()
    {
        using var db = TestDbContextFactory.Create();
        var appointment = await SeedUpcomingAppointmentAsync(db, DateTime.UtcNow.Date.AddDays(1), includePhone: true, includeEmail: true);
        var sut = CreateService(db, enableSecondReminder: true);

        var first = await sut.ProcessRemindersAsync();
        var second = await sut.ProcessRemindersAsync();

        first.CreatedReminderCount.Should().BeGreaterThan(0);
        second.CreatedReminderCount.Should().Be(0);
        second.DuplicateReminderCount.Should().BeGreaterThan(0);

        var notifications = await db.Notifications.Where(n => n.AppointmentId == appointment.Id).ToListAsync();
        notifications.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SendAsync_ShouldMarkFailedAndStoreFailureReason_WhenRecipientInvalid()
    {
        using var db = TestDbContextFactory.Create();
        var appointment = await SeedUpcomingAppointmentAsync(db, DateTime.UtcNow.Date.AddDays(2), includePhone: false, includeEmail: true);

        var notification = new Notification
        {
            AppointmentId = appointment.Id,
            NotificationType = NotificationType.AppointmentReminder,
            Recipient = "invalid-recipient",
            Message = "Test",
            ScheduledFor = DateTime.UtcNow.AddMinutes(-1),
            Status = NotificationStatus.Pending
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var sut = CreateService(db, enableSecondReminder: false);
        var sent = await sut.SendAsync(notification.Id);

        sent.Should().BeFalse();

        var persisted = await db.Notifications.FirstAsync(n => n.Id == notification.Id);
        persisted.Status.Should().Be(NotificationStatus.Failed);
        persisted.FailureReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ProcessRemindersAsync_ShouldSendDueNotifications()
    {
        using var db = TestDbContextFactory.Create();

        var appointmentDate = DateTime.UtcNow.Date.AddDays(1);
        var appointment = await SeedUpcomingAppointmentAsync(db, appointmentDate, includePhone: false, includeEmail: true);

        db.Notifications.Add(new Notification
        {
            AppointmentId = appointment.Id,
            NotificationType = NotificationType.AppointmentReminder,
            Recipient = "patient@example.com",
            Message = "Due reminder",
            ScheduledFor = DateTime.UtcNow.AddMinutes(-5),
            Status = NotificationStatus.Pending
        });
        await db.SaveChangesAsync();

        var sut = CreateService(db, enableSecondReminder: false);
        var result = await sut.ProcessRemindersAsync();

        result.SentCount.Should().BeGreaterThan(0);

        var sentRows = await db.Notifications.Where(n => n.Status == NotificationStatus.Sent).ToListAsync();
        sentRows.Should().NotBeEmpty();
    }

    private static NotificationService CreateService(ClinicManagementSystem.Data.ClinicDbContext db, bool enableSecondReminder)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new NotificationReminderOptions
        {
            Enabled = true,
            EnableSecondReminder = enableSecondReminder,
            FirstReminderHoursBefore = 24,
            SecondReminderHoursBefore = 2,
            AppointmentLookAheadDays = 30,
            ProcessingBatchSize = 100,
            ProcessorIntervalSeconds = 60
        });

        return new NotificationService(
            db,
            new LoggingEmailSender(NullLogger<LoggingEmailSender>.Instance),
            new LoggingSmsSender(NullLogger<LoggingSmsSender>.Instance),
            options,
            NullLogger<NotificationService>.Instance);
    }

    private static async Task<Appointment> SeedUpcomingAppointmentAsync(
        ClinicManagementSystem.Data.ClinicDbContext db,
        DateTime appointmentDate,
        bool includePhone,
        bool includeEmail)
    {
        var patient = new Patient
        {
            FirstName = "Notify",
            LastName = "Patient",
            DateOfBirth = new DateTime(1992, 4, 1),
            Email = includeEmail ? "patient@example.com" : null,
            PhoneNumber = includePhone ? "+15551234567" : null
        };

        var staff = new StaffMember
        {
            FirstName = "Notify",
            LastName = "Doctor",
            Email = $"notify.doctor.{Guid.NewGuid():N}@test.local",
            Role = UserRole.Doctor,
            Specialty = "General"
        };

        db.Patients.Add(patient);
        db.StaffMembers.Add(staff);
        await db.SaveChangesAsync();

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            StaffMemberId = staff.Id,
            AppointmentType = AppointmentType.Checkup,
            AppointmentDate = appointmentDate,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Status = AppointmentStatus.Scheduled,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        return appointment;
    }
}
