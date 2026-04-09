using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Doctor,Receptionist")]
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
    public async Task<ActionResult<IEnumerable<Notification>>> GetPending([FromQuery] int take = 200, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("API: fetching pending notifications. Take={Take}", take);
        return Ok(await _service.GetPendingAsync(take, cancellationToken));
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<Notification>>> GetHistory([FromQuery] NotificationStatus? status = null, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("API: fetching notification history. Status={Status}, Take={Take}", status, take);
        return Ok(await _service.GetHistoryAsync(status, take, cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<NotificationDashboardSummary>> GetSummary([FromQuery] int recentTake = 25, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("API: fetching notification summary");
        return Ok(await _service.GetSummaryAsync(recentTake, cancellationToken));
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

    [HttpPost("process-reminders")]
    public async Task<ActionResult<NotificationProcessingResult>> ProcessReminders(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("API: manual reminder processing triggered");
        var result = await _service.ProcessRemindersAsync(cancellationToken);
        return Ok(result);
    }
}
