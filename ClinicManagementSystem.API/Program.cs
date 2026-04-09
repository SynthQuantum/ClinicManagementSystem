using ClinicManagementSystem.API.Auth;
using ClinicManagementSystem.API.Middleware;
using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

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
            builder.Configuration.GetConnectionString("ClinicDb"),
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

var jwtSection = builder.Configuration.GetSection("Jwt");
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

        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            await IdentitySeeder.SeedAsync(scope.ServiceProvider, logger, app.Configuration, app.Environment);
        }
        else
        {
            logger.LogInformation("Skipping identity seeding outside Development/Testing environments.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration/seed failed during startup.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<RequestAuditLoggingMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
