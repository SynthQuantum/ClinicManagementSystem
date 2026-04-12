using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ClinicSettingsController : ControllerBase
{
    private readonly IClinicSettingsService _service;

    public ClinicSettingsController(IClinicSettingsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ClinicSettings>> GetCurrent()
    {
        var settings = await _service.GetCurrentAsync();
        return settings is null ? NotFound() : Ok(settings);
    }

    [HttpPut]
    public async Task<ActionResult<ClinicSettings>> Upsert(ClinicSettings settings)
    {
        var updated = await _service.UpsertAsync(settings);
        return Ok(updated);
    }
}

