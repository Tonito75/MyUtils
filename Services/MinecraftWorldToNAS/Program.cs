using MinecraftWorldToNAS;
using Common.Date;
using Common.FTP;
using Common.Hosting.Extensions;
using Common.IO;
using Common.Logger;

var builder = Host.CreateApplicationBuilder(args);

builder
    .UseSerilogWithFileRotation("MinecraftWorldToNAS");

builder.Services.AddWindowsService();
builder.Services.AddHostedService<Worker>()
    .AddSingleton(builder.Configuration.GetSection("Settings").Get<Settings>())
    .AddSingleton<IDateService, DateService>()
    .AddSingleton<IFTPService, FTPService>()
    .AddSingleton<ILogService, WorkerLogService>()
    .AddSingleton<IIOService, IOService>();

var host = builder.Build();
host.Run();
