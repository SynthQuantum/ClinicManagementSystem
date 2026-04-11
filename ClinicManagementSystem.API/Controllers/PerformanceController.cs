using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Doctor")]
public class PerformanceController : ControllerBase
{
    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly IHostEnvironment _environment;

    public PerformanceController(IPerformanceMonitoringService performanceMonitoringService, IHostEnvironment environment)
    {
        _performanceMonitoringService = performanceMonitoringService;
        _environment = environment;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<PerformanceSummary>> GetSummary(CancellationToken cancellationToken = default)
    {
        return Ok(await _performanceMonitoringService.GetSummaryAsync(cancellationToken));
    }

    [HttpPost("reset")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Reset(CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment() && !_environment.IsEnvironment("Testing"))
        {
            return Forbid();
        }

        await _performanceMonitoringService.ResetSamplesAsync(cancellationToken);
        return NoContent();
    }
}
