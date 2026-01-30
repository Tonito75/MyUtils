using Serilog;
using WindowsServiceUtils;
using WindowsServiceUtils.AppSettings;
using WindowsServiceUtils.Services.BackupLogFiles;
using WindowsServiceUtils.Services.IO;

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .UseSerilog((context, services, configuration) => configuration.ReadFrom.Configuration(context.Configuration))
    .ConfigureServices((hostContext, services) =>
    {
        services.Configure<FileSavingConfig>(hostContext.Configuration);
        services.AddHostedService<Worker>();
        services.AddSingleton<IIOService, IOService>();
        services.AddSingleton<IBackupFilesService, BackupFilesService>();
    });

Environment.SetEnvironmentVariable("appname", "TonitosWindowsService");

builder.Build().Run();