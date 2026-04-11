using System.Security.Claims;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Doctor,Receptionist")]
public class PredictionsController : ControllerBase
{
    private readonly IPredictionService _predictionService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PredictionsController> _logger;

    public PredictionsController(IPredictionService predictionService, IAuditLogService auditLogService, ILogger<PredictionsController> logger)
    {
        _predictionService = predictionService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpPost("no-show")]
    public async Task<ActionResult<NoShowPredictionOutput>> PredictNoShow([FromBody] NoShowPredictionInput input)
    {
        _logger.LogInformation("API: no-show prediction requested");
        var result = await _predictionService.PredictNoShowAsync(input);
        await WriteAuditAsync("PredictionResult", "PredictionRequested", null, "No-show prediction requested from direct input");
        return Ok(result);
    }

    [HttpPost("no-show/appointment/{appointmentId:guid}")]
    public async Task<ActionResult<NoShowPredictionOutput>> PredictNoShowForAppointment(Guid appointmentId, [FromQuery] bool persist = true)
    {
        _logger.LogInformation("API: no-show prediction requested for appointment {AppointmentId}", appointmentId);
        try
        {
            var result = await _predictionService.PredictNoShowForAppointmentAsync(appointmentId, persist);
            var action = persist ? "PredictionPersisted" : "PredictionRequested";
            var description = persist
                ? "No-show prediction generated and persisted for appointment"
                : "No-show prediction generated for appointment without persistence";

            await WriteAuditAsync("PredictionResult", action, appointmentId, description);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("no-show/dataset")]
    public async Task<ActionResult<NoShowDatasetGenerationResult>> GenerateNoShowDataset([FromQuery] int rows = 1200, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("API: no-show synthetic dataset generation requested. Rows={Rows}", rows);
        var result = await _predictionService.GenerateNoShowDatasetAsync(rows, cancellationToken);
        await WriteAuditAsync("PredictionResult", "DatasetGenerated", null, $"No-show dataset generated with {result.GeneratedRows} rows");
        return Ok(result);
    }

    [HttpPost("no-show/train")]
    public async Task<ActionResult<NoShowTrainingResult>> TrainNoShowModel([FromQuery] string? datasetPath = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("API: no-show model training requested");
        var result = await _predictionService.TrainNoShowModelAsync(datasetPath, cancellationToken);
        await WriteAuditAsync("PredictionResult", "ModelTrained", null, "No-show model training executed");
        return Ok(result);
    }

    [HttpGet("no-show/metrics/latest")]
    public async Task<ActionResult<NoShowModelEvaluationResult>> GetLatestNoShowMetrics(CancellationToken cancellationToken = default)
    {
        var result = await _predictionService.GetLatestNoShowModelMetricsAsync(cancellationToken);
        if (result is null)
        {
            return NotFound("No stored no-show model metrics were found.");
        }

        return Ok(result);
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
