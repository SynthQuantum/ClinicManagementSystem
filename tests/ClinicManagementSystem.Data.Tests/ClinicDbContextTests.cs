using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Data.Tests;

public class ClinicDbContextTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldSetCreatedAndUpdatedTimestamps()
    {
        await using var db = CreateContext();
        var before = DateTime.UtcNow.AddSeconds(-1);
        var patient = new Patient
        {
            FirstName = "Test",
            LastName = "Patient",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        patient.CreatedAt.Should().BeAfter(before);
        patient.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task QueryFilters_ShouldExcludeSoftDeletedEntities()
    {
        await using var db = CreateContext();
        db.Patients.AddRange(
            new Patient { FirstName = "Visible", LastName = "Patient", DateOfBirth = new DateTime(1991, 1, 1) },
            new Patient { FirstName = "Hidden", LastName = "Patient", DateOfBirth = new DateTime(1992, 2, 2), IsDeleted = true });
        await db.SaveChangesAsync();

        var visiblePatients = await db.Patients.ToListAsync();
        var allPatients = await db.Patients.IgnoreQueryFilters().ToListAsync();

        visiblePatients.Should().ContainSingle(p => p.FirstName == "Visible");
        allPatients.Should().HaveCount(2);
    }

    private static ClinicDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ClinicDbContext(options);
    }
}
