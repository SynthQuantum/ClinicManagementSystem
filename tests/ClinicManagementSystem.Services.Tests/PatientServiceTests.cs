using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Implementations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicManagementSystem.Services.Tests;

public class PatientServiceTests
{
    [Fact]
    public async Task CreateAndGetAllAsync_ShouldReturnCreatedPatient()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new PatientService(db, NullLogger<PatientService>.Instance);

        var patient = new Patient
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        await sut.CreateAsync(patient);

        var all = (await sut.GetAllAsync()).ToList();
        all.Should().ContainSingle(p => p.Id == patient.Id && p.FirstName == "John" && p.LastName == "Doe");
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteAndHideFromGetAll()
    {
        using var db = TestDbContextFactory.Create();
        var patient = new Patient
        {
            FirstName = "Alice",
            LastName = "Smith",
            DateOfBirth = new DateTime(1988, 5, 5)
        };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var sut = new PatientService(db, NullLogger<PatientService>.Instance);

        var deleted = await sut.DeleteAsync(patient.Id);

        deleted.Should().BeTrue();
        var stored = await db.Patients.IgnoreQueryFilters().SingleAsync(p => p.Id == patient.Id);
        stored.IsDeleted.Should().BeTrue();
        (await sut.GetAllAsync()).Should().BeEmpty();
    }
}
