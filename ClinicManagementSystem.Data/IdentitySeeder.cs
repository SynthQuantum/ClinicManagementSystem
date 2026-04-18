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
        var forceSeedByConfig = configuration.GetValue<bool>("Authentication:IdentitySeed:SeedAdmin")
            || configuration.GetValue<bool>("IdentitySeed:SeedAdmin");

        if (!canSeedByEnvironment && !forceSeedByConfig)
        {
            logger.LogInformation("Identity: skipping default admin seeding outside Development/Testing because IdentitySeed:SeedAdmin is not enabled.");
            return;
        }

        var adminEmail = configuration["Authentication:IdentitySeed:AdminEmail"]
            ?? configuration["IdentitySeed:AdminEmail"];
        var adminPassword = configuration["Authentication:IdentitySeed:AdminPassword"]
            ?? configuration["IdentitySeed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Identity: default admin seeding skipped because IdentitySeed:AdminEmail/AdminPassword are not configured.");
            return;
        }

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

        if (existingAdmin is null)
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

            return;
        }

        // Keep configured admin credentials usable in non-dev environments where force seeding is enabled.
        var isPasswordValid = await userManager.CheckPasswordAsync(existingAdmin, adminPassword);
        if (!isPasswordValid)
        {
            var removeResult = await userManager.RemovePasswordAsync(existingAdmin);
            if (!removeResult.Succeeded)
            {
                logger.LogWarning("Identity: failed to remove existing admin password for '{Email}': {Errors}",
                    adminEmail, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
            }

            var addResult = await userManager.AddPasswordAsync(existingAdmin, adminPassword);
            if (addResult.Succeeded)
            {
                logger.LogInformation("Identity: reset password for configured admin user '{Email}'", adminEmail);
            }
            else
            {
                logger.LogWarning("Identity: failed to reset admin password for '{Email}': {Errors}",
                    adminEmail, string.Join(", ", addResult.Errors.Select(e => e.Description)));
            }
        }

        if (await userManager.IsLockedOutAsync(existingAdmin))
        {
            await userManager.SetLockoutEndDateAsync(existingAdmin, null);
            await userManager.ResetAccessFailedCountAsync(existingAdmin);
            logger.LogInformation("Identity: unlocked configured admin user '{Email}'", adminEmail);
        }

        if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
        {
            await userManager.AddToRoleAsync(existingAdmin, "Admin");
            logger.LogInformation("Identity: ensured configured admin user '{Email}' is in role 'Admin'", adminEmail);
        }
    }
}
