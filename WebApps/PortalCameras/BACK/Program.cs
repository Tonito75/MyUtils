using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using PortalCameras.Endpoints;
using PortalCameras.Models;
using PortalCameras.Services;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Démarrage de PortalCameras API");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // CORS
    var allowedOriginsRaw = builder.Configuration["AllowedOrigins"] ?? "http://localhost:5173";
    var allowedOrigins = allowedOriginsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());
    });

    // Auth cookies
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/api/login";
            options.ExpireTimeSpan = TimeSpan.FromHours(24);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
        options.AddPolicy("RequireAuth", policy => policy.RequireAuthenticatedUser()));

    // YARP
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    // OpenAPI + Scalar
    builder.Services.AddOpenApi();

    // Services
    builder.Services.Configure<List<CameraConfig>>(builder.Configuration.GetSection("Cameras"));
    builder.Services.AddScoped<PingService>();
    builder.Services.AddScoped<IOService>();
    builder.Services.AddScoped<DateService>();
    builder.Services.AddScoped<DiscordService>();
    builder.Services.AddScoped<DetectThingsService>();

    var app = builder.Build();

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    // Scalar
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Endpoints
    app.MapAuthEndpoints();
    app.MapCameraEndpoints();
    app.MapImageEndpoints();

    // YARP
    app.MapReverseProxy().RequireAuthorization("RequireAuth");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application crash");
}
finally
{
    Log.CloseAndFlush();
}
