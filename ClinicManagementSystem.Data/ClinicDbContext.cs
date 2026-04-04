using ClinicManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Data;

public class ClinicDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options) { }

    // Alias for backward compatibility — Identity provides the underlying Users DbSet
    public DbSet<AppUser> AppUsers => Users;
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<VisitRecord> VisitRecords => Set<VisitRecord>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PredictionResult> PredictionResults => Set<PredictionResult>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ClinicSettings> ClinicSettings => Set<ClinicSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Custom table names — keep "App*" prefix to match existing naming and avoid "AspNet*" tables
        modelBuilder.Entity<AppUser>().ToTable("AppUsers");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("AppRoles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("AppUserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("AppUserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("AppUserLogins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("AppRoleClaims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("AppUserTokens");

        // Global soft-delete query filters
        modelBuilder.Entity<AppUser>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<StaffMember>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<VisitRecord>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PredictionResult>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ClinicSettings>().HasQueryFilter(e => !e.IsDeleted);

        // AppUser — email unique index (IdentityDbContext also enforces this, but explicit is fine)
        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // StaffMember
        modelBuilder.Entity<StaffMember>()
            .HasIndex(s => s.Email)
            .IsUnique();

        // Patient -> Appointments (Restrict delete)
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasIndex(a => new { a.AppointmentDate, a.StaffMemberId });

        // StaffMember -> Appointments (Restrict delete)
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.StaffMember)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.StaffMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Patient -> VisitRecords (Restrict delete)
        modelBuilder.Entity<VisitRecord>()
            .HasOne(v => v.Patient)
            .WithMany(p => p.VisitRecords)
            .HasForeignKey(v => v.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // StaffMember -> VisitRecords (Restrict delete)
        modelBuilder.Entity<VisitRecord>()
            .HasOne(v => v.StaffMember)
            .WithMany(s => s.VisitRecords)
            .HasForeignKey(v => v.StaffMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Appointment -> VisitRecords (Restrict delete)
        modelBuilder.Entity<VisitRecord>()
            .HasOne(v => v.Appointment)
            .WithMany(a => a.VisitRecords)
            .HasForeignKey(v => v.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Appointment -> Notifications
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Appointment)
            .WithMany(a => a.Notifications)
            .HasForeignKey(n => n.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Appointment -> PredictionResults
        modelBuilder.Entity<PredictionResult>()
            .HasOne(pr => pr.Appointment)
            .WithMany(a => a.PredictionResults)
            .HasForeignKey(pr => pr.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.NoShowProbability)
            .HasPrecision(5, 4);

        modelBuilder.Entity<PredictionResult>()
            .Property(p => p.ProbabilityScore)
            .HasPrecision(5, 4);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.EntityName, a.CreatedAt });

        modelBuilder.Entity<ClinicSettings>()
            .HasIndex(s => s.ClinicName);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var baseEntries = ChangeTracker.Entries<Models.Entities.BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in baseEntries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;
        }

        // AppUser inherits IdentityUser<Guid>, not BaseEntity — handle timestamps separately
        var userEntries = ChangeTracker.Entries<AppUser>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in userEntries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;
        }
    }
}
