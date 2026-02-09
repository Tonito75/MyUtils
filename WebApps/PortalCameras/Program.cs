using BlazorApp.Models;
using BlazorPortalCamera.Components;
using BlazorPortalCamera.Services;
using Common.Date;
using Common.Discord;
using Common.IO;
using Common.Pingg;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using Serilog;
using System;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Démarrage de l'application");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

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
builder.Services.AddScoped<DateService>();
builder.Services.AddScoped<IOService>();
builder.Services.AddScoped<DetectThingsService>();

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

// Static assets (tout est dans wwwroot après publication)
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
    var password = form["password"].ToString().ToLower();

    var validPass = config["Authentication:Password"];

    if (password == validPass)
    {
        var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "villy") };
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

// Endpoint pour servir les images du NAS
app.MapGet("/camera-history/{**path}", (HttpContext context, string path, IConfiguration config) =>
{
    Log.Information("camera-history endpoint hit - Path: {Path}, Authenticated: {IsAuth}",
        path, context.User.Identity?.IsAuthenticated);

    if (!context.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var baseFolder = config["BaseHistoryFolder"];
    if (string.IsNullOrEmpty(baseFolder))
    {
        Log.Information($"Base folder was empty.");
        return Results.NotFound();
    }

    Log.Information($"Base folder is {baseFolder}");

    var fullPath = Path.Combine(baseFolder, path);

    if (!File.Exists(fullPath))
    {
        Log.Information($"The full path of image {fullPath} does not exists.");
        return Results.NotFound();
    }

    Log.Information($"The full path of image {fullPath} does exists !");

    var contentType = Path.GetExtension(fullPath).ToLower() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };

    context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
    return Results.File(fullPath, contentType);
}).RequireAuthorization();

app.MapReverseProxy().RequireAuthorization("RequireAuth");

app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "L'application s'est arrêtée de manière inattendue");
}
finally
{
    Log.CloseAndFlush();
}
