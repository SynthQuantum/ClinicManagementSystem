using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _service;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(IAppointmentService service, ILogger<AppointmentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetAll(
        [FromQuery] DateTime? date,
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? staffMemberId)
    {
        _logger.LogInformation(
            "API: fetching appointments. Date filter: {Date}, Patient: {PatientId}, Staff: {StaffId}",
            date,
            patientId,
            staffMemberId);

        if (patientId.HasValue)
        {
            return Ok(await _service.GetByPatientAsync(patientId.Value));
        }

        if (staffMemberId.HasValue)
        {
            return Ok(await _service.GetByStaffAsync(staffMemberId.Value));
        }

        return Ok(date.HasValue ? await _service.GetByDateAsync(date.Value) : await _service.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Appointment>> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Appointment>> Create(Appointment appointment)
    {
        try
        {
            var created = await _service.CreateAsync(appointment);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Appointment>> Update(Guid id, Appointment appointment)
    {
        if (id != appointment.Id) return BadRequest("Id mismatch.");
        try
        {
            var updated = await _service.UpdateAsync(appointment);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid id, [FromQuery] AppointmentStatus status)
    {
        return await _service.UpdateStatusAsync(id, status) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        return await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
