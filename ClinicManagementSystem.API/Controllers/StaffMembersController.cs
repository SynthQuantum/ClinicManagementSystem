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
    private readonly ILogger<StaffMembersController> _logger;

    public StaffMembersController(IStaffService service, ILogger<StaffMembersController> logger)
    {
        _service = service;
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
    public async Task<ActionResult<StaffMember>> Create(StaffMember staff)
    {
        var created = await _service.CreateAsync(staff);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StaffMember>> Update(Guid id, StaffMember staff)
    {
        if (id != staff.Id) return BadRequest("Id mismatch.");
        var updated = await _service.UpdateAsync(staff);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        return await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
