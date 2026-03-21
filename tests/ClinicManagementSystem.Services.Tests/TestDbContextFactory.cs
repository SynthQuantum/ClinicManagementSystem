using ClinicManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Services.Tests;

internal static class TestDbContextFactory
{
    public static ClinicDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new ClinicDbContext(options);
    }
}
