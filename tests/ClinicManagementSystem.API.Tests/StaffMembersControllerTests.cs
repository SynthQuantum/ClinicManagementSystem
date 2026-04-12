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
/// Unit tests for <see cref="StaffMembersController"/> covering all CRUD actions.
/// </summary>
public class StaffMembersControllerTests
{
    private static readonly FakeAuditLogService Audit = new();

    // -----------------------------------------------------------------------
    // GET /api/StaffMembers
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAll_ShouldReturnOkWithAllStaff()
    {
        var staff = new[] { BuildStaff("Dr. Alpha"), BuildStaff("Nurse Beta") };
        var service = new FakeStaffService { All = staff };
        var sut = CreateController(service);

        var result = await sut.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(staff);
    }

    [Fact]
    public async Task GetAll_ShouldReturnEmptyCollection_WhenNoStaffExist()
    {
        var service = new FakeStaffService { All = Array.Empty<StaffMember>() };
        var sut = CreateController(service);

        var result = await sut.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        (ok.Value as IEnumerable<StaffMember>).Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // GET /api/StaffMembers/{id}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenStaffExists()
    {
        var member = BuildStaff("Dr. Found");
        var service = new FakeStaffService { ById = member };
        var sut = CreateController(service);

        var result = await sut.GetById(member.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(member);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenStaffDoesNotExist()
    {
        var service = new FakeStaffService { ById = null };
        var sut = CreateController(service);

        var result = await sut.GetById(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // -----------------------------------------------------------------------
    // POST /api/StaffMembers
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WithNewStaffMember()
    {
        var service = new FakeStaffService();
        var sut = CreateController(service);

        var request = new StaffUpsertRequest
        {
            FirstName = "Jane",
            LastName = "Doctor",
            Email = "jane@clinic.test",
            Role = UserRole.Doctor,
            Specialty = "Cardiology",
            IsAvailable = true
        };

        var result = await sut.Create(request);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(StaffMembersController.GetById));
        var member = created.Value.Should().BeOfType<StaffMember>().Subject;
        member.Email.Should().Be("jane@clinic.test");
        member.Specialty.Should().Be("Cardiology");
    }

    // -----------------------------------------------------------------------
    // PUT /api/StaffMembers/{id}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Update_ShouldReturnOk_WhenStaffExists()
    {
        var existing = BuildStaff("Dr. Original");
        var service = new FakeStaffService { ById = existing };
        var sut = CreateController(service);

        var request = new StaffUpsertRequest
        {
            FirstName = "Dr. Updated",
            LastName = "Name",
            Email = existing.Email,
            Role = existing.Role,
            Specialty = "Neurology",
            IsAvailable = false
        };

        var result = await sut.Update(existing.Id, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var updated = ok.Value.Should().BeOfType<StaffMember>().Subject;
        updated.Specialty.Should().Be("Neurology");
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenStaffDoesNotExist()
    {
        var service = new FakeStaffService { ById = null };
        var sut = CreateController(service);

        var result = await sut.Update(Guid.NewGuid(), new StaffUpsertRequest
        {
            FirstName = "X",
            LastName = "Y",
            Email = "x@test.local"
        });

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // -----------------------------------------------------------------------
    // DELETE /api/StaffMembers/{id}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenStaffDeleted()
    {
        var service = new FakeStaffService { DeleteResult = true };
        var sut = CreateController(service);

        var result = await sut.Delete(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenStaffDoesNotExist()
    {
        var service = new FakeStaffService { DeleteResult = false };
        var sut = CreateController(service);

        var result = await sut.Delete(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static StaffMembersController CreateController(IStaffService service)
    {
        var controller = new StaffMembersController(service, Audit, NullLogger<StaffMembersController>.Instance);
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

    private static StaffMember BuildStaff(string name) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = name,
        LastName = "Member",
        Email = $"{Guid.NewGuid():N}@test.local",
        Role = UserRole.Doctor
    };

    // -----------------------------------------------------------------------
    // Fakes
    // -----------------------------------------------------------------------

    private sealed class FakeStaffService : IStaffService
    {
        public IEnumerable<StaffMember> All { get; set; } = Array.Empty<StaffMember>();
        public StaffMember? ById { get; set; }
        public bool DeleteResult { get; set; } = true;

        public Task<IEnumerable<StaffMember>> GetAllAsync() => Task.FromResult(All);
        public Task<StaffMember?> GetByIdAsync(Guid id) => Task.FromResult(ById);
        public Task<StaffMember> CreateAsync(StaffMember staff) => Task.FromResult(staff);
        public Task<StaffMember> UpdateAsync(StaffMember staff) => Task.FromResult(staff);
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
