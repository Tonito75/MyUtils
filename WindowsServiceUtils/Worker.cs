using Microsoft.Extensions.Options;
using WindowsServiceUtils.AppSettings;
using WindowsServiceUtils.Services.BackupLogFiles;
using WindowsServiceUtils.Services.IO;

namespace WindowsServiceUtils
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly List<FileSavingItem> _fileSavingArray;
        private readonly IIOService _ioService;
        private readonly IBackupFilesService _bakckupFilesService;

        public Worker(ILogger<Worker> logger, IOptions<FileSavingConfig> fileSavingConfig, IIOService iOService, IBackupFilesService bakckupFilesService, ICredentialService credentialService)
        {
            _logger =  logger;
            _fileSavingArray = fileSavingConfig.Value.FileSavingArray;
            _ioService = iOService;
            _bakckupFilesService = bakckupFilesService;
            _credentialService = credentialService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                ExecuteSaveFiles();

                await Task.Delay(5000, stoppingToken);
            }
        }

        private void ExecuteSaveFiles()
        {
            foreach (var fileSavingItem in _fileSavingArray)
            {
                _bakckupFilesService.BackUp(fileSavingItem);
            }
        }

        private void ExecuteSaveDatabase()
        {

        }
    }
}
