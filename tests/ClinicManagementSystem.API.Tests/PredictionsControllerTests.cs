using System.Security.Claims;
using ClinicManagementSystem.API.Controllers;
using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicManagementSystem.API.Tests;

/// <summary>
/// Unit tests for <see cref="PredictionsController"/>, covering direct prediction,
/// appointment-based prediction (found/not found/no-persist), and metrics retrieval.
/// </summary>
public class PredictionsControllerTests
{
    private static readonly FakeAuditLogService Audit = new();

    // -----------------------------------------------------------------------
    // POST /api/Predictions/no-show  (direct input)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PredictNoShow_ShouldReturnOk_WithPredictionOutput()
    {
        var expected = new NoShowPredictionOutput
        {
            WillNoShow = false,
            Probability = 0.2m,
            Score = 0.2m,
            RiskLevel = "Low",
            Recommendation = "Monitor"
        };
        var sut = CreateController(new FakePredictionService { DirectOutput = expected });

        var result = await sut.PredictNoShow(new NoShowPredictionInput
        {
            PatientAge = 35,
            DaysBetweenBookingAndAppointment = 5
        });

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // POST /api/Predictions/no-show/appointment/{id}  — success (no persist)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PredictNoShowForAppointment_ShouldReturnOk_WhenAppointmentFound()
    {
        var expected = new NoShowPredictionOutput
        {
            WillNoShow = true,
            Probability = 0.75m,
            RiskLevel = "High",
            Recommendation = "Call patient"
        };
        var sut = CreateController(new FakePredictionService { AppointmentOutput = expected });

        var result = await sut.PredictNoShowForAppointment(Guid.NewGuid(), persist: false);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // POST /api/Predictions/no-show/appointment/{id}  — not found (ArgumentException)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PredictNoShowForAppointment_ShouldReturnBadRequest_WhenAppointmentNotFound()
    {
        var sut = CreateController(new FakePredictionService
        {
            AppointmentException = new ArgumentException("Appointment was not found.")
        });

        var result = await sut.PredictNoShowForAppointment(Guid.NewGuid());

        result.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Appointment was not found.");
    }

    // -----------------------------------------------------------------------
    // GET /api/Predictions/no-show/metrics/latest  — found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLatestNoShowMetrics_ShouldReturnOk_WhenMetricsExist()
    {
        var metrics = new NoShowModelEvaluationResult
        {
            Accuracy = 0.88,
            Precision = 0.82,
            Recall = 0.79,
            F1Score = 0.80,
            Auc = 0.91,
            TrainRowCount = 960,
            TestRowCount = 240,
            ModelPath = "/ml-artifacts/model.zip",
            DatasetPath = "/ml-artifacts/dataset.csv",
            TrainingTimestampUtc = DateTime.UtcNow.AddHours(-1)
        };
        var sut = CreateController(new FakePredictionService { Metrics = metrics });

        var result = await sut.GetLatestNoShowMetrics();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(metrics);
    }

    // -----------------------------------------------------------------------
    // GET /api/Predictions/no-show/metrics/latest  — no metrics stored
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLatestNoShowMetrics_ShouldReturnNotFound_WhenNoMetricsExist()
    {
        var sut = CreateController(new FakePredictionService { Metrics = null });

        var result = await sut.GetLatestNoShowMetrics();

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static PredictionsController CreateController(IPredictionService service)
    {
        var controller = new PredictionsController(service, Audit, NullLogger<PredictionsController>.Instance);
        controller.ControllerContext = BuildControllerContext();
        return controller;
    }

    private static ControllerContext BuildControllerContext()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) },
            "TestAuth"));
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    // -----------------------------------------------------------------------
    // Fakes
    // -----------------------------------------------------------------------

    private sealed class FakePredictionService : IPredictionService
    {
        public NoShowPredictionOutput DirectOutput { get; set; } = new();
        public NoShowPredictionOutput AppointmentOutput { get; set; } = new();
        public Exception? AppointmentException { get; set; }
        public NoShowModelEvaluationResult? Metrics { get; set; }

        public Task<NoShowPredictionOutput> PredictNoShowAsync(NoShowPredictionInput input)
            => Task.FromResult(DirectOutput);

        public Task<NoShowPredictionOutput> PredictNoShowForAppointmentAsync(
            Guid appointmentId, bool persistResult = true)
        {
            if (AppointmentException is not null) throw AppointmentException;
            return Task.FromResult(AppointmentOutput);
        }

        public Task<NoShowModelEvaluationResult?> GetLatestNoShowModelMetricsAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(Metrics);

        public Task<NoShowDatasetGenerationResult> GenerateNoShowDatasetAsync(
            int rowCount = 1200, CancellationToken cancellationToken = default)
            => Task.FromResult(new NoShowDatasetGenerationResult());

        public Task<NoShowTrainingResult> TrainNoShowModelAsync(
            string? datasetPath = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new NoShowTrainingResult());

        public Task<bool> TryLoadNoShowModelAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public NoShowMlDataPoint MapInputToFeatureVector(NoShowPredictionInput input)
            => new();
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public Task<IEnumerable<AuditLog>> GetAllAsync() => Task.FromResult(Enumerable.Empty<AuditLog>());
        public Task<AuditLog?> GetByIdAsync(Guid id) => Task.FromResult<AuditLog?>(null);
        public Task<AuditLog> CreateAsync(AuditLog log) => Task.FromResult(log);
    }
}
