using System.Security.Claims;
using ClinicManagementSystem.API.Auth;
using ClinicManagementSystem.API.Extensions;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        TokenService tokenService,
        IAuditLogService auditLogService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Exchange credentials for a JWT bearer token.
    /// Rate-limited to 10 requests/minute per IP to slow brute-force attacks.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting("auth-fixed-window")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Login failed for {Email}: user not found or inactive", request.Email);
            await WriteAuditAsync("Authentication", "LoginFailed", null,
                $"Login attempt failed for email '{request.Email}' (user not found or inactive)", "Failure");
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account locked out for {Email}", request.Email);
            await WriteAuditAsync("Authentication", "AccountLockedOut", user.Id,
                $"Account locked out after too many failed attempts for '{request.Email}'", "Failure");
            return StatusCode(423, new { message = "Account is temporarily locked. Try again later." });
        }

        if (!result.Succeeded)
        {
            _logger.LogWarning("Login failed for {Email}: invalid password", request.Email);
            await WriteAuditAsync("Authentication", "LoginFailed", user.Id,
                $"Invalid password for '{request.Email}'", "Failure");
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);

        _logger.LogInformation("User {Email} logged in successfully", user.Email);
        await WriteAuditAsync("Authentication", "LoginSuccess", user.Id,
            $"User '{user.Email}' authenticated successfully");

        return Ok(new
        {
            token,
            expiresIn = 480 * 60,
            userId = user.Id,
            email = user.Email,
            fullName = user.FullName,
            roles
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdClaim, out var parsed) ? parsed : null;

        await WriteAuditAsync("Authentication", "Logout", userId, "JWT logout endpoint invoked by authenticated user");
        return Ok(new { message = "Logged out. Dispose the client token." });
    }

    private Task WriteAuditAsync(string entityName, string actionType, Guid? userId, string description, string outcome = "Success")
    {
        return _auditLogService.CreateAsync(new AuditLog
        {
            EntityName = entityName,
            ActionType = actionType,
            PerformedByUserId = userId,
            UserRole = User.FindFirstValue(ClaimTypes.Role),
            IpAddress = HttpContext.GetClientIpAddress(),
            HttpMethod = HttpContext.Request.Method,
            RequestPath = HttpContext.Request.Path.Value,
            Outcome = outcome,
            Description = description
        });
    }
}
