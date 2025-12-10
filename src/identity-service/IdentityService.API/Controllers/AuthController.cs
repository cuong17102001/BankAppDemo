using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using IdentityService.API.Data;
using IdentityService.API.Entities;
using IdentityService.API.Models;
using Microsoft.AspNetCore.Authorization;

namespace IdentityService.API.Controllers;

[ApiController]
[Route("")] // root-level minimal routes kept
public class AuthController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly JwtOptions _opts;
    private readonly SymmetricSecurityKey _key;

    public AuthController(IdentityDbContext db, JwtOptions opts)
    {
        _db = db; _opts = opts; _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        if (!await _db.Clients.AnyAsync())
        {
            _db.Clients.AddRange(
                new Client { ClientId = "account-service", ClientSecret = "secret-account", AllowedScopes = "account.read account.write", AllowedAudiences = "account-service" },
                new Client { ClientId = "transaction-service", ClientSecret = "secret-transaction", AllowedScopes = "transaction.read transaction.write", AllowedAudiences = "transaction-service" }
            );
        }
        if (!await _db.Users.AnyAsync())
        {
            _db.Users.AddRange(
                new User { Username = "admin", PasswordHash = "admin", Roles = "admin" },
                new User { Username = "staff", PasswordHash = "staff", Roles = "staff" },
                new User { Username = "customer", PasswordHash = "customer", Roles = "customer" }
            );
        }
        await _db.SaveChangesAsync();
        return Ok(new { seeded = true });
    }

    [HttpPost("token")]
    public async Task<IActionResult> ClientToken([FromForm] string client_id, [FromForm] string client_secret, [FromForm] string scope, [FromForm] string? audience)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.ClientId == client_id && c.ClientSecret == client_secret);
        if (client == null) return Unauthorized();
        var allowedScopes = client.AllowedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var requestedScopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (!requestedScopes.All(s => allowedScopes.Contains(s))) return BadRequest(new { error = "invalid_scope" });
        var allowedAud = client.AllowedAudiences.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var aud = string.IsNullOrWhiteSpace(audience) ? _opts.DefaultAudience : audience!;
        if (allowedAud.Length > 0 && !allowedAud.Contains(aud)) return BadRequest(new { error = "invalid_audience" });

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, client_id),
            new Claim("client_id", client_id),
            new Claim("scope", string.Join(' ', requestedScopes))
        };
        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: aud,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_opts.AccessTokenMinutes),
            signingCredentials: creds);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new { access_token = accessToken, token_type = "Bearer", expires_in = _opts.AccessTokenMinutes * 60, scope = string.Join(' ', requestedScopes), audience = aud });
    }

    [HttpPost("token/password")]
    public async Task<IActionResult> PasswordToken([FromForm] string username, [FromForm] string password, [FromForm] string scope, [FromForm] string? audience)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == password);
        if (user == null) return Unauthorized();
        var aud = string.IsNullOrWhiteSpace(audience) ? _opts.DefaultAudience : audience!;
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("scope", scope),
            new Claim(ClaimTypes.Role, user.Roles)
        };
        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: aud,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_opts.AccessTokenMinutes),
            signingCredentials: creds);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new { access_token = accessToken, token_type = "Bearer", expires_in = _opts.AccessTokenMinutes * 60, scope, audience = aud });
    }

    [HttpPost("introspect")]
    public IActionResult Introspect([FromForm] string token)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _opts.Issuer,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key
            }, out var validated);
            var jwt = (JwtSecurityToken)validated;
            return Ok(new { active = true, sub = jwt.Subject, scope = string.Join(' ', jwt.Claims.Where(c => c.Type == "scope").Select(c => c.Value)) });
        }
        catch
        {
            return Ok(new { active = false });
        }
    }

    [HttpGet("only-admin")]
    [Authorize(Policy = "role.admin")]
    public IActionResult OnlyAdmin()
    {
        return Ok("This is an admin-only endpoint.");
    }

    [HttpGet("admin-or-staff")]
    [Authorize(Roles = "admin,staff")]
    public IActionResult AdminOrStaff()
    {
        return Ok("This is an admin or staff-only endpoint.");
    }

    [HttpGet("all-roles")]
    [Authorize(Roles = "admin,staff,customer")]
    public IActionResult AllRoles()
    {
        return Ok("This is an admin, staff, or customer-only endpoint.");
    }
}
