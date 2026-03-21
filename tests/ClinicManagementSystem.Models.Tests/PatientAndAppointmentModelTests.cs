using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using FluentAssertions;

namespace ClinicManagementSystem.Models.Tests;

public class PatientAndAppointmentModelTests
{
    [Fact]
    public void Patient_ComputedProperties_ShouldReflectCurrentState()
    {
        var patient = new Patient
        {
            FirstName = "Amina",
            LastName = "Rahman",
            DateOfBirth = DateTime.UtcNow.Date.AddYears(-30),
            InsuranceProvider = "BlueShield"
        };

        patient.FullName.Should().Be("Amina Rahman");
        patient.HasInsurance.Should().BeTrue();
        patient.Age.Should().BeInRange(29, 30);
    }

    [Fact]
    public void Appointment_Defaults_ShouldBeInitialized()
    {
        var appointment = new Appointment();

        appointment.AppointmentType.Should().Be(AppointmentType.General);
        appointment.Status.Should().Be(AppointmentStatus.Scheduled);
        appointment.ReminderSent.Should().BeFalse();
        appointment.IsPredictedNoShow.Should().BeFalse();
        appointment.Notifications.Should().BeEmpty();
        appointment.PredictionResults.Should().BeEmpty();
        appointment.VisitRecords.Should().BeEmpty();
    }
}
