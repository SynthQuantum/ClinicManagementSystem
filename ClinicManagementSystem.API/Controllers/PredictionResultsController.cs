using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Doctor")]
public class PredictionResultsController : ControllerBase
{
    private readonly IPredictionResultService _service;

    public PredictionResultsController(IPredictionResultService service)
    {
        _service = service;
    }

    [HttpGet("appointment/{appointmentId:guid}")]
    public async Task<ActionResult<IEnumerable<PredictionResult>>> GetByAppointment(Guid appointmentId)
        => Ok(await _service.GetByAppointmentAsync(appointmentId));

    /// <summary>Internal creation endpoint. Restricted to Admin to prevent result fabrication.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PredictionResult>> Create(PredictionResult result)
    {
        var created = await _service.CreateAsync(result);
        return Created(string.Empty, created);
    }
}

