using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummary>> GetSummary()
        => Ok(await _dashboardService.GetSummaryAsync());

    [HttpGet("trend")]
    public async Task<ActionResult<IEnumerable<AppointmentTrendPoint>>> GetTrend([FromQuery] int days = 30)
        => Ok(await _dashboardService.GetAppointmentTrendAsync(days));

    [HttpGet("staff-workload")]
    public async Task<ActionResult<IEnumerable<StaffWorkloadSummary>>> GetStaffWorkload()
        => Ok(await _dashboardService.GetStaffWorkloadAsync());
}
