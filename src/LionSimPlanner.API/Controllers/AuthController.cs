using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Data.Common;
using BCrypt.Net;
using LionSimPlanner.Personnel.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LionSimPlanner.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly PersonnelDbContext _personnel;

    public AuthController(IConfiguration configuration, PersonnelDbContext personnel)
    {
        _configuration = configuration;
        _personnel = personnel;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("password");

        await using var conn = _personnel.Database.GetDbConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT id, password_hash, role, name, employee_code
FROM (
    SELECT
        p.pilot_id AS id,
        @default_password_hash AS password_hash,
        'Pilot' AS role,
        p.full_name AS name,
        p.employee_code,
        lower(p.corporate_email) AS email
    FROM hr.pilots p

    UNION ALL

    SELECT
        i.instructor_id AS id,
        @default_password_hash AS password_hash,
        'Instructor' AS role,
        i.full_name AS name,
        i.employee_code,
        lower(i.corporate_email) AS email
    FROM hr.instructors i

    UNION ALL

    SELECT
        e.engineer_id AS id,
        @default_password_hash AS password_hash,
        'Engineer' AS role,
        e.full_name AS name,
        e.employee_code,
        lower('engineer' || row_number() OVER (ORDER BY e.employee_code) || '@lionair.co.id') AS email
    FROM maint.engineers e

    UNION ALL

    SELECT
        '11111111-1111-1111-1111-111111111111'::uuid AS id,
        @default_password_hash AS password_hash,
        'Admin' AS role,
        'Administrator' AS name,
        'LGA-ADM-001' AS employee_code,
        'admin@lionair.co.id' AS email
) u
WHERE u.email = @email
LIMIT 1";
        var p = cmd.CreateParameter();
        p.ParameterName = "@email";
        p.Value = email;
        cmd.Parameters.Add(p);
        var pHash = cmd.CreateParameter();
        pHash.ParameterName = "@default_password_hash";
        pHash.Value = defaultPasswordHash;
        cmd.Parameters.Add(pHash);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return Unauthorized(new { message = "Invalid email or password." });

        var id = reader.GetGuid(0);
        var passwordHash = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        var role = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
        var name = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
        var employeeCode = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);

        var verified = BCrypt.Net.BCrypt.Verify(request.Password, passwordHash);
        if (!verified)
            return Unauthorized(new { message = "Invalid email or password." });

        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("employee_code", employeeCode),
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse(tokenString, id.ToString(), name, email, employeeCode, role));
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