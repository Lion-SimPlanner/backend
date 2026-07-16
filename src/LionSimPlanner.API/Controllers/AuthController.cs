using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LionSimPlanner.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    private static readonly Dictionary<string, (string Password, string Role, string Name, string EmployeeCode, string UserId)> Users = new()
    {
        ["admin@lionair.co.id"]      = ("admin123",       "Admin",      "J. Davidson",       "EMP-001", "00000000-0000-0000-0000-000000000001"),
        ["pilot@lionair.co.id"]      = ("pilot123",       "Pilot",      "Capt. R. Holt",     "EMP-102", "00000000-0000-0000-0000-000000000002"),
        ["instructor@lionair.co.id"] = ("instructor123",  "Instructor", "Instr. I. Nakamura","EMP-203", "00000000-0000-0000-0000-000000000003"),
        ["engineer@lionair.co.id"]   = ("engineer123",    "Engineer",   "Eng. M. Kowalski",  "EMP-304", "00000000-0000-0000-0000-000000000004"),
    };

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!Users.TryGetValue(email, out var profile) || profile.Password != request.Password)
            return Unauthorized(new { message = "Invalid email or password." });

        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   profile.UserId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.NameIdentifier,     profile.UserId),
            new Claim(ClaimTypes.Name,               profile.Name),
            new Claim(ClaimTypes.Email,              email),
            new Claim(ClaimTypes.Role,               profile.Role),
            new Claim("employee_code",               profile.EmployeeCode),
        };

        var token = new JwtSecurityToken(
            issuer:             configuration["Jwt:Issuer"],
            audience:           configuration["Jwt:Audience"],
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse(
            tokenString,
            profile.UserId,
            profile.Name,
            email,
            profile.EmployeeCode,
            profile.Role));
    }
}

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string Token,
    string UserId,
    string Name,
    string Email,
    string EmployeeCode,
    string Role);
