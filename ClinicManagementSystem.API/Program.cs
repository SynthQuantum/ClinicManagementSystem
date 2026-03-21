using ClinicManagementSystem.Data;
using ClinicManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
