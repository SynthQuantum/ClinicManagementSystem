using System.Security.Claims;
using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Doctor,Receptionist")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _service;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(IAppointmentService service, IAuditLogService auditLogService, ILogger<AppointmentsController> logger)
    {
        _service = service;
        _auditLogService = auditLogService;
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
    public async Task<ActionResult<Appointment>> Create(AppointmentUpsertRequest request)
    {
        try
        {
            var appointment = new Appointment
            {
                PatientId = request.PatientId,
                StaffMemberId = request.StaffMemberId,
                AppointmentType = request.AppointmentType,
                AppointmentDate = request.AppointmentDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Status = request.Status,
                Reason = request.Reason,
                Notes = request.Notes,
                ReminderSent = request.ReminderSent
            };

            var created = await _service.CreateAsync(appointment);
            await WriteAuditAsync("Appointment", "Created", created.Id, $"Appointment created with status {created.Status}");
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
    public async Task<ActionResult<Appointment>> Update(Guid id, AppointmentUpsertRequest request)
    {
        try
        {
            var existing = await _service.GetByIdAsync(id);
            if (existing is null)
            {
                return NotFound();
            }

            existing.PatientId = request.PatientId;
            existing.StaffMemberId = request.StaffMemberId;
            existing.AppointmentType = request.AppointmentType;
            existing.AppointmentDate = request.AppointmentDate;
            existing.StartTime = request.StartTime;
            existing.EndTime = request.EndTime;
            existing.Status = request.Status;
            existing.Reason = request.Reason;
            existing.Notes = request.Notes;
            existing.ReminderSent = request.ReminderSent;

            var updated = await _service.UpdateAsync(existing);
            await WriteAuditAsync("Appointment", "Updated", updated.Id, $"Appointment updated with status {updated.Status}");
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
        var updated = await _service.UpdateStatusAsync(id, status);
        if (!updated)
        {
            return NotFound();
        }

        var actionType = status switch
        {
            AppointmentStatus.Cancelled => "Cancelled",
            AppointmentStatus.NoShow => "NoShowMarked",
            _ => "StatusUpdated"
        };

        await WriteAuditAsync("Appointment", actionType, id, $"Appointment status changed to {status}");
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        await WriteAuditAsync("Appointment", "Deleted", id, "Appointment soft-deleted");
        return NoContent();
    }

    private async Task WriteAuditAsync(string entityName, string actionType, Guid? entityId, string description)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        _ = Guid.TryParse(userIdValue, out var userId);

        await _auditLogService.CreateAsync(new AuditLog
        {
            EntityName = entityName,
            ActionType = actionType,
            EntityId = entityId,
            PerformedByUserId = userId == Guid.Empty ? null : userId,
            Description = description
        });
    }
}
