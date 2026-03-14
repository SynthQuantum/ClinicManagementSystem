using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

/// <summary>
/// Rule-based no-show prediction. Replace with an ML.NET model for production.
/// </summary>
public class PredictionService : IPredictionService
{
    private readonly ILogger<PredictionService> _logger;

    public PredictionService(ILogger<PredictionService> logger)
    {
        _logger = logger;
    }

    public Task<NoShowPredictionOutput> PredictNoShowAsync(NoShowPredictionInput input)
    {
        _logger.LogInformation("Running no-show prediction for appointment type {Type}", input.AppointmentType);

        // Rule-based scoring (replace with ML model in production)
        double score = 0;

        if (input.PreviousNoShowCount > 0)
            score += 0.3 * input.PreviousNoShowCount;

        if (input.DaysBetweenBookingAndAppointment > 14)
            score += 0.2;

        if (!input.HasReminderSent)
            score += 0.1;

        if (!input.HasInsurance)
            score += 0.1;

        if (input.DayOfWeek == DayOfWeek.Monday || input.DayOfWeek == DayOfWeek.Friday)
            score += 0.05;

        score = Math.Clamp(score, 0, 1);

        var output = new NoShowPredictionOutput
        {
            WillNoShow = score >= 0.5,
            Probability = (decimal)Math.Round(score, 4),
            RiskLevel = score < 0.3 ? "Low" : score < 0.6 ? "Medium" : "High"
        };

        _logger.LogInformation("No-show prediction result: WillNoShow={WillNoShow}, Risk={Risk}", output.WillNoShow, output.RiskLevel);
        return Task.FromResult(output);
    }
}
