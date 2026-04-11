using ClinicManagementSystem.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ClinicManagementSystem.API.Auth;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(AppUser user, IList<string> roles)
    {
        var jwtSection = _config.GetSection("Authentication:Jwt");
        if (!jwtSection.Exists())
        {
            jwtSection = _config.GetSection("Jwt");
        }

        const string testingJwtKey = "integration-test-jwt-signing-key-32-characters-minimum";
        const string testingJwtIssuer = "ClinicManagementSystem";
        const string testingJwtAudience = "ClinicManagementSystemAPI";

        var jwtKey = jwtSection["Key"]
            ?? _config["JWT_KEY"]
            ?? Environment.GetEnvironmentVariable("JWT_KEY");

        var isTesting = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
            "Testing",
            StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Testing",
                StringComparison.OrdinalIgnoreCase);

        if (isTesting)
        {
            jwtKey = testingJwtKey;
        }

        if (string.IsNullOrWhiteSpace(jwtKey) && isTesting)
        {
            jwtKey = testingJwtKey;
        }

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured for token generation.");
        }

        var issuer = isTesting
            ? testingJwtIssuer
            : jwtSection["Issuer"]
                ?? _config["JWT_ISSUER"]
                ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? "ClinicManagementSystem";
        var audience = isTesting
            ? testingJwtAudience
            : jwtSection["Audience"]
                ?? _config["JWT_AUDIENCE"]
                ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? "ClinicManagementSystemAPI";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = jwtSection.GetValue<int>("ExpiryMinutes", 480);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("role_enum", user.Role.ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
