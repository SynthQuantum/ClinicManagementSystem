using ClinicManagementSystem.API.Controllers;
using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Tests;

/// <summary>
/// Unit tests for <see cref="DashboardController"/> covering all three endpoints:
/// summary, trend, and staff workload.
/// The GetSummary baseline test lives in <see cref="ControllersUnitTests"/>.
/// </summary>
public class DashboardControllerTests
{
    // -----------------------------------------------------------------------
    // GET /api/Dashboard/summary  — data present
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetSummary_ShouldReturnOk_WhenDataIsPresent()
    {
        var summary = new DashboardSummary
        {
            TotalPatients = 42,
            TotalAppointments = 110,
            TodayAppointments = 8,
            CompletedAppointments = 95,
            CancelledAppointments = 10,
            NoShowAppointments = 5
        };
        var sut = new DashboardController(new FakeDashboardService { Summary = summary });

        var result = await sut.GetSummary();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = ok.Value.Should().BeOfType<DashboardSummary>().Subject;
        returned.TotalPatients.Should().Be(42);
        returned.NoShowRate.Should().BeApproximately(4.55m, 0.01m);
    }

    // -----------------------------------------------------------------------
    // GET /api/Dashboard/trend
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTrend_ShouldReturnOkWithTrendPoints()
    {
        var trend = Enumerable.Range(0, 7).Select(i => new AppointmentTrendPoint
        {
            Date = DateTime.UtcNow.Date.AddDays(-i),
            Count = i * 2
        }).ToArray();
        var sut = new DashboardController(new FakeDashboardService { Trend = trend });

        var result = await sut.GetTrend(7);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        (ok.Value as IEnumerable<AppointmentTrendPoint>).Should().HaveCount(7);
    }

    [Fact]
    public async Task GetTrend_ShouldPassDaysParameterToService()
    {
        var fake = new FakeDashboardService();
        var sut = new DashboardController(fake);

        await sut.GetTrend(14);

        fake.LastTrendDays.Should().Be(14);
    }

    [Fact]
    public async Task GetTrend_ShouldDefault30Days_WhenParameterOmitted()
    {
        var fake = new FakeDashboardService();
        var sut = new DashboardController(fake);

        await sut.GetTrend(); // uses default value (30)

        fake.LastTrendDays.Should().Be(30);
    }

    // -----------------------------------------------------------------------
    // GET /api/Dashboard/staff-workload
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetStaffWorkload_ShouldReturnOkWithWorkloadSummaries()
    {
        var workload = new[]
        {
            new StaffWorkloadSummary { StaffName = "Dr. A", TotalAppointments = 10 },
            new StaffWorkloadSummary { StaffName = "Dr. B", TotalAppointments = 7 }
        };
        var sut = new DashboardController(new FakeDashboardService { Workload = workload });

        var result = await sut.GetStaffWorkload();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = (ok.Value as IEnumerable<StaffWorkloadSummary>)!.ToList();
        list.Should().HaveCount(2);
        list[0].StaffName.Should().Be("Dr. A");
    }

    [Fact]
    public async Task GetStaffWorkload_ShouldReturnEmptyList_WhenNoStaffExist()
    {
        var sut = new DashboardController(
            new FakeDashboardService { Workload = Array.Empty<StaffWorkloadSummary>() });

        var result = await sut.GetStaffWorkload();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        (ok.Value as IEnumerable<StaffWorkloadSummary>).Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // Fakes
    // -----------------------------------------------------------------------

    private sealed class FakeDashboardService : IDashboardService
    {
        public DashboardSummary Summary { get; set; } = new();
        public IEnumerable<AppointmentTrendPoint> Trend { get; set; } = Array.Empty<AppointmentTrendPoint>();
        public IEnumerable<StaffWorkloadSummary> Workload { get; set; } = Array.Empty<StaffWorkloadSummary>();
        public int LastTrendDays { get; private set; }

        public Task<DashboardSummary> GetSummaryAsync() => Task.FromResult(Summary);

        public Task<IEnumerable<AppointmentTrendPoint>> GetAppointmentTrendAsync(int days = 30)
        {
            LastTrendDays = days;
            return Task.FromResult(Trend);
        }

        public Task<IEnumerable<StaffWorkloadSummary>> GetStaffWorkloadAsync() =>
            Task.FromResult(Workload);
    }
}
