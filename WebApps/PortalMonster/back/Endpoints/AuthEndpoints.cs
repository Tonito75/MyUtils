using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MonsterHub.Api.Dtos.Auth;
using MonsterHub.Api.Models;
using MonsterHub.Api.Settings;
using Microsoft.Extensions.Options;

namespace MonsterHub.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (
            RegisterRequest req,
            UserManager<AppUser> userManager,
            IOptions<JwtSettings> jwtOptions) =>
        {
            var user = new AppUser
            {
                UserName = req.Username,
                Email = req.Email,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded)
                return Results.BadRequest(result.Errors.Select(e => e.Description));

            return Results.Ok(new { token = GenerateToken(user, jwtOptions.Value) });
        });

        group.MapPost("/login", async (
            LoginRequest req,
            UserManager<AppUser> userManager,
            IOptions<JwtSettings> jwtOptions) =>
        {
            var user = await userManager.FindByNameAsync(req.Username);
            if (user == null || !await userManager.CheckPasswordAsync(user, req.Password))
                return Results.Unauthorized();

            return Results.Ok(new { token = GenerateToken(user, jwtOptions.Value) });
        });
    }

    private static string GenerateToken(AppUser user, JwtSettings settings)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!)
        };
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(settings.ExpiresInDays),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
