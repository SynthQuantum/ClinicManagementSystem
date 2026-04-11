using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Implementations;
using ClinicManagementSystem.Services.Tests.Builders;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicManagementSystem.Services.Tests;

/// <summary>
/// Tests for <see cref="DashboardService"/> covering appointment trend computation
/// (including zero-gap fill) and per-staff workload aggregation.
/// </summary>
public class DashboardAggregationTests
{
    // -----------------------------------------------------------------------
    // Trend tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAppointmentTrendAsync_ShouldReturnDaysPlusOnePoints()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new DashboardService(db, NullLogger<DashboardService>.Instance);

        var result = (await sut.GetAppointmentTrendAsync(7)).ToList();

        // days+1 entries: day-7, day-6, …, day-0 (today)
        result.Should().HaveCount(8);
    }

    [Fact]
    public async Task GetAppointmentTrendAsync_ShouldFillGapsWithZero()
    {
        using var db = TestDbContextFactory.Create();
        // Don't add any appointments — every day should be zero
        var sut = new DashboardService(db, NullLogger<DashboardService>.Instance);

        var result = (await sut.GetAppointmentTrendAsync(5)).ToList();

        result.Should().HaveCount(6);
        result.Should().AllSatisfy(p => p.Count.Should().Be(0));
    }

    [Fact]
    public async Task GetAppointmentTrendAsync_ShouldCountAppointmentsOnCorrectDay()
    {
        using var db = TestDbContextFactory.Create();
        var patient = PatientBuilder.Default().Build();
        var staff = StaffMemberBuilder.Default().Build();
        db.Patients.Add(patient);
        db.StaffMembers.Add(staff);

        var targetDate = DateTime.UtcNow.Date.AddDays(-2);
        db.Appointments.AddRange(
            AppointmentBuilder.For(patient.Id, staff.Id).OnDate(targetDate)
                .WithSlot(new(9, 0, 0), new(9, 30, 0)).Build(),
            AppointmentBuilder.For(patient.Id, staff.Id).OnDate(targetDate)
                .WithSlot(new(10, 0, 0), new(10, 30, 0)).Build()
        );
        await db.SaveChangesAsync();

        var sut = new DashboardService(db, NullLogger<DashboardService>.Instance);
        var result = (await sut.GetAppointmentTrendAsync(7)).ToList();

        var point = result.Single(p => p.Date == targetDate);
        point.Count.Should().Be(2);

        // All other days should be zero
        result.Where(p => p.Date != targetDate).Should().AllSatisfy(p => p.Count.Should().Be(0));
    }

    [Fact]
    public async Task GetAppointmentTrendAsync_ShouldExcludeAppointmentsOutsideWindow()
    {
        using var db = TestDbContextFactory.Create();
        var patient = PatientBuilder.Default().Build();
        var staff = StaffMemberBuilder.Default().Build();
        db.Patients.Add(patient);
        db.StaffMembers.Add(staff);

        // 40 days ago — outside a 30-day window
        var old = DateTime.UtcNow.Date.AddDays(-40);
        db.Appointments.Add(
            AppointmentBuilder.For(patient.Id, staff.Id).OnDate(old)
                .WithSlot(new(9, 0, 0), new(9, 30, 0)).Build()
        );
        await db.SaveChangesAsync();

        var sut = new DashboardService(db, NullLogger<DashboardService>.Instance);
        var result = (await sut.GetAppointmentTrendAsync(30)).ToList();

        result.Should().AllSatisfy(p => p.Count.Should().Be(0));
    }

    // -----------------------------------------------------------------------
    // Staff workload tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetStaffWorkloadAsync_ShouldReturnOneRowPerStaffMember()
    {
        using var db = TestDbContextFactory.Create();
        var staff1 = StaffMemberBuilder.Default().WithEmail("w1@test.local").Build();
        var staff2 = StaffMemberBuilder.Default().WithEmail("w2@test.local").Build();
        db.StaffMembers.AddRange(staff1, staff2);
        await db.SaveChangesAsync();

        var sut = new DashboardService(db, NullLogger<DashboardService>.Instance);
        var result = (await sut.GetStaffWorkloadAsync()).ToList();

        result.Should().HaveCount(2);
        result.Should().ContainSingle(r => r.StaffMemberId == staff1.Id);
        result.Should().ContainSingle(r => r.StaffMemberId == staff2.Id);
    }

    [Fact]
    public async Task GetStaffWorkloadAsync_ShouldCountTotalCompletedAndNoShows()
    {
        using var db = TestDbContextFactory.Create();
        var patient = PatientBuilder.Default().Build();
        var staff = StaffMemberBuilder.Default().Build();
        db.Patients.Add(patient);
        db.StaffMembers.Add(staff);

        var today = DateTime.UtcNow.Date;
        db.Appointments.AddRange(
            AppointmentBuilder.For(patient.Id, staff.Id).OnDate(today)
                .WithSlot(new(9, 0, 0), new(9, 30, 0)).WithStatus(AppointmentStatus.Completed).Build(),
            AppointmentBuilder.For(patient.Id, staff.Id).OnDate(today)
                .WithSlot(new(10, 0, 0), new(10, 30, 0)).WithStatus(AppointmentStatus.NoShow).Build(),
            AppointmentBuilder.For(patient.Id, staff.Id).OnDate(today)
                .WithSlot(new(11, 0, 0), new(11, 30, 0)).WithStatus(AppointmentStatus.Scheduled).Build()
        );
        await db.SaveChangesAsync();

        var sut = new DashboardService(db, NullLogger<DashboardService>.Instance);
        var result = (await sut.GetStaffWorkloadAsync()).ToList();

        var workload = result.Single(r => r.StaffMemberId == staff.Id);
        workload.TotalAppointments.Should().Be(3);
        workload.CompletedAppointments.Should().Be(1);
        workload.NoShows.Should().Be(1);
    }

    [Fact]
    public async Task GetStaffWorkloadAsync_ShouldOrderByTotalAppointmentsDescending()
    {
        using var db = TestDbContextFactory.Create();
        var patient = PatientBuilder.Default().Build();
        var staffBusy = StaffMemberBuilder.Default().WithEmail("busy@test.local").Build();
        var staffFree = StaffMemberBuilder.Default().WithEmail("free@test.local").Build();
        db.Patients.Add(patient);
        db.StaffMembers.AddRange(staffBusy, staffFree);

        var today = DateTime.UtcNow.Date;
        // staffBusy gets 3 appointments, staffFree gets 1
        db.Appointments.AddRange(
            AppointmentBuilder.For(patient.Id, staffBusy.Id).OnDate(today)
                .WithSlot(new(9, 0, 0), new(9, 30, 0)).Build(),
            AppointmentBuilder.For(patient.Id, staffBusy.Id).OnDate(today)
                .WithSlot(new(10, 0, 0), new(10, 30, 0)).Build(),
            AppointmentBuilder.For(patient.Id, staffBusy.Id).OnDate(today)
                .WithSlot(new(11, 0, 0), new(11, 30, 0)).Build(),
            AppointmentBuilder.For(patient.Id, staffFree.Id).OnDate(today.AddDays(1))
                .WithSlot(new(9, 0, 0), new(9, 30, 0)).Build()
        );
        await db.SaveChangesAsync();

        var sut = new DashboardService(db, NullLogger<DashboardService>.Instance);
        var result = (await sut.GetStaffWorkloadAsync()).ToList();

        result[0].StaffMemberId.Should().Be(staffBusy.Id);
        result[0].TotalAppointments.Should().Be(3);
        result[1].TotalAppointments.Should().Be(1);
    }

    [Fact]
    public async Task GetStaffWorkloadAsync_ShouldIncludeStaffName()
    {
        using var db = TestDbContextFactory.Create();
        var staff = StaffMemberBuilder.Default()
            .WithName("Jane", "Doe")
            .Build();
        db.StaffMembers.Add(staff);
        await db.SaveChangesAsync();

        var sut = new DashboardService(db, NullLogger<DashboardService>.Instance);
        var result = (await sut.GetStaffWorkloadAsync()).ToList();

        result.Single().StaffName.Should().Be("Jane Doe");
    }
}
