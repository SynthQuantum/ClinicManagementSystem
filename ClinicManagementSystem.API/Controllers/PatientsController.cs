using System.Security.Claims;
using ClinicManagementSystem.API.Extensions;
using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Doctor,Receptionist")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _service;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(IPatientService service, IAuditLogService auditLogService, ILogger<PatientsController> logger)
    {
        _service = service;
        _auditLogService = auditLogService;
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
    public async Task<ActionResult<Patient>> Create(PatientUpsertRequest request)
    {
        var patient = new Patient
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Address = request.Address,
            BloodType = request.BloodType,
            InsuranceProvider = request.InsuranceProvider,
            InsurancePolicyNumber = request.InsurancePolicyNumber,
            InsuranceExpiryDate = request.InsuranceExpiryDate,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            EmergencyContactRelationship = request.EmergencyContactRelationship,
            Notes = request.Notes
        };

        var created = await _service.CreateAsync(patient);
        await WriteAuditAsync("Patient", "Created", created.Id, "Patient record created");
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Patient>> Update(Guid id, PatientUpsertRequest request)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();

        existing.FirstName = request.FirstName;
        existing.LastName = request.LastName;
        existing.DateOfBirth = request.DateOfBirth;
        existing.Gender = request.Gender;
        existing.PhoneNumber = request.PhoneNumber;
        existing.Email = request.Email;
        existing.Address = request.Address;
        existing.BloodType = request.BloodType;
        existing.InsuranceProvider = request.InsuranceProvider;
        existing.InsurancePolicyNumber = request.InsurancePolicyNumber;
        existing.InsuranceExpiryDate = request.InsuranceExpiryDate;
        existing.EmergencyContactName = request.EmergencyContactName;
        existing.EmergencyContactPhone = request.EmergencyContactPhone;
        existing.EmergencyContactRelationship = request.EmergencyContactRelationship;
        existing.Notes = request.Notes;

        var updated = await _service.UpdateAsync(existing);
        await WriteAuditAsync("Patient", "Updated", updated.Id, "Patient record updated");
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        await WriteAuditAsync("Patient", "Deleted", id, "Patient record soft-deleted");
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
            UserRole = User.FindFirstValue(ClaimTypes.Role),
            IpAddress = HttpContext.GetClientIpAddress(),
            HttpMethod = HttpContext.Request.Method,
            RequestPath = HttpContext.Request.Path.Value,
            Outcome = "Success",
            Description = description
        });
    }
}
