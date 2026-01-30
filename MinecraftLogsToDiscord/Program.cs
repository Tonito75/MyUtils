using MinecraftLogsToDiscord;
using Common.Discord;
using Common.FileWatcher;
using Common.Hosting.Extensions;
using Common.Logger;

var builder = Host.CreateApplicationBuilder(args);

builder
    .UseSerilogWithFileRotation("MinecraftLogsToDiscord");

builder.Services.AddWindowsService();
builder.Services
    .AddHostedService<Worker>()
    .AddSingleton<ILogService, WorkerLogService>()
    .AddSingleton<IDiscordWebHookService, DiscordWebHookService>()
    .AddSingleton<IFileWatcherService,PollingFileWatcherService>()
    .AddSingleton(builder.Configuration.GetSection("Settings").Get<Settings>());

var host = builder.Build();
host.Run();
