using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace PortalCameras.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/login", async (HttpContext ctx, IConfiguration config) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var password = form["password"].ToString();
            var validPassword = config["Authentication:Password"] ?? string.Empty;

            if (password != validPassword)
                return Results.Unauthorized();

            var claims = new[] { new Claim(ClaimTypes.Name, "user") };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return Results.Ok(new { success = true });
        }).AllowAnonymous();

        app.MapPost("/api/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok(new { success = true });
        }).RequireAuthorization();

        app.MapGet("/api/me", (HttpContext ctx) =>
        {
            var isAuth = ctx.User.Identity?.IsAuthenticated ?? false;
            return Results.Ok(new { isAuthenticated = isAuth });
        }).AllowAnonymous();
    }
}
