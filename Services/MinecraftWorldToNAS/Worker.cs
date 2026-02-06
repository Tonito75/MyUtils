using System.IO.Compression;
using Common.Date;
using Common.FTP;
using Common.Logger;
using Common.IO;

namespace MinecraftWorldToNAS
{
    public class Worker : BackgroundService
    {
        private readonly ILogService _logService;
        private readonly Settings _settings;
        private readonly IDateService _dateService;
        private readonly IFTPService _ftpService;
        private readonly IIOService _ioService;

        private string _tempFolder = "minecraftworldtonas";

        public Worker(ILogService logService, Settings settings, IDateService dateService, IFTPService ftpService, IIOService iOService)
        {
            _logService = logService;
            _settings = settings;
            _dateService = dateService;
            _ftpService = ftpService;
            _ioService = iOService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Backup();

                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }

        private async Task Backup()
        {
            _logService.Log("Début de la backup...");

            try
            {
                // Create temp folder
                var tempFolder = Path.Combine(Path.GetTempPath(), _tempFolder);
                Directory.CreateDirectory(tempFolder);
                Directory.CreateDirectory($"{tempFolder}/world_unzip");
                Directory.CreateDirectory($"{tempFolder}/world_zipped");

                _ioService.CleanDirectory($"{tempFolder}/world_unzip");
                _ioService.CleanDirectory($"{tempFolder}/world_zipped");

                // Copy world to unzip dir
                await _ioService.CopyDirectory(_settings.WorldFolderPath, $"{tempFolder}/world_unzip", true);

                var outZip = $"{tempFolder}/world_zipped/{_dateService.GetCurrentDateForFile()}.zip";

                ZipFile.CreateFromDirectory($"{tempFolder}/world_unzip", outZip , CompressionLevel.Optimal, false);

                _ftpService.Init(_settings.FtpConfiguration);

                await _ftpService.Send(outZip,_settings.FtpConfiguration.Folder);

                _logService.Log("Sauvegarde du monde minecraft effectuée");
            }
            catch (Exception ex)
            {
                _logService.Error($"Erreur lors de la sauvegarde du monde minecraft : {ex.Message}");
            }
            
        }
    }
}
