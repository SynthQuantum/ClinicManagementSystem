using ClinicManagementSystem.Blazor.Components;
using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services;
using ClinicManagementSystem.Services.Interfaces;
using ClinicManagementSystem.Services.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);
var startupBehavior = builder.Configuration
    .GetSection(StartupBehaviorOptions.SectionName)
    .Get<StartupBehaviorOptions>() ?? new StartupBehaviorOptions();

ValidateRequiredConfiguration(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("ClinicDb");

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
        connectionString,
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
builder.Services.Configure<NotificationReminderOptions>(builder.Configuration.GetSection(NotificationReminderOptions.SectionName));
builder.Services.Configure<PerformanceMonitoringOptions>(builder.Configuration.GetSection(PerformanceMonitoringOptions.SectionName));
builder.Services.Configure<MlArtifactsOptions>(builder.Configuration.GetSection(MlArtifactsOptions.SectionName));
builder.Services.Configure<StartupBehaviorOptions>(builder.Configuration.GetSection(StartupBehaviorOptions.SectionName));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbStartup");
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();

        if (db.Database.IsRelational())
        {
            var hasMigrations = db.Database.GetMigrations().Any();

            if (startupBehavior.ApplyMigrations && hasMigrations)
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
            else if (!hasMigrations && startupBehavior.EnsureCreatedWhenNoMigrations)
            {
                db.Database.EnsureCreated();
                logger.LogInformation("Database created with EnsureCreated (no migrations found).");
            }
            else
            {
                logger.LogInformation("Relational startup initialization skipped. ApplyMigrations={ApplyMigrations}, EnsureCreatedWhenNoMigrations={EnsureCreatedWhenNoMigrations}, HasMigrations={HasMigrations}",
                    startupBehavior.ApplyMigrations,
                    startupBehavior.EnsureCreatedWhenNoMigrations,
                    hasMigrations);
            }
        }
        else if (startupBehavior.EnsureCreatedWhenNoMigrations)
        {
            db.Database.EnsureCreated();
            logger.LogInformation("Database created with EnsureCreated for non-relational provider.");
        }
        else
        {
            logger.LogInformation("EnsureCreated skipped for non-relational provider by StartupBehavior settings.");
        }

        if (startupBehavior.SeedDevelopmentData)
        {
            await DevelopmentDataSeeder.SeedAsync(db, logger);
        }
        else
        {
            logger.LogInformation("DevelopmentDataSeeder skipped by StartupBehavior settings.");
        }

        if (startupBehavior.SeedIdentityData)
        {
            await IdentitySeeder.SeedAsync(scope.ServiceProvider, logger, app.Configuration, app.Environment);
        }
        else
        {
            logger.LogInformation("IdentitySeeder skipped by StartupBehavior settings.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed during startup.");
        if (startupBehavior.FailFastOnInitializationError)
        {
            throw;
        }

        logger.LogWarning("Continuing startup because StartupBehavior:FailFastOnInitializationError is false.");
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
    var normalizedEmail = email.Trim();

    var user = await userManager.FindByEmailAsync(normalizedEmail);
    if (user is null || !user.IsActive)
    {
        await auditLogService.CreateAsync(new AuditLog
        {
            EntityName = "Authentication",
            ActionType = "LoginFailed",
            Description = $"Blazor login failed for '{normalizedEmail}' (user missing/inactive)"
        });

        return Results.LocalRedirect($"/login?error=1&email={Uri.EscapeDataString(normalizedEmail)}&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
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
                ? $"Blazor account locked for '{normalizedEmail}'"
                : $"Blazor login failed for '{normalizedEmail}' (bad credentials)"
        });

        return Results.LocalRedirect($"/login?error=1&email={Uri.EscapeDataString(normalizedEmail)}&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    var roles = await userManager.GetRolesAsync(user);

    await auditLogService.CreateAsync(new AuditLog
    {
        EntityName = "Authentication",
        ActionType = "LoginSuccess",
        PerformedByUserId = user.Id,
        Description = $"Blazor login success for '{normalizedEmail}'"
    });

    var destination = !string.IsNullOrWhiteSpace(returnUrl) && !string.Equals(returnUrl, "/", StringComparison.Ordinal)
        ? returnUrl
        : roles.Contains("Receptionist", StringComparer.OrdinalIgnoreCase)
            ? "/appointments"
            : "/";

    return Results.LocalRedirect(destination);
});

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
});

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
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .WithStaticAssets();

app.Run();

static void ValidateRequiredConfiguration(IConfiguration configuration)
{
    if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("ClinicDb")))
    {
        throw new InvalidOperationException("Configuration error: ConnectionStrings:ClinicDb is required.");
    }

    var mlOptions = configuration.GetSection(MlArtifactsOptions.SectionName).Get<MlArtifactsOptions>() ?? new MlArtifactsOptions();
    if (string.IsNullOrWhiteSpace(mlOptions.NoShowArtifactsPath))
    {
        throw new InvalidOperationException("Configuration error: MlArtifacts:NoShowArtifactsPath is required.");
    }

    var notificationOptions = configuration.GetSection(NotificationReminderOptions.SectionName).Get<NotificationReminderOptions>() ?? new NotificationReminderOptions();
    if (notificationOptions.FirstReminderHoursBefore <= 0
        || notificationOptions.SecondReminderHoursBefore <= 0
        || notificationOptions.ProcessingBatchSize <= 0
        || notificationOptions.ProcessorIntervalSeconds <= 0)
    {
        throw new InvalidOperationException("Configuration error: NotificationReminders values must be greater than zero.");
    }

    var performanceOptions = configuration.GetSection(PerformanceMonitoringOptions.SectionName).Get<PerformanceMonitoringOptions>() ?? new PerformanceMonitoringOptions();
    if (performanceOptions.FlushIntervalSeconds <= 0
        || performanceOptions.MaxInMemorySamples <= 0
        || performanceOptions.MaxSummarySamples <= 0
        || performanceOptions.SlowEndpointCount <= 0
        || performanceOptions.RecentFailedRequestCount <= 0)
    {
        throw new InvalidOperationException("Configuration error: PerformanceMonitoring values must be greater than zero.");
    }
}
