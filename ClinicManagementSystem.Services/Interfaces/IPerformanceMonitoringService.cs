using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IPerformanceMonitoringService
{
    bool IsEnabled { get; }
    void CaptureSample(PerformanceSample sample);
    Task FlushPendingSamplesAsync(CancellationToken cancellationToken = default);
    Task<PerformanceSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task ResetSamplesAsync(CancellationToken cancellationToken = default);
}
