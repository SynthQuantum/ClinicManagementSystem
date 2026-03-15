using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _service;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(IPatientService service, ILogger<PatientsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Patient>>> GetAll([FromQuery] string? q)
    {
        _logger.LogInformation("API: fetching patients with query {Query}", q);
        return Ok(string.IsNullOrWhiteSpace(q) ? await _service.GetAllAsync() : await _service.SearchAsync(q));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Patient>> GetById(Guid id)
    {
        var patient = await _service.GetByIdAsync(id);
        return patient is null ? NotFound() : Ok(patient);
    }

    [HttpPost]
    public async Task<ActionResult<Patient>> Create(Patient patient)
    {
        var created = await _service.CreateAsync(patient);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Patient>> Update(Guid id, Patient patient)
    {
        if (id != patient.Id) return BadRequest("Id mismatch.");
        var updated = await _service.UpdateAsync(patient);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        return await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
