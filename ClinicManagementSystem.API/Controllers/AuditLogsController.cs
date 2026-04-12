using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

/// <summary>
/// Read-only access to the audit log. Write operations are performed internally
/// by the application; there is intentionally no public Create/Update/Delete endpoint
/// so that the audit trail cannot be fabricated or tampered with via the API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _service;

    public AuditLogsController(IAuditLogService service)
    {
        _service = service;
    }

    /// <summary>Returns the most recent audit entries (default 500).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetAll([FromQuery] int take = 500)
    {
        take = Math.Clamp(take, 1, 2000);
        return Ok(await _service.GetAllAsync(take));
    }

    /// <summary>Returns a single audit entry by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuditLog>> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Returns audit entries performed by a specific user.</summary>
    [HttpGet("by-user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetByUser(Guid userId, [FromQuery] int take = 200)
    {
        take = Math.Clamp(take, 1, 1000);
        return Ok(await _service.GetByUserAsync(userId, take));
    }

    /// <summary>Returns audit entries for a specific entity type, optionally filtered to a single record.</summary>
    [HttpGet("by-entity/{entityName}")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetByEntity(
        string entityName,
        [FromQuery] Guid? entityId = null,
        [FromQuery] int take = 200)
    {
        take = Math.Clamp(take, 1, 1000);
        return Ok(await _service.GetByEntityAsync(entityName, entityId, take));
    }

    /// <summary>Returns audit entries within a UTC date/time range.</summary>
    [HttpGet("by-date-range")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetByDateRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int maxResults = 500)
    {
        if (from >= to)
            return BadRequest("'from' must be earlier than 'to'.");
        maxResults = Math.Clamp(maxResults, 1, 2000);
        return Ok(await _service.GetByDateRangeAsync(from, to, maxResults));
    }

    /// <summary>Returns recent authentication failures, lockouts, and access-denied events.</summary>
    [HttpGet("security-events")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetSecurityEvents([FromQuery] int take = 100)
    {
        take = Math.Clamp(take, 1, 500);
        return Ok(await _service.GetSecurityEventsAsync(take));
    }
}

