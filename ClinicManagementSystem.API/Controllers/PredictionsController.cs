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
    private readonly ILogger<PredictionsController> _logger;

    public PredictionsController(IPredictionService predictionService, ILogger<PredictionsController> logger)
    {
        _predictionService = predictionService;
        _logger = logger;
    }

    [HttpPost("no-show")]
    public async Task<ActionResult<NoShowPredictionOutput>> PredictNoShow(NoShowPredictionInput input)
    {
        _logger.LogInformation("API: no-show prediction requested");
        return Ok(await _predictionService.PredictNoShowAsync(input));
    }

    [HttpPost("no-show/appointment/{appointmentId:guid}")]
    public async Task<ActionResult<NoShowPredictionOutput>> PredictNoShowForAppointment(Guid appointmentId, [FromQuery] bool persist = true)
    {
        _logger.LogInformation("API: no-show prediction requested for appointment {AppointmentId}", appointmentId);
        try
        {
            return Ok(await _predictionService.PredictNoShowForAppointmentAsync(appointmentId, persist));
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
        return Ok(result);
    }

    [HttpPost("no-show/train")]
    public async Task<ActionResult<NoShowTrainingResult>> TrainNoShowModel([FromQuery] string? datasetPath = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("API: no-show model training requested");
        var result = await _predictionService.TrainNoShowModelAsync(datasetPath, cancellationToken);
        return Ok(result);
    }
}
