using ClinicManagementSystem.API.Auth;
using ClinicManagementSystem.API.BackgroundServices;
using ClinicManagementSystem.API.Middleware;
using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services;
using ClinicManagementSystem.Services.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var startupBehavior = builder.Configuration
    .GetSection(StartupBehaviorOptions.SectionName)
    .Get<StartupBehaviorOptions>() ?? new StartupBehaviorOptions();

ValidateRequiredConfiguration(builder.Configuration, builder.Environment);

var connectionString = builder.Configuration.GetConnectionString("ClinicDb");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<ClinicDbContext>(options =>
        options.UseInMemoryDatabase("ClinicManagementSystemApiTesting"));
}
else
{
    builder.Services.AddDbContext<ClinicDbContext>(options =>
        options.UseSqlServer(
            connectionString,
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null)));
}

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

var jwtSection = builder.Configuration.GetSection("Authentication:Jwt");
if (!jwtSection.Exists())
{
    jwtSection = builder.Configuration.GetSection("Jwt");
}

var jwtKey = jwtSection["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Critical security configuration error: Jwt:Key is not configured. " +
        "Set JWT_KEY environment variable or use 'dotnet user-secrets set Jwt:Key <strong-key>' in Development. " +
        "Never commit JWT secrets to source control.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenService>();
builder.Services.AddClinicServices();
builder.Services.Configure<NotificationReminderOptions>(builder.Configuration.GetSection(NotificationReminderOptions.SectionName));
builder.Services.Configure<PerformanceMonitoringOptions>(builder.Configuration.GetSection(PerformanceMonitoringOptions.SectionName));
builder.Services.Configure<MlArtifactsOptions>(builder.Configuration.GetSection(MlArtifactsOptions.SectionName));
builder.Services.Configure<StartupBehaviorOptions>(builder.Configuration.GetSection(StartupBehaviorOptions.SectionName));
builder.Services.AddHostedService<ReminderProcessingHostedService>();
builder.Services.AddHostedService<PerformanceSampleFlushHostedService>();

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
        logger.LogError(ex, "Database migration/seed failed during startup.");
        if (startupBehavior.FailFastOnInitializationError)
        {
            throw;
        }

        logger.LogWarning("Continuing startup because StartupBehavior:FailFastOnInitializationError is false.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<PerformanceMonitoringMiddleware>();
app.UseAuthentication();
app.UseMiddleware<RequestAuditLoggingMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void ValidateRequiredConfiguration(IConfiguration configuration, IHostEnvironment environment)
{
    if (!environment.IsEnvironment("Testing")
        && string.IsNullOrWhiteSpace(configuration.GetConnectionString("ClinicDb")))
    {
        throw new InvalidOperationException("Configuration error: ConnectionStrings:ClinicDb is required outside Testing.");
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

public partial class Program;
