using System.Security.Claims;
using ClinicManagementSystem.API.Extensions;
using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

/// <summary>
/// Manages application user accounts. All mutations go through UserManager
/// to ensure Identity pipeline (password hashing, security stamps, etc.) is
/// never bypassed, preventing overposting and credential-injection attacks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AppUsersController : ControllerBase
{
    private readonly IAppUserService _service;
    private readonly IAuditLogService _auditLogService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AppUsersController> _logger;

    public AppUsersController(
        IAppUserService service,
        IAuditLogService auditLogService,
        UserManager<AppUser> userManager,
        ILogger<AppUsersController> logger)
    {
        _service = service;
        _auditLogService = auditLogService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppUser>>> GetAll()
    {
        _logger.LogInformation("API: fetching app users");
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppUser>> GetById(Guid id)
    {
        var user = await _service.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Creates a new user through the Identity pipeline (password is hashed, security
    /// stamp is generated). Accepts a DTO — raw AppUser is never bound from the request.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AppUser>> Create(CreateUserRequest request)
    {
        var newUser = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpperInvariant(),
            NormalizedUserName = request.Email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(newUser, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            _logger.LogWarning("Failed to create user {Email}: {Errors}", request.Email, string.Join(", ", errors));
            return BadRequest(new { errors });
        }

        // Assign the Identity role string that matches the enum name
        var roleName = request.Role.ToString();
        if (await _userManager.FindByEmailAsync(request.Email) is { } created)
        {
            await _userManager.AddToRoleAsync(created, roleName);
            await WriteAuditAsync("AppUser", "Created", created.Id, $"User account created for '{request.Email}'");
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        return StatusCode(500, new { message = "User created but could not be retrieved." });
    }

    /// <summary>
    /// Updates safe profile fields only. Password changes must use the dedicated
    /// change-password flow; this endpoint never touches PasswordHash or SecurityStamp.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AppUser>> Update(Guid id, UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        // Only update fields explicitly allowed in the DTO
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.UserName = request.Email;
        user.NormalizedEmail = request.Email.ToUpperInvariant();
        user.NormalizedUserName = request.Email.ToUpperInvariant();
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            _logger.LogWarning("Failed to update user {UserId}: {Errors}", id, string.Join(", ", errors));
            return BadRequest(new { errors });
        }

        await WriteAuditAsync("AppUser", "Updated", id, $"User account updated for '{request.Email}'");
        return Ok(user);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound();
        await WriteAuditAsync("AppUser", "Deleted", id, "User account soft-deleted");
        return NoContent();
    }

    private async Task WriteAuditAsync(string entityName, string actionType, Guid? entityId, string description)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        _ = Guid.TryParse(userIdValue, out var performedBy);

        await _auditLogService.CreateAsync(new AuditLog
        {
            EntityName = entityName,
            ActionType = actionType,
            EntityId = entityId,
            PerformedByUserId = performedBy == Guid.Empty ? null : performedBy,
            UserRole = User.FindFirstValue(ClaimTypes.Role),
            IpAddress = HttpContext.GetClientIpAddress(),
            HttpMethod = HttpContext.Request.Method,
            RequestPath = HttpContext.Request.Path.Value,
            Outcome = "Success",
            Description = description
        });
    }
}

