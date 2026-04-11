using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicManagementSystem.Services.Tests;

public class PredictionServiceMlTests
{
    [Fact]
    public void MapInputToFeatureVector_ShouldBeDeterministic()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new PredictionService(NullLogger<PredictionService>.Instance, db);

        var input = new NoShowPredictionInput
        {
            PatientAge = 39,
            PreviousNoShowCount = 2,
            PreviousCompletedCount = 5,
            DaysBetweenBookingAndAppointment = 16,
            DayOfWeek = DayOfWeek.Friday,
            AppointmentType = AppointmentType.Consultation,
            HasInsurance = true,
            HasReminderSent = false
        };

        var first = sut.MapInputToFeatureVector(input);
        var second = sut.MapInputToFeatureVector(input);

        first.Should().BeEquivalentTo(second);
        first.DayOfWeek.Should().Be("Friday");
        first.AppointmentType.Should().Be("Consultation");
    }

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
        training.Evaluation.TrainRowCount.Should().BeGreaterThan(0);
        training.Evaluation.TestRowCount.Should().BeGreaterThan(0);
        training.Evaluation.ModelPath.Should().NotBeNullOrWhiteSpace();
        training.Evaluation.DatasetPath.Should().NotBeNullOrWhiteSpace();
        training.Evaluation.TrainingTimestampUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task TryLoadNoShowModelAsync_ShouldReturnTrueAfterTraining()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new PredictionService(NullLogger<PredictionService>.Instance, db);

        var dataset = await sut.GenerateNoShowDatasetAsync(700);
        await sut.TrainNoShowModelAsync(dataset.DatasetPath);

        var loaded = await sut.TryLoadNoShowModelAsync();
        loaded.Should().BeTrue();
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

    [Fact]
    public async Task PredictNoShowForAppointmentAsync_ShouldInferForKnownAppointment()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new PredictionService(NullLogger<PredictionService>.Instance, db);

        var now = DateTime.UtcNow;
        var patient = new Patient
        {
            FirstName = "Inference",
            LastName = "Patient",
            DateOfBirth = now.AddYears(-42),
            InsuranceProvider = "Contoso Health"
        };

        var staff = new StaffMember
        {
            FirstName = "Known",
            LastName = "Doctor",
            Email = "known.doctor@test.local",
            Role = UserRole.Doctor,
            Specialty = "General"
        };

        db.Patients.Add(patient);
        db.StaffMembers.Add(staff);
        await db.SaveChangesAsync();

        db.Appointments.AddRange(
            new Appointment
            {
                PatientId = patient.Id,
                StaffMemberId = staff.Id,
                AppointmentType = AppointmentType.Checkup,
                AppointmentDate = now.AddDays(-50),
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(9, 30, 0),
                Status = AppointmentStatus.Completed,
                ReminderSent = true,
                CreatedAt = now.AddDays(-65)
            },
            new Appointment
            {
                PatientId = patient.Id,
                StaffMemberId = staff.Id,
                AppointmentType = AppointmentType.Consultation,
                AppointmentDate = now.AddDays(-20),
                StartTime = new TimeSpan(11, 0, 0),
                EndTime = new TimeSpan(11, 30, 0),
                Status = AppointmentStatus.NoShow,
                ReminderSent = false,
                CreatedAt = now.AddDays(-34)
            });

        var target = new Appointment
        {
            PatientId = patient.Id,
            StaffMemberId = staff.Id,
            AppointmentType = AppointmentType.Consultation,
            AppointmentDate = now.AddDays(7),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Status = AppointmentStatus.Scheduled,
            ReminderSent = false,
            CreatedAt = now.AddDays(-2)
        };

        db.Appointments.Add(target);
        await db.SaveChangesAsync();

        var dataset = await sut.GenerateNoShowDatasetAsync(800);
        await sut.TrainNoShowModelAsync(dataset.DatasetPath);

        var output = await sut.PredictNoShowForAppointmentAsync(target.Id, persistResult: false);

        output.Probability.Should().BeInRange(0m, 1m);
        output.RiskLevel.Should().BeOneOf("Low", "Medium", "High");
        output.Recommendation.Should().NotBeNullOrWhiteSpace();
    }
}
