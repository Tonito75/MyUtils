using System.Timers;
using TimelapseCreator.Configuration;
using Common.Classes.Configuration;
using Common.Discord;
using Common.FTP;
using Common.Logger;
using Common.OpenCV.RTSP;
using Common.Date;

namespace TimelapseCreator
{
    public class WorkerScreener : BackgroundService
    {
        private readonly ILogService _logger;
        private readonly IRTSPService _rtspService;
        private readonly IFTPService _ftpService;
        private readonly IDiscordWebHookService _discordService;
        private readonly IDateService _dateService;

        private readonly System.Timers.Timer _timer;

        private readonly Settings _settings;

        // 6 heures
        private readonly int _timerInterval = 60 * 60 * 1000 * 6;

        public WorkerScreener(ILogService logger, Settings setting, IRTSPService rtspService, IFTPService ftpService, IDiscordWebHookService discordService, IDateService dateService)
        {
            _logger = logger;
            _settings = setting;
            _rtspService = rtspService;
            _ftpService = ftpService;
            _discordService = discordService;
            _dateService = dateService;

            _timer = new System.Timers.Timer(_timerInterval);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;
            _timer.Enabled = true;

            _logger.Log("Démarrage du worker Screener.");
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            foreach (var timelapseConfiguration in _settings.TimelapseConfiguration)
            {
                Task.FromResult(_discordService.SendOkAsync($"Le script de capture RTSP tourne"));
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach(var timelapseConfiguration in _settings.TimelapseConfiguration)
            {
                await _discordService.SendOkAsync($"Démarrage du script de capture RTSP avec délai de {_settings.DelayTimeInSeconds} secondes");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                foreach(var timelapseConfiguration in _settings.TimelapseConfiguration)
                {
                    await CaptureFromRTSP(timelapseConfiguration);
                }
                await Task.Delay(_settings.DelayTimeInSeconds * 1000, stoppingToken);
            }

            foreach (var timelapseConfiguration in _settings.TimelapseConfiguration)
            {
                await _discordService.SendErrorAsync("Fin du script de capture RTSP");
            }
        }

        private async Task CaptureFromRTSP(TimelapseConfiguration timelapseConfiguration)
        {
            var rtspUrl = timelapseConfiguration.RtspConfiguration.GetRtspUrl;

            await _rtspService.Capture(rtspUrl).ContinueWith(t =>
            {
                t.Result.Switch(
                    async imageBytes =>
                    {
                        await SendToFtp(imageBytes, timelapseConfiguration);
                    },
                    async error =>
                    {
                        var message = $"Erreur lors de la récupération du flux RTSP : {error}";
                        await _discordService.SendWarnAsync(message);
                        _logger.Error(message);
                    });
            });
        }

        private async Task SendToFtp(byte[] bytes, TimelapseConfiguration timelapseConfiguration)
        {
            var ftpConfiguration = timelapseConfiguration.FtpConfiguration;

            var fileName = $"{_dateService.GetCurrentDateForFile()}.jpg";
            var folderName = _dateService.GetCurrentDateForFolderYYYYMMDD();

            var filePath = $"{ftpConfiguration.Folder}/{folderName}/{fileName}";

            _ftpService.Init(ftpConfiguration.Host, ftpConfiguration.Port, ftpConfiguration.UserName, ftpConfiguration.Password);

            var (success, error) = await _ftpService.Send(bytes, filePath);
            if(!success)
            {
                var message = $"Une erreur est survenue lors de l'envoi au serveur FTP : {error}";
                await _discordService.SendWarnAsync(message);
                _logger.Error(message);
            }
        }
    }
}
