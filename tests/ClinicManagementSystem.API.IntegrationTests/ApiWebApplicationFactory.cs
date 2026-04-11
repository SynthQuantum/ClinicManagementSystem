using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManagementSystem.API.IntegrationTests;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public ApiWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_KEY", "test-jwt-signing-key-32-characters-minimum-length!");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "ClinicManagementSystem");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "ClinicManagementSystemAPI");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-jwt-signing-key-32-characters-minimum-length!",
                ["StartupBehavior:SeedIdentityData"] = "true",
                ["IdentitySeed:SeedAdmin"] = "true",
                ["IdentitySeed:AdminEmail"] = "admin.test@clinic.local",
                ["IdentitySeed:AdminPassword"] = "AdminTest@12345!"
            });
        });

        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            Seed(db);

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            if (!roleManager.RoleExistsAsync("Admin").GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole<Guid>("Admin")).GetAwaiter().GetResult();
            }

            const string adminEmail = "admin.test@clinic.local";
            const string adminPassword = "AdminTest@12345!";

            var adminUser = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
            if (adminUser is null)
            {
                adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Integration",
                    LastName = "Admin",
                    Role = UserRole.Admin,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var createResult = userManager.CreateAsync(adminUser, adminPassword).GetAwaiter().GetResult();
                if (!createResult.Succeeded)
                {
                    var reasons = string.Join(", ", createResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Failed to seed integration admin user: {reasons}");
                }
            }

            var roles = userManager.GetRolesAsync(adminUser).GetAwaiter().GetResult();
            if (!roles.Contains("Admin"))
            {
                userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
            }
        });
    }

    private static void Seed(ClinicDbContext db)
    {
        var patient1 = new Patient { FirstName = "Integration", LastName = "Patient1", DateOfBirth = new DateTime(1990, 1, 1) };
        var patient2 = new Patient { FirstName = "Integration", LastName = "Patient2", DateOfBirth = new DateTime(1988, 2, 2) };
        var staff = new StaffMember
        {
            FirstName = "Integration",
            LastName = "Doctor",
            Email = "integration.doctor@test.local",
            Role = UserRole.Doctor,
            Specialty = "General"
        };

        db.Patients.AddRange(patient1, patient2);
        db.StaffMembers.Add(staff);
        db.SaveChanges();

        var today = DateTime.UtcNow.Date;
        db.Appointments.AddRange(
            new Appointment
            {
                PatientId = patient1.Id,
                StaffMemberId = staff.Id,
                AppointmentDate = today,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(9, 30, 0),
                Status = AppointmentStatus.Scheduled,
                AppointmentType = AppointmentType.General
            },
            new Appointment
            {
                PatientId = patient2.Id,
                StaffMemberId = staff.Id,
                AppointmentDate = today,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
                Status = AppointmentStatus.Completed,
                AppointmentType = AppointmentType.Checkup
            });

        db.SaveChanges();
    }
}
