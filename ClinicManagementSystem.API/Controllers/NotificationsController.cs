using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService service, ILogger<NotificationsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<Notification>>> GetPending()
    {
        _logger.LogInformation("API: fetching pending notifications");
        return Ok(await _service.GetPendingAsync());
    }

    [HttpPost]
    public async Task<ActionResult<Notification>> Create(Notification notification)
    {
        var created = await _service.CreateAsync(notification);
        return Created(string.Empty, created);
    }

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult> Send(Guid id)
    {
        return await _service.SendAsync(id) ? NoContent() : NotFound();
    }
}
