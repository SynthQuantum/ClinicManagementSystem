using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppUsersController : ControllerBase
{
    private readonly IAppUserService _service;
    private readonly ILogger<AppUsersController> _logger;

    public AppUsersController(IAppUserService service, ILogger<AppUsersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppUser>>> GetAll()
    {
        _logger.LogInformation("API: fetching app users");
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppUser>> GetById(Guid id)
    {
        var user = await _service.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<AppUser>> Create(AppUser user)
    {
        var created = await _service.CreateAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AppUser>> Update(Guid id, AppUser user)
    {
        if (id != user.Id) return BadRequest("Id mismatch.");
        var updated = await _service.UpdateAsync(user);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        return await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
