using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Services.Tests.Builders;

/// <summary>Fluent builder for <see cref="Appointment"/> test fixtures.</summary>
public sealed class AppointmentBuilder
{
    private Guid _patientId;
    private Guid _staffMemberId;
    private DateTime _date = DateTime.UtcNow.Date.AddDays(1);
    private TimeSpan _start = new(9, 0, 0);
    private TimeSpan _end = new(9, 30, 0);
    private AppointmentStatus _status = AppointmentStatus.Scheduled;
    private AppointmentType _type = AppointmentType.General;
    private string? _reason;
    private string? _notes;

    public static AppointmentBuilder For(Guid patientId, Guid staffMemberId) =>
        new() { _patientId = patientId, _staffMemberId = staffMemberId };

    public AppointmentBuilder OnDate(DateTime date)
    {
        _date = date;
        return this;
    }

    public AppointmentBuilder WithSlot(TimeSpan start, TimeSpan end)
    {
        _start = start;
        _end = end;
        return this;
    }

    public AppointmentBuilder WithStatus(AppointmentStatus status)
    {
        _status = status;
        return this;
    }

    public AppointmentBuilder WithType(AppointmentType type)
    {
        _type = type;
        return this;
    }

    public AppointmentBuilder WithReason(string reason)
    {
        _reason = reason;
        return this;
    }

    public AppointmentBuilder WithNotes(string notes)
    {
        _notes = notes;
        return this;
    }

    public Appointment Build() => new()
    {
        PatientId = _patientId,
        StaffMemberId = _staffMemberId,
        AppointmentDate = _date,
        StartTime = _start,
        EndTime = _end,
        Status = _status,
        AppointmentType = _type,
        Reason = _reason,
        Notes = _notes
    };
}
