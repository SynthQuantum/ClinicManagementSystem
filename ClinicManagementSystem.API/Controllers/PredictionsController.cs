using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
}
