using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TmsApi.Application.DTOs;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Tags("Authentication")]
public class AuthController(IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequestDto request)
    {
        // Demo credential check
        if (request.Email != "admin@tms.com" || request.Password != "Admin123!")
        {
            return Unauthorized(new { detail = "Invalid email or password." });
        }

        var jwtSettings = config.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
        var expiresAt = DateTime.UtcNow.AddHours(2);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, request.Email),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Append HttpOnly authentication cookie — JavaScript CANNOT read this token
        Response.Cookies.Append("tms_auth", tokenString, new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(), // HTTPS in production; HTTP in local development
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt
        });

        return Ok(new UserProfileDto("System Admin", "Admin"));
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        // Inspect cookie attached automatically by the browser on requests
        if (Request.Cookies.TryGetValue("tms_auth", out _))
        {
            return Ok(new UserProfileDto("System Admin", "Admin"));
        }

        return Unauthorized(new { detail = "Session expired or missing authentication cookie." });
    }
}