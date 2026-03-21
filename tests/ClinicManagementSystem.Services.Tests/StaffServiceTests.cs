using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Implementations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicManagementSystem.Services.Tests;

public class StaffServiceTests
{
    [Fact]
    public async Task CreateAndUpdateAsync_ShouldPersistChanges()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new StaffService(db, NullLogger<StaffService>.Instance);

        var staff = new StaffMember
        {
            FirstName = "Jane",
            LastName = "Miller",
            Email = "jane.miller@test.local",
            Role = UserRole.Doctor,
            Specialty = "Family Medicine"
        };

        await sut.CreateAsync(staff);
        staff.Specialty = "Cardiology";
        await sut.UpdateAsync(staff);

        var updated = await sut.GetByIdAsync(staff.Id);
        updated.Should().NotBeNull();
        updated!.Specialty.Should().Be("Cardiology");
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete()
    {
        using var db = TestDbContextFactory.Create();
        var staff = new StaffMember
        {
            FirstName = "Noah",
            LastName = "Taylor",
            Email = "noah.taylor@test.local",
            Role = UserRole.Nurse
        };
        db.StaffMembers.Add(staff);
        await db.SaveChangesAsync();

        var sut = new StaffService(db, NullLogger<StaffService>.Instance);

        var deleted = await sut.DeleteAsync(staff.Id);

        deleted.Should().BeTrue();
        var stored = await db.StaffMembers.IgnoreQueryFilters().SingleAsync(s => s.Id == staff.Id);
        stored.IsDeleted.Should().BeTrue();
    }
}
