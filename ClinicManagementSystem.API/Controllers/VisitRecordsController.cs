using System.Security.Claims;
using ClinicManagementSystem.API.Extensions;
using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

/// <summary>
/// Manages visit records, which contain highly sensitive PHI (diagnosis, treatment,
/// prescription). Every mutation and individual-record read is written to the audit log.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Doctor,Receptionist")]
public class VisitRecordsController : ControllerBase
{
    private readonly IVisitRecordService _service;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<VisitRecordsController> _logger;

    public VisitRecordsController(
        IVisitRecordService service,
        IAuditLogService auditLogService,
        ILogger<VisitRecordsController> logger)
    {
        _service = service;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<ActionResult<IEnumerable<VisitRecord>>> GetByPatient(Guid patientId)
    {
        _logger.LogInformation("API: fetching visit records for patient {PatientId}", patientId);
        var records = await _service.GetByPatientAsync(patientId);
        await WriteAuditAsync("VisitRecord", "ListedByPatient", patientId, $"Visit records listed for patient {patientId}", "Success");
        return Ok(records);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VisitRecord>> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null)
        {
            await WriteAuditAsync("VisitRecord", "ReadNotFound", id, $"Visit record {id} not found", "Failure");
            return NotFound();
        }

        // Individual PHI record read — must be audited for HIPAA §164.312(b)
        await WriteAuditAsync("VisitRecord", "Read", id, $"Visit record {id} accessed", "Success");
        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<ActionResult<VisitRecord>> Create(VisitRecordUpsertRequest request)
    {
        var record = new VisitRecord
        {
            PatientId = request.PatientId,
            AppointmentId = request.AppointmentId,
            StaffMemberId = request.StaffMemberId,
            VisitDate = request.VisitDate,
            Diagnosis = request.Diagnosis,
            Treatment = request.Treatment,
            Prescription = request.Prescription,
            Notes = request.Notes
        };

        var created = await _service.CreateAsync(record);
        await WriteAuditAsync("VisitRecord", "Created", created.Id, $"Visit record created for patient {created.PatientId}", "Success");
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<ActionResult<VisitRecord>> Update(Guid id, VisitRecordUpsertRequest request)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null)
        {
            await WriteAuditAsync("VisitRecord", "UpdateNotFound", id, $"Update failed — visit record {id} not found", "Failure");
            return NotFound();
        }

        existing.PatientId = request.PatientId;
        existing.AppointmentId = request.AppointmentId;
        existing.StaffMemberId = request.StaffMemberId;
        existing.VisitDate = request.VisitDate;
        existing.Diagnosis = request.Diagnosis;
        existing.Treatment = request.Treatment;
        existing.Prescription = request.Prescription;
        existing.Notes = request.Notes;

        var updated = await _service.UpdateAsync(existing);
        await WriteAuditAsync("VisitRecord", "Updated", updated.Id, $"Visit record updated for patient {updated.PatientId}", "Success");
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
        {
            await WriteAuditAsync("VisitRecord", "DeleteNotFound", id, $"Delete failed — visit record {id} not found", "Failure");
            return NotFound();
        }

        await WriteAuditAsync("VisitRecord", "Deleted", id, $"Visit record {id} soft-deleted", "Success");
        return NoContent();
    }

    private async Task WriteAuditAsync(string entityName, string actionType, Guid? entityId, string description, string outcome)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        _ = Guid.TryParse(userIdValue, out var userId);

        await _auditLogService.CreateAsync(new AuditLog
        {
            EntityName = entityName,
            ActionType = actionType,
            EntityId = entityId,
            PerformedByUserId = userId == Guid.Empty ? null : userId,
            UserRole = User.FindFirstValue(ClaimTypes.Role),
            IpAddress = HttpContext.GetClientIpAddress(),
            HttpMethod = HttpContext.Request.Method,
            RequestPath = HttpContext.Request.Path.Value,
            Outcome = outcome,
            Description = description
        });
    }
}

