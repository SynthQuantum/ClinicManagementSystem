using ClinicManagementSystem.Models.Entities;

namespace ClinicManagementSystem.Services.Interfaces;

public interface IAppUserService
{
    Task<IEnumerable<AppUser>> GetAllAsync();
    Task<AppUser?> GetByIdAsync(Guid id);
    Task<AppUser> CreateAsync(AppUser user);
    Task<AppUser> UpdateAsync(AppUser user);
    Task<bool> DeleteAsync(Guid id);
}
