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
[Authorize(Roles = "Admin")]
public class StaffMembersController : ControllerBase
{
    private readonly IStaffService _service;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<StaffMembersController> _logger;

    public StaffMembersController(IStaffService service, IAuditLogService auditLogService, ILogger<StaffMembersController> logger)
    {
        _service = service;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StaffMember>>> GetAll()
    {
        _logger.LogInformation("API: fetching staff members");
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StaffMember>> GetById(Guid id)
    {
        var staff = await _service.GetByIdAsync(id);
        return staff is null ? NotFound() : Ok(staff);
    }

    [HttpPost]
    public async Task<ActionResult<StaffMember>> Create(StaffUpsertRequest request)
    {
        var staff = new StaffMember
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = request.Role,
            Specialty = request.Specialty,
            IsAvailable = request.IsAvailable
        };

        var created = await _service.CreateAsync(staff);
        await WriteAuditAsync("StaffMember", "Created", created.Id, "Staff member created");
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StaffMember>> Update(Guid id, StaffUpsertRequest request)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();

        existing.FirstName = request.FirstName;
        existing.LastName = request.LastName;
        existing.Email = request.Email;
        existing.PhoneNumber = request.PhoneNumber;
        existing.Role = request.Role;
        existing.Specialty = request.Specialty;
        existing.IsAvailable = request.IsAvailable;

        var updated = await _service.UpdateAsync(existing);
        await WriteAuditAsync("StaffMember", "Updated", updated.Id, "Staff member updated");
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

        await WriteAuditAsync("StaffMember", "Deleted", id, "Staff member soft-deleted");
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
