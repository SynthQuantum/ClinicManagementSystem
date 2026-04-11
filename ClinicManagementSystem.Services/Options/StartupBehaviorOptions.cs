namespace ClinicManagementSystem.Services.Options;

public class StartupBehaviorOptions
{
    public const string SectionName = "StartupBehavior";

    public bool ApplyMigrations { get; set; } = true;

    public bool EnsureCreatedWhenNoMigrations { get; set; } = true;

    public bool SeedDevelopmentData { get; set; } = true;

    public bool SeedIdentityData { get; set; } = true;

    public bool FailFastOnInitializationError { get; set; } = true;
}
