using BlazorApp.Models;
using BlazorPortalCamera.Auth;
using BlazorPortalCamera.Components;
using Common.Discord;
using Common.Pingg;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Ajouter l'authentification simple
builder.Services.AddAuthentication("BasicAuth").AddScheme<AuthenticationSchemeOptions, YarpAuthHandler>("BasicAuth",null);
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

// Configuration des cameras
builder.Services.Configure<List<CameraConfig>>(builder.Configuration.GetSection("Cameras"));
builder.Services.AddScoped<PingService>();
builder.Services.AddScoped<IDiscordWebHookService, DiscordWebHookService>();

builder.Services.Configure<DiscordWebHookServiceOptions>(options =>
{
    options.WebHookUrl = builder.Configuration["WebHookUrl"];
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
