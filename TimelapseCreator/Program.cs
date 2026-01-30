using TimelapseCreator;
using Common.Date;
using Common.Discord;
using Common.FTP;
using Common.Hosting.Extensions;
using Common.IO;
using Common.Logger;
using Common.OpenCV.RTSP;

var builder = Host.CreateApplicationBuilder(args);

builder
    .UseSerilogWithFileRotation("TimelapseCreator");

builder.Services.AddWindowsService();
builder.Services.AddHostedService<WorkerCreator>();
builder.Services.AddHostedService<WorkerScreener>();

builder.Services.Configure<DiscordWebHookServiceOptions>(options => options.WebHookUrl = builder.Configuration["WebHookUrl"]);

builder.Services
    .AddSingleton(builder.Configuration.GetSection("Settings").Get<Settings>())
    .AddSingleton<ILogService, WorkerLogService>()
    .AddSingleton<IDiscordWebHookService, DiscordWebHookService>()
    .AddSingleton<IRTSPService, RTSPService>()
    .AddSingleton<ITimeLapseBuilder, TimeLapseBuilder>()
    .AddSingleton<IFTPService, FTPService>()
    .AddSingleton<IDateService, DateService>()
    .AddSingleton<IIOService, IOService>();

var host = builder.Build();
host.Run();
