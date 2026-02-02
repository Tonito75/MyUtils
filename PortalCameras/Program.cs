using BlazorApp.Models;
using BlazorPortalCamera.Components;
using Common.Discord;
using Common.Pingg;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Authentification par cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                // Pour les routes YARP, retourner 401 au lieu de rediriger
                if (context.Request.Path.StartsWithSegments("/asnieres") ||
                    context.Request.Path.StartsWithSegments("/villy"))
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuth", policy => policy.RequireAuthenticatedUser());
});

// Ajouter YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// MudBlazor
builder.Services.AddMudServices();

// Configuration des cameras
builder.Services.Configure<List<CameraConfig>>(builder.Configuration.GetSection("Cameras"));
builder.Services.AddScoped<PingService>();
builder.Services.AddScoped<IDiscordWebHookService, DiscordWebHookService>();

builder.Services.Configure<DiscordWebHookServiceOptions>(options =>
{
    options.WebHookUrl = builder.Configuration["WebHookUrl"];
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// Static assets AVANT tout le reste - court-circuite le pipeline
// 1. Fichiers dans Components/wwwroot (app.css, bootstrap, etc.)
var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "Components", "wwwroot");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath)
});
// 2. Fichiers Blazor générés (_framework, styles.css)
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Désactivé temporairement pour debug
// app.UseStatusCodePagesWithReExecute("/not-found");

// Endpoint de login
app.MapPost("/api/login", async (HttpContext context, IConfiguration config) =>
{
    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    var validUser = config["Authentication:Username"];
    var validPass = config["Authentication:Password"];

    if (username == validUser && password == validPass)
    {
        var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username) };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        context.Response.Redirect("/");
        return;
    }

    context.Response.Redirect("/login?error=1");
}).AllowAnonymous();

// Endpoint de logout
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Redirect("/login");
}).RequireAuthorization();

app.MapReverseProxy().RequireAuthorization("RequireAuth");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
