using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicManagementSystem.Services.Tests;

public class AppointmentServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistAppointment()
    {
        using var db = TestDbContextFactory.Create();
        var (patient, staff) = await SeedPatientAndStaffAsync(db);
        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            StaffMemberId = staff.Id,
            AppointmentDate = DateTime.UtcNow.Date.AddDays(1),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(9, 30, 0),
            Status = AppointmentStatus.Scheduled,
            AppointmentType = AppointmentType.General
        };

        var created = await sut.CreateAsync(appointment);

        created.Id.Should().NotBeEmpty();
        var all = (await sut.GetAllAsync()).ToList();
        all.Should().ContainSingle(a => a.Id == created.Id);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenStartAfterEnd()
    {
        using var db = TestDbContextFactory.Create();
        var (patient, staff) = await SeedPatientAndStaffAsync(db);
        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);

        var invalid = new Appointment
        {
            PatientId = patient.Id,
            StaffMemberId = staff.Id,
            AppointmentDate = DateTime.UtcNow.Date.AddDays(1),
            StartTime = new TimeSpan(11, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Status = AppointmentStatus.Scheduled
        };

        var act = async () => await sut.CreateAsync(invalid);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*start time must be earlier*");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenOverlappingForSameStaff()
    {
        using var db = TestDbContextFactory.Create();
        var (patient1, staff) = await SeedPatientAndStaffAsync(db);
        var patient2 = new Patient
        {
            FirstName = "Second",
            LastName = "Patient",
            DateOfBirth = new DateTime(1993, 2, 2)
        };
        db.Patients.Add(patient2);

        db.Appointments.Add(new Appointment
        {
            PatientId = patient1.Id,
            StaffMemberId = staff.Id,
            AppointmentDate = DateTime.UtcNow.Date.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            Status = AppointmentStatus.Scheduled
        });
        await db.SaveChangesAsync();

        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);

        var overlapping = new Appointment
        {
            PatientId = patient2.Id,
            StaffMemberId = staff.Id,
            AppointmentDate = DateTime.UtcNow.Date.AddDays(1),
            StartTime = new TimeSpan(10, 30, 0),
            EndTime = new TimeSpan(11, 30, 0),
            Status = AppointmentStatus.Confirmed
        };

        var act = async () => await sut.CreateAsync(overlapping);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Scheduling conflict*");
    }

    [Fact]
    public async Task UpdateStatusAndDelete_ShouldWork()
    {
        using var db = TestDbContextFactory.Create();
        var (patient, staff) = await SeedPatientAndStaffAsync(db);
        var appointment = new Appointment
        {
            PatientId = patient.Id,
            StaffMemberId = staff.Id,
            AppointmentDate = DateTime.UtcNow.Date,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(8, 30, 0),
            Status = AppointmentStatus.Scheduled
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);

        var statusUpdated = await sut.UpdateStatusAsync(appointment.Id, AppointmentStatus.Completed);
        var deleted = await sut.DeleteAsync(appointment.Id);

        statusUpdated.Should().BeTrue();
        deleted.Should().BeTrue();
    }

    private static async Task<(Patient patient, StaffMember staff)> SeedPatientAndStaffAsync(ClinicManagementSystem.Data.ClinicDbContext db)
    {
        var patient = new Patient
        {
            FirstName = "Base",
            LastName = "Patient",
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        var staff = new StaffMember
        {
            FirstName = "Base",
            LastName = "Doctor",
            Email = "base.doctor@test.local",
            Role = UserRole.Doctor
        };

        db.Patients.Add(patient);
        db.StaffMembers.Add(staff);
        await db.SaveChangesAsync();

        return (patient, staff);
    }
}
