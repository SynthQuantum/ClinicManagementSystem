using ClinicManagementSystem.Models.Entities;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IStaffService
{
    Task<IEnumerable<StaffMember>> GetAllAsync();
    Task<StaffMember?> GetByIdAsync(Guid id);
    Task<StaffMember> CreateAsync(StaffMember staff);
    Task<StaffMember> UpdateAsync(StaffMember staff);
    Task<bool> DeleteAsync(Guid id);
}
