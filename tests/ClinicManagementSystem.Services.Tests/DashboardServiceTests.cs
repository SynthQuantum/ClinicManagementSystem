using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicManagementSystem.Services.Tests;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ShouldReturnExpectedCounts()
    {
        using var db = TestDbContextFactory.Create();

        var patient1 = new Patient { FirstName = "P1", LastName = "L1", DateOfBirth = new DateTime(1991, 1, 1) };
        var patient2 = new Patient { FirstName = "P2", LastName = "L2", DateOfBirth = new DateTime(1992, 2, 2) };
        var staff = new StaffMember { FirstName = "S", LastName = "D", Email = "sd@test.local", Role = UserRole.Doctor };

        db.Patients.AddRange(patient1, patient2);
        db.StaffMembers.Add(staff);
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;
        db.Appointments.AddRange(
            new Appointment
            {
                PatientId = patient1.Id,
                StaffMemberId = staff.Id,
                AppointmentDate = today,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(9, 30, 0),
                Status = AppointmentStatus.Completed
            },
            new Appointment
            {
                PatientId = patient2.Id,
                StaffMemberId = staff.Id,
                AppointmentDate = today,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
                Status = AppointmentStatus.Cancelled
            },
            new Appointment
            {
                PatientId = patient2.Id,
                StaffMemberId = staff.Id,
                AppointmentDate = today.AddDays(-1),
                StartTime = new TimeSpan(11, 0, 0),
                EndTime = new TimeSpan(11, 30, 0),
                Status = AppointmentStatus.NoShow
            });
        await db.SaveChangesAsync();

        var sut = new DashboardService(db, NullLogger<DashboardService>.Instance);

        var summary = await sut.GetSummaryAsync();

        summary.TotalPatients.Should().Be(2);
        summary.TotalAppointments.Should().Be(3);
        summary.TodayAppointments.Should().Be(2);
        summary.CompletedAppointments.Should().Be(1);
        summary.CancelledAppointments.Should().Be(1);
        summary.NoShowAppointments.Should().Be(1);
    }
}
