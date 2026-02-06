using CameraWatcher;
using CameraWatcher.Options;
using Common.Date;
using Common.FTP;
using Common.Hosting.Extensions;
using Common.IO;

var builder = Host.CreateApplicationBuilder(args);

builder
    .UseSerilogWithFileRotation("CameraWatcher");

builder.Services.AddWindowsService();

builder.Services.AddDiscordWorker<WorkerWatcher, CameraWatcherOptions>(
    builder.Configuration,
    workerSectionName: "Worker");

builder.Services
    .AddSingleton<IFTPService, FTPService>()
    .AddSingleton<IDateService, DateService>()
    .AddSingleton<IIOService, IOService>();

var host = builder.Build();
host.Run();
