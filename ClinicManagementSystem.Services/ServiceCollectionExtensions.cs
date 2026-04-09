using ClinicManagementSystem.Services.Implementations;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManagementSystem.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClinicServices(this IServiceCollection services)
    {
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IVisitRecordService, VisitRecordService>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<ISmsSender, LoggingSmsSender>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPredictionService, PredictionService>();
        services.AddScoped<IPredictionResultService, PredictionResultService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IClinicSettingsService, ClinicSettingsService>();
        return services;
    }
}
