using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Services.Implementations;

public class AppUserService : IAppUserService
{
    private readonly ClinicDbContext _db;
    private readonly ILogger<AppUserService> _logger;

    public AppUserService(ClinicDbContext db, ILogger<AppUserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<AppUser>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all app users");
        return await _db.AppUsers.AsNoTracking().OrderBy(u => u.LastName).ToListAsync();
    }

    public async Task<AppUser?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching app user {UserId}", id);
        return await _db.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<AppUser> CreateAsync(AppUser user)
    {
        _logger.LogInformation("Creating app user {Email}", user.Email);
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<AppUser> UpdateAsync(AppUser user)
    {
        _logger.LogInformation("Updating app user {UserId}", user.Id);
        _db.AppUsers.Update(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user is null)
        {
            _logger.LogWarning("App user {UserId} not found for deletion", id);
            return false;
        }

        user.IsDeleted = true;
        await _db.SaveChangesAsync();
        return true;
    }
}
