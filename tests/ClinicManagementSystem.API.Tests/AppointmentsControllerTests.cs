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
/// Unit tests for <see cref="AppointmentsController"/> covering query filters,
/// CRUD actions, and conflict/bad-request error paths.
/// Core conflict creation test already lives in <see cref="ControllersUnitTests"/>.
/// </summary>
public class AppointmentsControllerTests
{
    private static readonly FakeAuditLogService Audit = new();

    // -----------------------------------------------------------------------
    // GET /api/Appointments  — no filter
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAll_WithNoFilter_ShouldReturnAllAppointments()
    {
        var appointments = new[] { BuildAppt(), BuildAppt() };
        var service = new FakeAppointmentService { All = appointments };
        var sut = CreateController(service);

        var result = await sut.GetAll(null, null, null);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(appointments);
        service.GetAllCalls.Should().Be(1);
    }

    // -----------------------------------------------------------------------
    // GET /api/Appointments?patientId=...
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAll_WithPatientId_ShouldFilterByPatient()
    {
        var patientId = Guid.NewGuid();
        var patientAppts = new[] { BuildAppt(), BuildAppt() };
        var service = new FakeAppointmentService { ByPatient = patientAppts };
        var sut = CreateController(service);

        var result = await sut.GetAll(null, patientId, null);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(patientAppts);
        service.GetByPatientId.Should().Be(patientId);
    }

    // -----------------------------------------------------------------------
    // GET /api/Appointments?staffMemberId=...
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAll_WithStaffMemberId_ShouldFilterByStaff()
    {
        var staffId = Guid.NewGuid();
        var staffAppts = new[] { BuildAppt() };
        var service = new FakeAppointmentService { ByStaff = staffAppts };
        var sut = CreateController(service);

        var result = await sut.GetAll(null, null, staffId);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(staffAppts);
        service.GetByStaffId.Should().Be(staffId);
    }

    // -----------------------------------------------------------------------
    // GET /api/Appointments?date=...
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAll_WithDate_ShouldFilterByDate()
    {
        var date = DateTime.UtcNow.Date;
        var todayAppts = new[] { BuildAppt() };
        var service = new FakeAppointmentService { ByDate = todayAppts };
        var sut = CreateController(service);

        var result = await sut.GetAll(date, null, null);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(todayAppts);
        service.GetByDateValue.Should().Be(date);
    }

    // -----------------------------------------------------------------------
    // GET /api/Appointments/{id}  — found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenAppointmentExists()
    {
        var appt = BuildAppt();
        var service = new FakeAppointmentService { ById = appt };
        var sut = CreateController(service);

        var result = await sut.GetById(appt.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(appt);
    }

    // -----------------------------------------------------------------------
    // GET /api/Appointments/{id}  — not found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenAppointmentDoesNotExist()
    {
        var service = new FakeAppointmentService { ById = null };
        var sut = CreateController(service);

        var result = await sut.GetById(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // -----------------------------------------------------------------------
    // POST /api/Appointments  — success
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WhenRequestIsValid()
    {
        var service = new FakeAppointmentService();
        var sut = CreateController(service);

        var result = await sut.Create(new AppointmentUpsertRequest
        {
            PatientId = Guid.NewGuid(),
            StaffMemberId = Guid.NewGuid(),
            AppointmentDate = DateTime.UtcNow.Date.AddDays(1),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30)),
            Status = AppointmentStatus.Scheduled
        });

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    // -----------------------------------------------------------------------
    // POST /api/Appointments  — bad request (ArgumentException)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenArgumentExceptionThrown()
    {
        var service = new FakeAppointmentService
        {
            CreateException = new ArgumentException("start time must be earlier than end time")
        };
        var sut = CreateController(service);

        var result = await sut.Create(new AppointmentUpsertRequest
        {
            PatientId = Guid.NewGuid(),
            StaffMemberId = Guid.NewGuid(),
            AppointmentDate = DateTime.UtcNow.Date.AddDays(1),
            StartTime = TimeSpan.FromHours(11),
            EndTime = TimeSpan.FromHours(9),
            Status = AppointmentStatus.Scheduled
        });

        result.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("start time must be earlier than end time");
    }

    // -----------------------------------------------------------------------
    // PUT /api/Appointments/{id}  — not found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenAppointmentDoesNotExist()
    {
        var service = new FakeAppointmentService { ById = null };
        var sut = CreateController(service);

        var result = await sut.Update(Guid.NewGuid(), new AppointmentUpsertRequest
        {
            PatientId = Guid.NewGuid(),
            StaffMemberId = Guid.NewGuid(),
            AppointmentDate = DateTime.UtcNow.Date.AddDays(1),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Status = AppointmentStatus.Scheduled
        });

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // -----------------------------------------------------------------------
    // PUT /api/Appointments/{id}  — conflict
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Update_ShouldReturnConflict_WhenSchedulingConflictOccurs()
    {
        var existing = BuildAppt();
        var service = new FakeAppointmentService
        {
            ById = existing,
            UpdateException = new InvalidOperationException("Scheduling conflict detected.")
        };
        var sut = CreateController(service);

        var result = await sut.Update(existing.Id, new AppointmentUpsertRequest
        {
            PatientId = Guid.NewGuid(),
            StaffMemberId = Guid.NewGuid(),
            AppointmentDate = existing.AppointmentDate,
            StartTime = existing.StartTime,
            EndTime = existing.EndTime,
            Status = AppointmentStatus.Confirmed
        });

        result.Result.Should().BeOfType<ConflictObjectResult>()
            .Which.Value.Should().Be("Scheduling conflict detected.");
    }

    // -----------------------------------------------------------------------
    // DELETE /api/Appointments/{id}  — found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenAppointmentDeleted()
    {
        var service = new FakeAppointmentService { DeleteResult = true };
        var sut = CreateController(service);

        var result = await sut.Delete(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }

    // -----------------------------------------------------------------------
    // DELETE /api/Appointments/{id}  — not found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenAppointmentDoesNotExist()
    {
        var service = new FakeAppointmentService { DeleteResult = false };
        var sut = CreateController(service);

        var result = await sut.Delete(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static AppointmentsController CreateController(IAppointmentService service)
    {
        var controller = new AppointmentsController(service, Audit, NullLogger<AppointmentsController>.Instance);
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

    private static Appointment BuildAppt() => new()
    {
        Id = Guid.NewGuid(),
        PatientId = Guid.NewGuid(),
        StaffMemberId = Guid.NewGuid(),
        AppointmentDate = DateTime.UtcNow.Date.AddDays(1),
        StartTime = TimeSpan.FromHours(9),
        EndTime = TimeSpan.FromHours(10),
        Status = AppointmentStatus.Scheduled
    };

    // -----------------------------------------------------------------------
    // Fakes
    // -----------------------------------------------------------------------

    private sealed class FakeAppointmentService : IAppointmentService
    {
        public IEnumerable<Appointment> All { get; set; } = Array.Empty<Appointment>();
        public IEnumerable<Appointment> ByPatient { get; set; } = Array.Empty<Appointment>();
        public IEnumerable<Appointment> ByStaff { get; set; } = Array.Empty<Appointment>();
        public IEnumerable<Appointment> ByDate { get; set; } = Array.Empty<Appointment>();
        public Appointment? ById { get; set; }
        public bool DeleteResult { get; set; } = true;
        public Exception? CreateException { get; set; }
        public Exception? UpdateException { get; set; }

        public Guid GetByPatientId { get; private set; }
        public Guid GetByStaffId { get; private set; }
        public DateTime GetByDateValue { get; private set; }
        public int GetAllCalls { get; private set; }

        public Task<IEnumerable<Appointment>> GetAllAsync()
        {
            GetAllCalls++;
            return Task.FromResult(All);
        }

        public Task<IEnumerable<Appointment>> GetByPatientAsync(Guid patientId)
        {
            GetByPatientId = patientId;
            return Task.FromResult(ByPatient);
        }

        public Task<IEnumerable<Appointment>> GetByStaffAsync(Guid staffMemberId)
        {
            GetByStaffId = staffMemberId;
            return Task.FromResult(ByStaff);
        }

        public Task<IEnumerable<Appointment>> GetByDateAsync(DateTime date)
        {
            GetByDateValue = date;
            return Task.FromResult(ByDate);
        }

        public Task<Appointment?> GetByIdAsync(Guid id) => Task.FromResult(ById);

        public Task<Appointment> CreateAsync(Appointment appointment)
        {
            if (CreateException is not null) throw CreateException;
            return Task.FromResult(appointment);
        }

        public Task<Appointment> UpdateAsync(Appointment appointment)
        {
            if (UpdateException is not null) throw UpdateException;
            return Task.FromResult(appointment);
        }

        public Task<bool> UpdateStatusAsync(Guid id, AppointmentStatus status) => Task.FromResult(true);

        public Task<bool> DeleteAsync(Guid id) => Task.FromResult(DeleteResult);
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public Task<IEnumerable<AuditLog>> GetAllAsync() => Task.FromResult(Enumerable.Empty<AuditLog>());
        public Task<AuditLog?> GetByIdAsync(Guid id) => Task.FromResult<AuditLog?>(null);
        public Task<AuditLog> CreateAsync(AuditLog log) => Task.FromResult(log);
    }
}
