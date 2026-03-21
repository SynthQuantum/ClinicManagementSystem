using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicManagementSystem.Services.Tests;

public class PredictionServiceMlTests
{
    [Fact]
    public async Task GenerateNoShowDatasetAsync_ShouldCreateCsvWithExpectedHeader()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new PredictionService(NullLogger<PredictionService>.Instance, db);

        var result = await sut.GenerateNoShowDatasetAsync(600);

        result.GeneratedRows.Should().Be(600);
        File.Exists(result.DatasetPath).Should().BeTrue();

        var firstLine = (await File.ReadAllLinesAsync(result.DatasetPath)).First();
        firstLine.Should().Contain("PatientAge");
        firstLine.Should().Contain("PreviousNoShows");
        firstLine.Should().Contain("Label");
    }

    [Fact]
    public async Task TrainNoShowModelAsync_ShouldReturnMetricsAndSaveModel()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new PredictionService(NullLogger<PredictionService>.Instance, db);

        var dataset = await sut.GenerateNoShowDatasetAsync(700);
        var training = await sut.TrainNoShowModelAsync(dataset.DatasetPath);

        File.Exists(training.ModelPath).Should().BeTrue();
        training.Metrics.Accuracy.Should().BeInRange(0, 1);
        training.Metrics.Precision.Should().BeInRange(0, 1);
        training.Metrics.Recall.Should().BeInRange(0, 1);
        training.Metrics.F1Score.Should().BeInRange(0, 1);
        training.Metrics.Auc.Should().BeInRange(0, 1);
    }

    [Fact]
    public async Task PredictNoShowAsync_ShouldReturnValidOutputShape()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new PredictionService(NullLogger<PredictionService>.Instance, db);

        var input = new NoShowPredictionInput
        {
            PatientAge = 45,
            PreviousNoShowCount = 1,
            PreviousCompletedCount = 3,
            DaysBetweenBookingAndAppointment = 12,
            DayOfWeek = DayOfWeek.Monday,
            AppointmentType = AppointmentType.Checkup,
            HasInsurance = true,
            HasReminderSent = false
        };

        var output = await sut.PredictNoShowAsync(input);

        output.RiskLevel.Should().BeOneOf("Low", "Medium", "High");
        output.Probability.Should().BeInRange(0m, 1m);
        output.Recommendation.Should().NotBeNullOrWhiteSpace();
    }
}
