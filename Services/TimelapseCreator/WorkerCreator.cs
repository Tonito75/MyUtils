using TimelapseCreator.Configuration;
using Common.Date;
using Common.Discord;
using Common.FTP;
using Common.Logger;
using Common.OpenCV.RTSP;
using Xabe.FFmpeg;
using Common.IO;

namespace TimelapseCreator
{
    public class WorkerCreator : BackgroundService
    {
        private readonly ILogService _logger;
        private readonly Settings _settings;
        private readonly IFTPService _ftpService;
        private readonly ITimeLapseBuilder _timeLapseBuilder;
        private readonly IDiscordWebHookService _discordService;
        private readonly IDateService _dateService;
        private readonly IIOService _ioService;

        private readonly string _tempFolderImages = "temp_images";
        private readonly string _tempFolderTimelapse = "temp_timelapse";

        public WorkerCreator(ILogService logger, Settings settings, IFTPService ftpService, ITimeLapseBuilder timeLapseBuilder, IDiscordWebHookService discordService, IDateService dateService, IIOService ioservoce)
        {
            _logger = logger;
            _settings = settings;
            _ftpService = ftpService;
            _timeLapseBuilder = timeLapseBuilder;
            _discordService = discordService;
            _dateService = dateService;
            _ioService = ioservoce;

            FFmpeg.SetExecutablesPath(Path.Combine(AppContext.BaseDirectory, "ffmpeg"));

            _logger.Log("Démarrage du worker Creator.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var hourToRun = 1;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.Now.Hour == hourToRun)
                {
                    foreach (var config in _settings.TimelapseConfiguration)
                    {
                        _logger.Log("Generating timelapse...");
                        await _discordService.SendAsync(config.WebHookUrl, "Génération du timelapse en cours...");
                        await GenerateTimeLapse(config);
                    }
                }

                await Task.Delay(1000 * 60 * 60, stoppingToken);
            }
        }

        private async Task GenerateTimeLapse(TimelapseConfiguration timelapseConfiguration)
        {
            var tempFolderImages = Path.Combine(Path.GetTempPath(), _tempFolderImages);
            Directory.CreateDirectory(tempFolderImages);
            _ioService.CleanDirectory(tempFolderImages);

            var tempFolderTimelapse = Path.Combine(Path.GetTempPath(), _tempFolderTimelapse);
            Directory.CreateDirectory(tempFolderTimelapse);
            _ioService.CleanDirectory(tempFolderTimelapse);

            _ftpService.Init(timelapseConfiguration.FtpConfiguration);

            var remoteFolder = $"{timelapseConfiguration.FtpConfiguration.Folder}/{_dateService.GetCurrentDateForFolderYesterdayYYYYMMDD()}";

            var (success, error) = await _ftpService.DownloadFilesFromFtpToLocalFolder(remoteFolder, tempFolderImages);
            if (!success)
            {
                await HandleError($"Une erreur est survenue lors du téléchargement des photos du timelapse : {error}", timelapseConfiguration);
                return;
            }

            var tempFilePath = $"{tempFolderTimelapse}/timelapse.mp4";

            (success, error) = await _timeLapseBuilder.CreateTimelapse($"{tempFolderImages}/{remoteFolder}", tempFilePath, 30, "jpg",0);

            if (!success)
            {
                await HandleError($"Une erreur est survenue lors de la création technique du timelapse : {error}", timelapseConfiguration);
                return;
            }

            (success, error) = await SplitVideoByTargetSize(tempFilePath, tempFolderTimelapse, _settings.SizeOfTimelapseInMb);
            if (!success)
            {
                await HandleError($"Une erreur est survenue lors du split du timelapse : {error}", timelapseConfiguration);
                return;
            }

            foreach(var file in Directory.EnumerateFiles(tempFolderTimelapse))
            {
                if (file.Contains("timelapse.mp4"))
                {
                    continue;
                }
                (success, error) = await _ftpService.Send(file, remoteFolder);
                if (success)
                {
                    var message = $"Le timelapse {file} a été généré et envoyé par FTP dans le dossier {remoteFolder}";
                    _logger.Log(message);
                    await _discordService.SendOkAsync(message);

                    await CleanYesterdayFolder(remoteFolder);
                }
                else
                {
                    await HandleError($"Une erreur est survenue lors de l'envoi du timelapse {file} par ftp : {error}", timelapseConfiguration);
                }
            }
        }

        private async Task CleanYesterdayFolder(string remoteFolder)
        {
            var (success, error) = await _ftpService.CleanFolder(remoteFolder, ".jpg");

            if (!success)
            {
                _logger.Error($"Impossible de supprimer les photos du répertoire {remoteFolder} : {error}");
            }
        }

        private async Task HandleError(string errorMessage, TimelapseConfiguration timelapseConfiguration)
        {
            _logger.Error(errorMessage);
            await _discordService.SendErrorAsync(errorMessage);
        }

        private static async Task<(bool,string)> SplitVideoByTargetSize(string inputPath, string outputDirectory, long targetSizeMB)
        {
            try
            {
                var fileInfo = new FileInfo(inputPath);
                var mediaInfo = await FFmpeg.GetMediaInfo(inputPath);

                // Estimation du nombre de segments nécessaires
                long targetSizeBytes = targetSizeMB * 1024 * 1024;
                int estimatedSegments = (int)Math.Ceiling((double)fileInfo.Length / targetSizeBytes);

                var segmentDuration = TimeSpan.FromSeconds(mediaInfo.Duration.TotalSeconds / estimatedSegments);

                return await SplitVideoByDuration(inputPath, outputDirectory, segmentDuration);
            }
            catch(Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private static async Task<(bool,string)> SplitVideoByDuration(string inputPath, string outputDirectory, TimeSpan segmentDuration)
        {
            try
            {
                var mediaInfo = await FFmpeg.GetMediaInfo(inputPath);
                var totalDuration = mediaInfo.Duration;

                int segmentCount = 0;
                TimeSpan currentTime = TimeSpan.Zero;

                while (currentTime < totalDuration)
                {
                    var outputPath = Path.Combine(outputDirectory, $"segment_{segmentCount:D3}.mp4");

                    var conversion = FFmpeg.Conversions.New()
                        .AddParameter($"-i \"{inputPath}\"")
                        .AddParameter($"-ss {currentTime}")
                        .AddParameter($"-t {segmentDuration}")
                        .AddParameter("-c copy") // Copie sans réencodage (plus rapide)
                        .AddParameter($"\"{outputPath}\"");

                    await conversion.Start();

                    currentTime = currentTime.Add(segmentDuration);
                    segmentCount++;
                }
                return (true, string.Empty);
            }
            catch(Exception ex)
            {
                return (false, ex.Message);
            }
            
        }
    }
}
