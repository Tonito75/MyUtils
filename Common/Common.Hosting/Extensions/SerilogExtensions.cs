using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Common.Hosting.Extensions;

public static class SerilogExtensions
{
    /// <summary>
    /// Configure Serilog avec logs console et fichier avec rotation.
    /// Les fichiers sont créés dans le dossier "logs" du répertoire courant.
    /// </summary>
    /// <param name="builder">Host application builder</param>
    /// <param name="applicationName">Nom de l'application pour le préfixe des fichiers de log</param>
    /// <param name="logDirectory">Dossier des logs (par défaut: "logs")</param>
    /// <param name="fileSizeLimitBytes">Taille max d'un fichier avant rotation (par défaut: 10MB)</param>
    /// <param name="retainedFileCountLimit">Nombre de fichiers à conserver (par défaut: 31)</param>
    /// <returns>Le builder pour chaînage</returns>
    public static IHostApplicationBuilder UseSerilogWithFileRotation(
        this IHostApplicationBuilder builder,
        string applicationName,
        string logDirectory = "logs",
        long fileSizeLimitBytes = 10 * 1024 * 1024,
        int retainedFileCountLimit = 31)
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, logDirectory, $"{applicationName}-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: fileSizeLimitBytes,
                retainedFileCountLimit: retainedFileCountLimit,
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Services.AddSerilog();

        return builder;
    }

    /// <summary>
    /// Configure Serilog pour le pattern Host.CreateDefaultBuilder (ancien style).
    /// </summary>
    public static IHostBuilder UseSerilogWithFileRotation(
        this IHostBuilder builder,
        string applicationName,
        string logDirectory = "logs",
        long fileSizeLimitBytes = 10 * 1024 * 1024,
        int retainedFileCountLimit = 31)
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, logDirectory, $"{applicationName}-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: fileSizeLimitBytes,
                retainedFileCountLimit: retainedFileCountLimit,
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        return builder.UseSerilog();
    }
}
