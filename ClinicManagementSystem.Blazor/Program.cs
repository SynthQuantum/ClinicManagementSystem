using ClinicManagementSystem.Blazor.Components;
using ClinicManagementSystem.Data;
using ClinicManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("Development"))
{
    builder.WebHost.UseStaticWebAssets();
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
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
builder.Services.AddClinicServices();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbStartup");
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        if (db.Database.GetMigrations().Any())
        {
            var hasPendingMigrations = db.Database.GetPendingMigrations().Any();
            if (hasPendingMigrations)
            {
                db.Database.Migrate();
                logger.LogInformation("Database migration completed successfully.");
                await DevelopmentDataSeeder.SeedAsync(db, logger);
            }
            else
            {
                logger.LogInformation("No pending migrations. Skipping seed.");
            }
        }
        else
        {
            db.Database.EnsureCreated();
            logger.LogInformation("Database created with EnsureCreated (no migrations found).");
        }
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .WithStaticAssets();

app.Run();
