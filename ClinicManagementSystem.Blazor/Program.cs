using ClinicManagementSystem.Blazor.Components;
using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("Development"))
{
    builder.WebHost.UseStaticWebAssets();
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();

builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ClinicDb"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<ClinicDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/unauthorized";
});
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddClinicServices();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbStartup");
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();

        if (db.Database.IsRelational())
        {
            if (db.Database.GetMigrations().Any())
            {
                if (db.Database.GetPendingMigrations().Any())
                {
                    db.Database.Migrate();
                    logger.LogInformation("Database migration completed successfully.");
                }
                else
                {
                    logger.LogInformation("No pending migrations.");
                }
            }
            else
            {
                db.Database.EnsureCreated();
                logger.LogInformation("Database created with EnsureCreated (no migrations found).");
            }
        }
        else
        {
            db.Database.EnsureCreated();
            logger.LogInformation("Database created with EnsureCreated for non-relational provider.");
        }

        await DevelopmentDataSeeder.SeedAsync(db, logger);
        await IdentitySeeder.SeedAsync(scope.ServiceProvider, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed during startup.");
        if (app.Environment.IsDevelopment())
        {
            throw;
        }

        logger.LogWarning("Continuing startup without applying migrations because environment is not Development.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/account/login", async (
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    IAuditLogService auditLogService,
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] string? returnUrl) =>
{
    var user = await userManager.FindByEmailAsync(email);
    if (user is null || !user.IsActive)
    {
        await auditLogService.CreateAsync(new AuditLog
        {
            EntityName = "Authentication",
            ActionType = "LoginFailed",
            Description = $"Blazor login failed for '{email}' (user missing/inactive)"
        });

        return Results.LocalRedirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    var result = await signInManager.PasswordSignInAsync(user.UserName!, password, isPersistent: true, lockoutOnFailure: true);

    if (!result.Succeeded)
    {
        await auditLogService.CreateAsync(new AuditLog
        {
            EntityName = "Authentication",
            ActionType = result.IsLockedOut ? "AccountLockedOut" : "LoginFailed",
            PerformedByUserId = user.Id,
            Description = result.IsLockedOut
                ? $"Blazor account locked for '{email}'"
                : $"Blazor login failed for '{email}' (bad credentials)"
        });

        return Results.LocalRedirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    var roles = await userManager.GetRolesAsync(user);

    await auditLogService.CreateAsync(new AuditLog
    {
        EntityName = "Authentication",
        ActionType = "LoginSuccess",
        PerformedByUserId = user.Id,
        Description = $"Blazor login success for '{email}'"
    });

    var destination = !string.IsNullOrWhiteSpace(returnUrl) && !string.Equals(returnUrl, "/", StringComparison.Ordinal)
        ? returnUrl
        : roles.Contains("Receptionist", StringComparer.OrdinalIgnoreCase)
            ? "/appointments"
            : "/";

    return Results.LocalRedirect(destination);
}).DisableAntiforgery();

app.MapPost("/account/register", async (
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    IAuditLogService auditLogService,
    [FromForm] string firstName,
    [FromForm] string lastName,
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] string confirmPassword,
    [FromForm] string? returnUrl) =>
{
    var trimmedFirstName = firstName.Trim();
    var trimmedLastName = lastName.Trim();
    var trimmedEmail = email.Trim();

    if (string.IsNullOrWhiteSpace(trimmedFirstName)
        || string.IsNullOrWhiteSpace(trimmedLastName)
        || string.IsNullOrWhiteSpace(trimmedEmail))
    {
        return Results.LocalRedirect($"/register?error={Uri.EscapeDataString("All registration fields are required.")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
    {
        return Results.LocalRedirect($"/register?error={Uri.EscapeDataString("Passwords do not match.")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    if (await userManager.FindByEmailAsync(trimmedEmail) is not null)
    {
        await auditLogService.CreateAsync(new AuditLog
        {
            EntityName = "Authentication",
            ActionType = "RegistrationFailed",
            Description = $"Blazor registration failed for '{trimmedEmail}' because the email already exists"
        });

        return Results.LocalRedirect($"/register?error={Uri.EscapeDataString("An account with that email already exists.")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    var user = new AppUser
    {
        UserName = trimmedEmail,
        Email = trimmedEmail,
        FirstName = trimmedFirstName,
        LastName = trimmedLastName,
        Role = UserRole.Receptionist,
        IsActive = true,
        EmailConfirmed = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    var result = await userManager.CreateAsync(user, password);
    if (!result.Succeeded)
    {
        var errorMessage = string.Join(" ", result.Errors.Select(error => error.Description));

        await auditLogService.CreateAsync(new AuditLog
        {
            EntityName = "Authentication",
            ActionType = "RegistrationFailed",
            Description = $"Blazor registration failed for '{trimmedEmail}': {errorMessage}"
        });

        return Results.LocalRedirect($"/register?error={Uri.EscapeDataString(errorMessage)}&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    await userManager.AddToRoleAsync(user, "Receptionist");

    await auditLogService.CreateAsync(new AuditLog
    {
        EntityName = "Authentication",
        ActionType = "RegistrationSuccess",
        PerformedByUserId = user.Id,
        Description = $"Blazor self-registration succeeded for '{trimmedEmail}' with role 'Receptionist'"
    });

    await signInManager.SignInAsync(user, isPersistent: true);
    return Results.LocalRedirect("/appointments");
}).DisableAntiforgery();

app.MapPost("/account/logout", async (
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    IAuditLogService auditLogService,
    HttpContext httpContext) =>
{
    var currentUser = await userManager.GetUserAsync(httpContext.User);

    if (currentUser is not null)
    {
        await auditLogService.CreateAsync(new AuditLog
        {
            EntityName = "Authentication",
            ActionType = "Logout",
            PerformedByUserId = currentUser.Id,
            Description = $"Blazor logout for '{currentUser.Email}'"
        });
    }

    await signInManager.SignOutAsync();
    return Results.LocalRedirect("/login");
}).RequireAuthorization().DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .WithStaticAssets();

app.Run();
