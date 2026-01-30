using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using CameraWatcher.Configuration;
using CameraWatcher.Options;
using Common.Classes;
using Common.Date;
using Common.Discord;
using Common.FTP;
using Common.Hosting.Worker;
using Common.IO;
using Common.Logger;

namespace CameraWatcher;

public class WorkerWatcher : DiscordWorker<CameraWatcherOptions>
{
    private readonly IFTPService _ftpService;
    private readonly IDateService _dateService;
    private readonly IIOService _ioService;

    private bool _firstExec = true;
    private string _lastSentFileName = string.Empty;

    public WorkerWatcher(
        IOptions<CameraWatcherOptions> options,
        IHostApplicationLifetime lifetime,
        ILogService logService,
        IDiscordWebHookService discordService,
        IFTPService ftpService,
        IDateService dateService,
        IIOService ioService)
        : base(options, lifetime, logService, discordService)
    {
        _ftpService = ftpService;
        _dateService = dateService;
        _ioService = ioService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var watcherConfiguration in Options.WatcherConfigurations)
            {
                try
                {
                    await HandleWatcher(watcherConfiguration);
                }
                catch (Exception ex)
                {
                    LogService.Error($"Une erreur fatale est survenue lors du check de nouvelles photos : {ex.Message}");
                }
            }
            await Task.Delay(DelayInSeconds * 1000, stoppingToken);
        }
    }

    private async Task HandleWatcher(WatcherConfiguration watcherConfiguration)
    {
        _ftpService.Init(watcherConfiguration.FtpConfiguration);

        var ftpFolder = $"{watcherConfiguration.FtpConfiguration.Folder}/{_dateService.GetCurrentDateForFolderYYYYMMDD()}";

        var (success, error, result) = await _ftpService.ListFiles(ftpFolder);

        if (!success)
        {
            LogService.Error($"Impossible de lister les fichiers du Ftp : {error}");
            return;
        }

        if (result != null)
        {
            var lastFtpFile = result
                .Where(f => f.Name.Split(".").LastOrDefault() == "jpg")
                .OrderBy(f => f.Created)
                .LastOrDefault();

            if (lastFtpFile != null && lastFtpFile.Name != _lastSentFileName)
            {
                if (_firstExec)
                {
                    _lastSentFileName = lastFtpFile.Name;
                    _firstExec = false;
                    return;
                }

                var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempFolder);

                LogService.Log($"Téléchargement du fichier distant {lastFtpFile.FullName}");

                (success, error) = await _ftpService.DownloadFile(lastFtpFile.FullName, $"{tempFolder}/{lastFtpFile.Name}");
                if (!success)
                {
                    LogService.Error($"Impossible de télécharger le fichier du ftp : {error}");
                    return;
                }

                var tempFilePath = $"{tempFolder}/{lastFtpFile.Name}";

                if (!File.Exists(tempFilePath))
                {
                    LogService.Error($"Impossible de trouver le fichier temporaire au chemin {tempFilePath}");
                    return;
                }

                (success, error) = await DiscordService.SendAsync(tempFilePath, $"{Emojis.Movement} Mouvement détecté ({Emojis.Folder} {lastFtpFile.FullName})");
                if (!success)
                {
                    LogService.Error($"Impossible d'envoyer la pièce jointe à discord : {error}");
                    return;
                }

                _ioService.CleanDirectory(tempFolder);
                _lastSentFileName = lastFtpFile.Name;

                LogService.Log($"Detected new file on ftp {watcherConfiguration.FtpConfiguration} and sent to discord successfully.");
            }
        }
    }
}
