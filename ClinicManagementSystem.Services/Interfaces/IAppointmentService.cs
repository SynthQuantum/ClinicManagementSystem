using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IAppointmentService
{
    Task<IEnumerable<Appointment>> GetAllAsync();
    Task<IEnumerable<Appointment>> SearchAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? patientId = null,
        Guid? staffMemberId = null,
        AppointmentStatus? status = null,
        bool highRiskOnly = false);
    Task<IEnumerable<Appointment>> GetByDateAsync(DateTime date);
    Task<IEnumerable<Appointment>> GetByPatientAsync(Guid patientId);
    Task<IEnumerable<Appointment>> GetByStaffAsync(Guid staffMemberId);
    Task<Appointment?> GetByIdAsync(Guid id);
    Task<Appointment> CreateAsync(Appointment appointment);
    Task<Appointment> UpdateAsync(Appointment appointment);
    Task<bool> UpdateStatusAsync(Guid id, AppointmentStatus status);
    Task<bool> DeleteAsync(Guid id);
}
