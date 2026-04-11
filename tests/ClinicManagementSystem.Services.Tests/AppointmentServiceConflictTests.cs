using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Implementations;
using ClinicManagementSystem.Services.Tests.Builders;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicManagementSystem.Services.Tests;

/// <summary>
/// Extended edge-case tests for appointment conflict detection, query filtering,
/// and status/delete transitions. The core create/conflict tests live in
/// <see cref="AppointmentServiceTests"/>.
/// </summary>
public class AppointmentServiceConflictTests
{
    private static readonly DateTime Tomorrow = DateTime.UtcNow.Date.AddDays(1);

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static async Task<(Patient patient, StaffMember staff)> SeedPatientAndStaffAsync(Data.ClinicDbContext db)
    {
        var patient = PatientBuilder.Default().Build();
        var staff = StaffMemberBuilder.Default().Build();
        db.Patients.Add(patient);
        db.StaffMembers.Add(staff);
        await db.SaveChangesAsync();
        return (patient, staff);
    }

    // -----------------------------------------------------------------------
    // Conflict: adjacent slots should NOT conflict
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ShouldNotConflict_WhenAppointmentsAreAdjacent()
    {
        // Same staff, slot #2 starts exactly when slot #1 ends — not an overlap
        using var db = TestDbContextFactory.Create();
        var (patient, staff) = await SeedPatientAndStaffAsync(db);
        var patient2 = PatientBuilder.Default().WithName("Other", "Pat").Build();
        db.Patients.Add(patient2);
        db.Appointments.Add(AppointmentBuilder
            .For(patient.Id, staff.Id)
            .OnDate(Tomorrow)
            .WithSlot(new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0))
            .Build());
        await db.SaveChangesAsync();

        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);

        // 10:00-11:00 starts exactly when previous ends — should succeed
        var adjacent = AppointmentBuilder
            .For(patient2.Id, staff.Id)
            .OnDate(Tomorrow)
            .WithSlot(new TimeSpan(10, 0, 0), new TimeSpan(11, 0, 0))
            .Build();

        var act = async () => await sut.CreateAsync(adjacent);

        await act.Should().NotThrowAsync();
    }

    // -----------------------------------------------------------------------
    // Conflict: cancelled appointments must not block the slot
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ShouldNotConflict_WhenExistingAppointmentIsCancelled()
    {
        using var db = TestDbContextFactory.Create();
        var (patient, staff) = await SeedPatientAndStaffAsync(db);
        var patient2 = PatientBuilder.Default().WithName("Sec", "Pat").Build();
        db.Patients.Add(patient2);

        // Seed a CANCELLED appointment that occupies 10:00-11:00
        db.Appointments.Add(AppointmentBuilder
            .For(patient.Id, staff.Id)
            .OnDate(Tomorrow)
            .WithSlot(new TimeSpan(10, 0, 0), new TimeSpan(11, 0, 0))
            .WithStatus(AppointmentStatus.Cancelled)
            .Build());
        await db.SaveChangesAsync();

        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);

        var overlapping = AppointmentBuilder
            .For(patient2.Id, staff.Id)
            .OnDate(Tomorrow)
            .WithSlot(new TimeSpan(10, 30, 0), new TimeSpan(11, 30, 0))
            .Build();

        var act = async () => await sut.CreateAsync(overlapping);

        await act.Should().NotThrowAsync();
    }

    // -----------------------------------------------------------------------
    // Conflict: same patient, different staff, overlapping time → patient conflict
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenSamePatientOverlaps_WithDifferentStaff()
    {
        using var db = TestDbContextFactory.Create();
        var patient = PatientBuilder.Default().Build();
        var staff1 = StaffMemberBuilder.Default().WithName("Dr.", "Alpha").WithEmail("a@test.local").Build();
        var staff2 = StaffMemberBuilder.Default().WithName("Dr.", "Beta").WithEmail("b@test.local").Build();
        db.Patients.Add(patient);
        db.StaffMembers.AddRange(staff1, staff2);

        db.Appointments.Add(AppointmentBuilder
            .For(patient.Id, staff1.Id)
            .OnDate(Tomorrow)
            .WithSlot(new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0))
            .Build());
        await db.SaveChangesAsync();

        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);

        // Same patient, different staff but overlapping time → conflict
        var conflicting = AppointmentBuilder
            .For(patient.Id, staff2.Id)
            .OnDate(Tomorrow)
            .WithSlot(new TimeSpan(9, 30, 0), new TimeSpan(10, 30, 0))
            .Build();

        var act = async () => await sut.CreateAsync(conflicting);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*patient*");
    }

    // -----------------------------------------------------------------------
    // Conflict: cancelling a new appointment itself must not throw
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ShouldNotConflict_WhenNewAppointmentIsItself_Cancelled()
    {
        using var db = TestDbContextFactory.Create();
        var (patient, staff) = await SeedPatientAndStaffAsync(db);
        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);

        // A cancelled new appointment should skip the conflict check
        var cancelled = AppointmentBuilder
            .For(patient.Id, staff.Id)
            .OnDate(Tomorrow)
            .WithSlot(new TimeSpan(10, 0, 0), new TimeSpan(11, 0, 0))
            .WithStatus(AppointmentStatus.Cancelled)
            .Build();

        var act = async () => await sut.CreateAsync(cancelled);

        await act.Should().NotThrowAsync();
    }

    // -----------------------------------------------------------------------
    // UpdateAsync: updating to an overlapping slot throws
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ShouldThrowConflict_WhenUpdatedSlotOverlapsAnother()
    {
        using var db = TestDbContextFactory.Create();
        var (patient, staff) = await SeedPatientAndStaffAsync(db);
        var patient2 = PatientBuilder.Default().WithName("P2", "L2").Build();
        db.Patients.Add(patient2);

        // Block 10:00-11:00 with a second appointment
        var blocker = AppointmentBuilder
            .For(patient2.Id, staff.Id)
            .OnDate(Tomorrow)
            .WithSlot(new TimeSpan(10, 0, 0), new TimeSpan(11, 0, 0))
            .Build();
        db.Appointments.Add(blocker);

        // Original appointment at 9:00-10:00
        var original = AppointmentBuilder
            .For(patient.Id, staff.Id)
            .OnDate(Tomorrow)
            .WithSlot(new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0))
            .Build();
        db.Appointments.Add(original);
        await db.SaveChangesAsync();

        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);

        // Move original into conflict zone
        original.StartTime = new TimeSpan(10, 30, 0);
        original.EndTime = new TimeSpan(11, 30, 0);

        var act = async () => await sut.UpdateAsync(original);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*conflict*");
    }

    // -----------------------------------------------------------------------
    // UpdateStatusAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateStatusAsync_ShouldReturnTrue_WhenAppointmentExists()
    {
        using var db = TestDbContextFactory.Create();
        var (patient, staff) = await SeedPatientAndStaffAsync(db);
        var appt = AppointmentBuilder.For(patient.Id, staff.Id).OnDate(Tomorrow).Build();
        db.Appointments.Add(appt);
        await db.SaveChangesAsync();

        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);
        var result = await sut.UpdateStatusAsync(appt.Id, AppointmentStatus.Completed);

        result.Should().BeTrue();
        var stored = await db.Appointments.FindAsync(appt.Id);
        stored!.Status.Should().Be(AppointmentStatus.Completed);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldReturnFalse_WhenAppointmentDoesNotExist()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);

        var result = await sut.UpdateStatusAsync(Guid.NewGuid(), AppointmentStatus.Cancelled);

        result.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenAppointmentDoesNotExist()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);

        var result = await sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Query filters
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByPatientAsync_ShouldReturnOnlyAppointmentsForThatPatient()
    {
        using var db = TestDbContextFactory.Create();
        var patient1 = PatientBuilder.Default().WithName("Alice", "A").Build();
        var patient2 = PatientBuilder.Default().WithName("Bob", "B").Build();
        var staff = StaffMemberBuilder.Default().Build();
        db.Patients.AddRange(patient1, patient2);
        db.StaffMembers.Add(staff);

        db.Appointments.AddRange(
            AppointmentBuilder.For(patient1.Id, staff.Id).OnDate(Tomorrow).WithSlot(new(9, 0, 0), new(9, 30, 0)).Build(),
            AppointmentBuilder.For(patient1.Id, staff.Id).OnDate(Tomorrow).WithSlot(new(10, 0, 0), new(10, 30, 0)).Build(),
            AppointmentBuilder.For(patient2.Id, staff.Id).OnDate(Tomorrow).WithSlot(new(11, 0, 0), new(11, 30, 0)).Build()
        );
        await db.SaveChangesAsync();

        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);
        var results = (await sut.GetByPatientAsync(patient1.Id)).ToList();

        results.Should().HaveCount(2);
        results.Should().AllSatisfy(a => a.PatientId.Should().Be(patient1.Id));
    }

    [Fact]
    public async Task GetByStaffAsync_ShouldReturnOnlyAppointmentsForThatStaff()
    {
        using var db = TestDbContextFactory.Create();
        var patient = PatientBuilder.Default().Build();
        var staff1 = StaffMemberBuilder.Default().WithEmail("s1@test.local").Build();
        var staff2 = StaffMemberBuilder.Default().WithEmail("s2@test.local").Build();
        db.Patients.Add(patient);
        db.StaffMembers.AddRange(staff1, staff2);

        db.Appointments.AddRange(
            AppointmentBuilder.For(patient.Id, staff1.Id).OnDate(Tomorrow).WithSlot(new(9, 0, 0), new(9, 30, 0)).Build(),
            AppointmentBuilder.For(patient.Id, staff2.Id).OnDate(Tomorrow.AddDays(1)).WithSlot(new(9, 0, 0), new(9, 30, 0)).Build()
        );
        await db.SaveChangesAsync();

        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);
        var results = (await sut.GetByStaffAsync(staff1.Id)).ToList();

        results.Should().HaveCount(1);
        results[0].StaffMemberId.Should().Be(staff1.Id);
    }

    [Fact]
    public async Task GetByDateAsync_ShouldReturnOnlyAppointmentsOnSpecifiedDate()
    {
        using var db = TestDbContextFactory.Create();
        var patient = PatientBuilder.Default().Build();
        var staff = StaffMemberBuilder.Default().Build();
        db.Patients.Add(patient);
        db.StaffMembers.Add(staff);

        var targetDate = DateTime.UtcNow.Date.AddDays(3);
        db.Appointments.AddRange(
            AppointmentBuilder.For(patient.Id, staff.Id).OnDate(targetDate).WithSlot(new(9, 0, 0), new(9, 30, 0)).Build(),
            AppointmentBuilder.For(patient.Id, staff.Id).OnDate(targetDate.AddDays(1)).WithSlot(new(9, 0, 0), new(9, 30, 0)).Build()
        );
        await db.SaveChangesAsync();

        var sut = new AppointmentService(db, NullLogger<AppointmentService>.Instance);
        var results = (await sut.GetByDateAsync(targetDate)).ToList();

        results.Should().HaveCount(1);
        results[0].AppointmentDate.Date.Should().Be(targetDate.Date);
    }
}
