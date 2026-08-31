using ClinicManagementSystem.Models.Entities;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IStaffService
{
    Task<IEnumerable<StaffMember>> GetAllAsync();
    Task<(IEnumerable<StaffMember> Items, int TotalCount)> QueryAsync(
        int page,
        int pageSize,
        string? sortBy,
        bool sortDescending);
    Task<StaffMember?> GetByIdAsync(Guid id);
    Task<StaffMember> CreateAsync(StaffMember staff);
    Task<StaffMember> UpdateAsync(StaffMember staff);
    Task<bool> DeleteAsync(Guid id);
}
