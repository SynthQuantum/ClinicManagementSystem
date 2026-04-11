using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Implementations;
using ClinicManagementSystem.Services.Tests.Builders;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicManagementSystem.Services.Tests;

/// <summary>
/// Tests focused on <see cref="PredictionService"/> fallback (rule-based) behavior
/// when no trained ML model is present, and on the appointment-lookup path.
/// </summary>
public class PredictionFallbackTests
{
    // -----------------------------------------------------------------------
    // Fallback output shape
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PredictNoShowAsync_ShouldReturnValidOutput_WhenNoModelExists()
    {
        // A fresh PredictionService with a clean in-memory DB has no trained model,
        // so the rule-based fallback is triggered.
        using var db = TestDbContextFactory.Create();
        var sut = new PredictionService(NullLogger<PredictionService>.Instance, db);

        var input = new NoShowPredictionInput
        {
            PatientAge = 30,
            PreviousNoShowCount = 0,
            PreviousCompletedCount = 5,
            DaysBetweenBookingAndAppointment = 7,
            DayOfWeek = DayOfWeek.Monday,
            AppointmentType = AppointmentType.General,
            HasInsurance = true,
            HasReminderSent = true
        };

        var output = await sut.PredictNoShowAsync(input);

        output.RiskLevel.Should().BeOneOf("Low", "Medium", "High");
        output.Probability.Should().BeInRange(0m, 1m);
        output.Recommendation.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PredictNoShowAsync_FallbackShouldReturnHighRisk_ForHighRiskProfile()
    {
        // Many previous no-shows + far out booking date is a high-risk profile
        using var db = TestDbContextFactory.Create();
        var sut = new PredictionService(NullLogger<PredictionService>.Instance, db);

        var highRiskInput = new NoShowPredictionInput
        {
            PatientAge = 25,
            PreviousNoShowCount = 5,        // high history of no-shows
            PreviousCompletedCount = 0,
            DaysBetweenBookingAndAppointment = 30,  // booked far in advance
            DayOfWeek = DayOfWeek.Monday,
            AppointmentType = AppointmentType.General,
            HasInsurance = false,
            HasReminderSent = false
        };

        var output = await sut.PredictNoShowAsync(highRiskInput);

        // The rule-based formula should produce a higher risk than Low for this profile
        output.RiskLevel.Should().BeOneOf("Medium", "High");
        output.Probability.Should().BeGreaterThan(0.3m);
    }

    [Fact]
    public async Task PredictNoShowAsync_FallbackShouldReturnLowRisk_ForLowRiskProfile()
    {
        // Excellent history, confirmed, reminder sent → low risk
        using var db = TestDbContextFactory.Create();
        var sut = new PredictionService(NullLogger<PredictionService>.Instance, db);

        var lowRiskInput = new NoShowPredictionInput
        {
            PatientAge = 50,
            PreviousNoShowCount = 0,
            PreviousCompletedCount = 20,
            DaysBetweenBookingAndAppointment = 1,
            DayOfWeek = DayOfWeek.Wednesday,
            AppointmentType = AppointmentType.Checkup,
            HasInsurance = true,
            HasReminderSent = true
        };

        var output = await sut.PredictNoShowAsync(lowRiskInput);

        output.Probability.Should().BeLessThan(0.6m);
        output.RiskLevel.Should().NotBeNullOrWhiteSpace();
    }

    // -----------------------------------------------------------------------
    // Appointment-based prediction: appointment not found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PredictNoShowForAppointmentAsync_ShouldThrow_WhenAppointmentDoesNotExist()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new PredictionService(NullLogger<PredictionService>.Instance, db);

        var act = async () =>
            await sut.PredictNoShowForAppointmentAsync(Guid.NewGuid(), persistResult: false);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    // -----------------------------------------------------------------------
    // Appointment-based prediction: persists result when persist=true
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PredictNoShowForAppointmentAsync_ShouldPersistPredictionResult_WhenPersistIsTrue()
    {
        using var db = TestDbContextFactory.Create();

        var patient = PatientBuilder.Default()
            .WithDateOfBirth(new DateTime(1990, 1, 1))
            .Build();
        var staff = StaffMemberBuilder.Default().Build();
        db.Patients.Add(patient);
        db.StaffMembers.Add(staff);

        var appointment = AppointmentBuilder
            .For(patient.Id, staff.Id)
            .OnDate(DateTime.UtcNow.Date.AddDays(2))
            .WithSlot(new(9, 0, 0), new(9, 30, 0))
            .Build();
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var sut = new PredictionService(NullLogger<PredictionService>.Instance, db);
        var output = await sut.PredictNoShowForAppointmentAsync(appointment.Id, persistResult: true);

        output.Should().NotBeNull();
        output.RiskLevel.Should().BeOneOf("Low", "Medium", "High");

        // Prediction result should have been persisted
        var stored = db.PredictionResults.SingleOrDefault(r => r.AppointmentId == appointment.Id);
        stored.Should().NotBeNull();
        stored!.PredictionType.Should().Be("NoShow");
    }

    // -----------------------------------------------------------------------
    // Appointment-based prediction: skip persistence when persist=false
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PredictNoShowForAppointmentAsync_ShouldNotPersist_WhenPersistIsFalse()
    {
        using var db = TestDbContextFactory.Create();

        var patient = PatientBuilder.Default().Build();
        var staff = StaffMemberBuilder.Default().Build();
        db.Patients.Add(patient);
        db.StaffMembers.Add(staff);

        var appointment = AppointmentBuilder
            .For(patient.Id, staff.Id)
            .OnDate(DateTime.UtcNow.Date.AddDays(3))
            .WithSlot(new(9, 0, 0), new(9, 30, 0))
            .Build();
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var sut = new PredictionService(NullLogger<PredictionService>.Instance, db);
        await sut.PredictNoShowForAppointmentAsync(appointment.Id, persistResult: false);

        db.PredictionResults.Should().BeEmpty();
    }
}
