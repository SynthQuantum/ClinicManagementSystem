using ClinicManagementSystem.API.Controllers;
using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicManagementSystem.API.Tests;

public class ControllersUnitTests
{
    [Fact]
    public async Task PatientsController_GetAll_WithQuery_ShouldUseSearch()
    {
        var expected = new[]
        {
            new Patient { FirstName = "Search", LastName = "Result", DateOfBirth = new DateTime(1990, 1, 1) }
        };
        var service = new FakePatientService { SearchResults = expected };
        var controller = new PatientsController(service, new FakeAuditLogService(), NullLogger<PatientsController>.Instance);

        var result = await controller.GetAll("search");
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;

        ok.Value.Should().BeEquivalentTo(expected);
        service.SearchInvocations.Should().Be(1);
        service.GetAllInvocations.Should().Be(0);
    }

    [Fact]
    public async Task AppointmentsController_Create_WhenConflictOccurs_ShouldReturnConflict()
    {
        var service = new FakeAppointmentService
        {
            CreateException = new InvalidOperationException("Scheduling conflict detected.")
        };
        var controller = new AppointmentsController(service, new FakeAuditLogService(), NullLogger<AppointmentsController>.Instance);

        var result = await controller.Create(new AppointmentUpsertRequest
        {
            PatientId = Guid.NewGuid(),
            StaffMemberId = Guid.NewGuid(),
            AppointmentDate = DateTime.UtcNow.Date,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10)
        });

        result.Result.Should().BeOfType<ConflictObjectResult>()
            .Which.Value.Should().Be("Scheduling conflict detected.");
    }

    [Fact]
    public async Task DashboardController_GetSummary_ShouldReturnOkWithSummary()
    {
        var summary = new DashboardSummary { TotalPatients = 5, TotalAppointments = 9 };
        var controller = new DashboardController(new FakeDashboardService { Summary = summary });

        var result = await controller.GetSummary();
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;

        ok.Value.Should().Be(summary);
    }

    private sealed class FakePatientService : IPatientService
    {
        public IEnumerable<Patient> SearchResults { get; set; } = Array.Empty<Patient>();
        public int SearchInvocations { get; private set; }
        public int GetAllInvocations { get; private set; }

        public Task<Patient> CreateAsync(Patient patient) => Task.FromResult(patient);
        public Task<bool> DeleteAsync(Guid id) => Task.FromResult(true);
        public Task<IEnumerable<Patient>> GetAllAsync()
        {
            GetAllInvocations++;
            return Task.FromResult(Enumerable.Empty<Patient>());
        }
        public Task<Patient?> GetByIdAsync(Guid id) => Task.FromResult<Patient?>(null);
        public Task<IEnumerable<Patient>> SearchAsync(string searchTerm)
        {
            SearchInvocations++;
            return Task.FromResult(SearchResults);
        }
        public Task<Patient> UpdateAsync(Patient patient) => Task.FromResult(patient);
    }

    private sealed class FakeAppointmentService : IAppointmentService
    {
        public Exception? CreateException { get; set; }

        public Task<Appointment> CreateAsync(Appointment appointment)
        {
            if (CreateException is not null)
            {
                throw CreateException;
            }

            return Task.FromResult(appointment);
        }

        public Task<bool> DeleteAsync(Guid id) => Task.FromResult(true);
        public Task<IEnumerable<Appointment>> GetAllAsync() => Task.FromResult(Enumerable.Empty<Appointment>());
        public Task<IEnumerable<Appointment>> SearchAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            Guid? patientId = null,
            Guid? staffMemberId = null,
            AppointmentStatus? status = null,
            bool highRiskOnly = false)
            => Task.FromResult(Enumerable.Empty<Appointment>());
        public Task<IEnumerable<Appointment>> GetByDateAsync(DateTime date) => Task.FromResult(Enumerable.Empty<Appointment>());
        public Task<Appointment?> GetByIdAsync(Guid id) => Task.FromResult<Appointment?>(null);
        public Task<IEnumerable<Appointment>> GetByPatientAsync(Guid patientId) => Task.FromResult(Enumerable.Empty<Appointment>());
        public Task<IEnumerable<Appointment>> GetByStaffAsync(Guid staffMemberId) => Task.FromResult(Enumerable.Empty<Appointment>());
        public Task<Appointment> UpdateAsync(Appointment appointment) => Task.FromResult(appointment);
        public Task<bool> UpdateStatusAsync(Guid id, AppointmentStatus status) => Task.FromResult(true);
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public DashboardSummary Summary { get; set; } = new();

        public Task<IEnumerable<AppointmentTrendPoint>> GetAppointmentTrendAsync(int days = 30)
            => Task.FromResult(Enumerable.Empty<AppointmentTrendPoint>());

        public Task<DashboardSummary> GetSummaryAsync() => Task.FromResult(Summary);

        public Task<IEnumerable<StaffWorkloadSummary>> GetStaffWorkloadAsync()
            => Task.FromResult(Enumerable.Empty<StaffWorkloadSummary>());
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public Task<IEnumerable<AuditLog>> GetAllAsync() => Task.FromResult(Enumerable.Empty<AuditLog>());

        public Task<AuditLog?> GetByIdAsync(Guid id) => Task.FromResult<AuditLog?>(null);

        public Task<AuditLog> CreateAsync(AuditLog log) => Task.FromResult(log);
    }
}
