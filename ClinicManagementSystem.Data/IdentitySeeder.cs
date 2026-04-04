using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Data;

public static class IdentitySeeder
{
    public static readonly string[] DefaultRoles = ["Admin", "Doctor", "Receptionist"];

    // Development-only credentials — change before deploying to production
    private const string DevAdminEmail = "admin@clinic.local";
    private const string DevAdminPassword = "Admin@12345!";

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
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

        // Seed development admin user
        if (await userManager.FindByEmailAsync(DevAdminEmail) is null)
        {
            var admin = new AppUser
            {
                Id = new Guid("d0000000-0000-0000-0000-000000000001"),
                UserName = DevAdminEmail,
                Email = DevAdminEmail,
                NormalizedEmail = DevAdminEmail.ToUpperInvariant(),
                NormalizedUserName = DevAdminEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Admin",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(admin, DevAdminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("Identity: seeded dev admin user '{Email}'", DevAdminEmail);
            }
            else
            {
                logger.LogWarning("Identity: failed to seed admin user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
