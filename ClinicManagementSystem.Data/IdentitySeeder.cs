using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Data;

public static class IdentitySeeder
{
    public static readonly string[] DefaultRoles = ["Admin", "Doctor", "Receptionist"];

    public static async Task SeedAsync(
        IServiceProvider services,
        ILogger logger,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        // Seed roles
        foreach (var roleName in DefaultRoles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (result.Succeeded)
                    logger.LogInformation("Identity: created role '{Role}'", roleName);
                else
                    logger.LogWarning("Identity: failed to create role '{Role}': {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        var canSeedByEnvironment = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        var forceSeedByConfig = configuration.GetValue<bool>("IdentitySeed:SeedAdmin");

        if (!canSeedByEnvironment && !forceSeedByConfig)
        {
            logger.LogInformation("Identity: skipping default admin seeding outside Development/Testing because IdentitySeed:SeedAdmin is not enabled.");
            return;
        }

        var adminEmail = configuration["IdentitySeed:AdminEmail"];
        var adminPassword = configuration["IdentitySeed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Identity: default admin seeding skipped because IdentitySeed:AdminEmail/AdminPassword are not configured.");
            return;
        }

        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                NormalizedEmail = adminEmail.ToUpperInvariant(),
                NormalizedUserName = adminEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Admin",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("Identity: seeded configured admin user '{Email}'", adminEmail);
            }
            else
            {
                logger.LogWarning("Identity: failed to seed admin user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
