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
using System.Threading.Tasks;
using System.Text.Json;

namespace CameraWatcher;

public class WorkerWatcher : DiscordWorker<CameraWatcherOptions>
{
    private readonly IFTPService _ftpService;
    private readonly IDateService _dateService;
    private readonly IIOService _ioService;

    private bool _firstExec = true;
    private string _lastSentFileName = string.Empty;
    private DateTime? _lastSentFileCreationDate = null;
    private readonly string _apiMeteoUrl = string.Empty;

    private readonly int _apiMeteoErrorProtections = 15;
    private int _currentApiMeteoErrors = 0;

    public WorkerWatcher(
        IOptions<CameraWatcherOptions> options,
        IHostApplicationLifetime lifetime,
        ILogService logService,
        IDiscordWebHookService discordService,
        IFTPService ftpService,
        IDateService dateService,
        IIOService ioService,
        IConfiguration configuration)
        : base(options, lifetime, logService, discordService)
    {
        _ftpService = ftpService;
        _dateService = dateService;
        _ioService = ioService;

        _apiMeteoUrl = configuration["ApiMeteoUrl"];
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

                // Last file in the ftp list is older than the last sent file ; nothing has to be done. 
                if(_lastSentFileCreationDate != null && lastFtpFile.Created <= _lastSentFileCreationDate.Value)
                {
                    return;
                }

                _lastSentFileCreationDate = lastFtpFile.Created;

                (success, error, (var isDay, var isRaining)) = await GetMeteoInfos(watcherConfiguration.ApiMeteoUrl);
                if (!success)
                {
                    LogService.Error(error);

                    _currentApiMeteoErrors++;

                    if (_currentApiMeteoErrors == 1)
                    {
                        await DiscordService.SendAsync($"Erreur lors de la récupération des informations météo : {error}. Le fichier sera envoyé.");
                    }
                    else if (_currentApiMeteoErrors == _apiMeteoErrorProtections)
                    {
                        _currentApiMeteoErrors = 0;
                    }
                }
                else
                {
                    // Nuit + pluie : on filtre !
                    if(!isDay && isRaining)
                    {
                        LogService.Log($"Rain and night : image {lastFtpFile.FullName} to be deleted.");
                        await DiscordService.SendAsync($"{Emojis.Rain} Nuit + pluie : fichier {lastFtpFile.FullName} ignoré + supprimé.");

                        (success,error) = await _ftpService.DeleteFile(lastFtpFile.FullName);
                        if (success)
                        {
                            _lastSentFileName = lastFtpFile.Name;
                            return;
                        }
                        else
                        {
                            LogService.Error($"Could not delete file {lastFtpFile.FullName} because {error}");
                            await DiscordService.SendAsync($"{Emojis.RedCross} Impossible de supprimer le fichier car {error}.");
                        }
                    }

                    LogService.Log($"Rain : {isRaining}, Day : {isDay} : handling file normally");
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

    private async Task<(bool Error,string ErrorMessage, (bool IsDay, bool IsRaining) Result)> GetMeteoInfos(string apiUrl)
    {
        try
        {
            using var client = new HttpClient();

            var response = await client.GetStringAsync(apiUrl);
            var json = JsonDocument.Parse(response);

            var isDay = json.RootElement.GetProperty("isDay").GetBoolean();
            var isRaining = json.RootElement.GetProperty("isRaining").GetBoolean();

            return (true, string.Empty,(isDay, isRaining));
        }
        catch (Exception ex)
        {
            return (false, ex.Message, (false, false));
        }
    }
}
