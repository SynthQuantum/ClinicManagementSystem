using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitRecordsController : ControllerBase
{
    private readonly IVisitRecordService _service;
    private readonly ILogger<VisitRecordsController> _logger;

    public VisitRecordsController(IVisitRecordService service, ILogger<VisitRecordsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<ActionResult<IEnumerable<VisitRecord>>> GetByPatient(Guid patientId)
    {
        _logger.LogInformation("API: fetching visit records for patient {PatientId}", patientId);
        return Ok(await _service.GetByPatientAsync(patientId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VisitRecord>> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<VisitRecord>> Create(VisitRecord record)
    {
        var created = await _service.CreateAsync(record);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VisitRecord>> Update(Guid id, VisitRecord record)
    {
        if (id != record.Id) return BadRequest("Id mismatch.");
        var updated = await _service.UpdateAsync(record);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        return await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
