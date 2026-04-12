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
/// Unit tests for <see cref="PatientsController"/> covering all CRUD actions
/// and query-based search branching. Fakes are self-contained in this file.
/// </summary>
public class PatientsControllerTests
{
    private static readonly FakeAuditLogService Audit = new();

    // -----------------------------------------------------------------------
    // GET /api/Patients  (no query)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAll_WithNoQuery_ShouldCallGetAllAsync()
    {
        var patients = new[] { BuildPatient("Alice"), BuildPatient("Bob") };
        var service = new FakePatientService { AllPatients = patients };
        var sut = CreateController(service);

        var result = await sut.GetAll(null);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(patients);
        service.GetAllCalls.Should().Be(1);
        service.SearchCalls.Should().Be(0);
    }

    // -----------------------------------------------------------------------
    // GET /api/Patients  (with query)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAll_WithQuery_ShouldCallSearchAsync()
    {
        var hits = new[] { BuildPatient("SearchResult") };
        var service = new FakePatientService { SearchResults = hits };
        var sut = CreateController(service);

        var result = await sut.GetAll("search");

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(hits);
        service.SearchCalls.Should().Be(1);
        service.GetAllCalls.Should().Be(0);
    }

    // -----------------------------------------------------------------------
    // GET /api/Patients/{id}  — found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenPatientExists()
    {
        var patient = BuildPatient("Eve");
        var service = new FakePatientService { ById = patient };
        var sut = CreateController(service);

        var result = await sut.GetById(patient.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(patient);
    }

    // -----------------------------------------------------------------------
    // GET /api/Patients/{id}  — not found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenPatientDoesNotExist()
    {
        var service = new FakePatientService { ById = null };
        var sut = CreateController(service);

        var result = await sut.GetById(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // -----------------------------------------------------------------------
    // POST /api/Patients
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WithNewPatient()
    {
        var service = new FakePatientService();
        var sut = CreateController(service);

        var request = new PatientUpsertRequest
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        var result = await sut.Create(request);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(PatientsController.GetById));
        var patient = created.Value.Should().BeOfType<Patient>().Subject;
        patient.FirstName.Should().Be("John");
        patient.LastName.Should().Be("Doe");
    }

    // -----------------------------------------------------------------------
    // PUT /api/Patients/{id}  — found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Update_ShouldReturnOk_WhenPatientExists()
    {
        var existing = BuildPatient("Original");
        var service = new FakePatientService { ById = existing };
        var sut = CreateController(service);

        var request = new PatientUpsertRequest
        {
            FirstName = "Updated",
            LastName = "Name",
            DateOfBirth = existing.DateOfBirth
        };

        var result = await sut.Update(existing.Id, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var updated = ok.Value.Should().BeOfType<Patient>().Subject;
        updated.FirstName.Should().Be("Updated");
    }

    // -----------------------------------------------------------------------
    // PUT /api/Patients/{id}  — not found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenPatientDoesNotExist()
    {
        var service = new FakePatientService { ById = null };
        var sut = CreateController(service);

        var result = await sut.Update(Guid.NewGuid(), new PatientUpsertRequest
        {
            FirstName = "X",
            LastName = "Y",
            DateOfBirth = new DateTime(1990, 1, 1)
        });

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // -----------------------------------------------------------------------
    // DELETE /api/Patients/{id}  — found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenPatientDeleted()
    {
        var service = new FakePatientService { DeleteResult = true };
        var sut = CreateController(service);

        var result = await sut.Delete(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }

    // -----------------------------------------------------------------------
    // DELETE /api/Patients/{id}  — not found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenPatientDoesNotExist()
    {
        var service = new FakePatientService { DeleteResult = false };
        var sut = CreateController(service);

        var result = await sut.Delete(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static PatientsController CreateController(IPatientService service)
    {
        var controller = new PatientsController(service, Audit, NullLogger<PatientsController>.Instance);
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

    private static Patient BuildPatient(string first) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = first,
        LastName = "Test",
        DateOfBirth = new DateTime(1990, 1, 1)
    };

    // -----------------------------------------------------------------------
    // Fakes
    // -----------------------------------------------------------------------

    private sealed class FakePatientService : IPatientService
    {
        public IEnumerable<Patient> AllPatients { get; set; } = Array.Empty<Patient>();
        public IEnumerable<Patient> SearchResults { get; set; } = Array.Empty<Patient>();
        public Patient? ById { get; set; }
        public bool DeleteResult { get; set; } = true;

        public int GetAllCalls { get; private set; }
        public int SearchCalls { get; private set; }

        public Task<IEnumerable<Patient>> GetAllAsync()
        {
            GetAllCalls++;
            return Task.FromResult(AllPatients);
        }

        public Task<IEnumerable<Patient>> SearchAsync(string searchTerm)
        {
            SearchCalls++;
            return Task.FromResult(SearchResults);
        }

        public Task<Patient?> GetByIdAsync(Guid id) => Task.FromResult(ById);

        public Task<Patient> CreateAsync(Patient patient) => Task.FromResult(patient);

        public Task<Patient> UpdateAsync(Patient patient) => Task.FromResult(patient);

        public Task<bool> DeleteAsync(Guid id) => Task.FromResult(DeleteResult);
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public Task<IEnumerable<AuditLog>> GetAllAsync(int take = 500) => Task.FromResult(Enumerable.Empty<AuditLog>());
        public Task<AuditLog?> GetByIdAsync(Guid id) => Task.FromResult<AuditLog?>(null);
        public Task<AuditLog> CreateAsync(AuditLog log) => Task.FromResult(log);
        public Task<IEnumerable<AuditLog>> GetByUserAsync(Guid userId, int take = 200) => Task.FromResult(Enumerable.Empty<AuditLog>());
        public Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, Guid? entityId = null, int take = 200) => Task.FromResult(Enumerable.Empty<AuditLog>());
        public Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, int maxResults = 500) => Task.FromResult(Enumerable.Empty<AuditLog>());
        public Task<IEnumerable<AuditLog>> GetSecurityEventsAsync(int take = 100) => Task.FromResult(Enumerable.Empty<AuditLog>());
    }
}
