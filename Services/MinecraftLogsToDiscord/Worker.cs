using System.Text.RegularExpressions;
using Common.Classes;
using Common.Discord;
using Common.FileWatcher;
using Common.Logger;

namespace MinecraftLogsToDiscord
{
    public class Worker : BackgroundService
    {
        private readonly ILogService _logger;
        private readonly IDiscordWebHookService _discordService;
        private readonly IFileWatcherService _fileWatcherService;

        private Settings _settings;

        public Worker(ILogService logger, Settings settings, IDiscordWebHookService discordService, IFileWatcherService fileWatcherService)
        {
            _logger = logger;
            _settings = settings;
            _discordService = discordService;
            _fileWatcherService = fileWatcherService;

            if(_settings == null)
            {
                throw new ArgumentNullException("Les settings ont été null");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discordService.SendAsync(_settings.WebhookUrl,$"Programme de surveillance des logs du fichier {_settings.LatestLogPath} démarré.");

            var (fileWatcherStarted, errorMessage) = _fileWatcherService.Init(_settings.LatestLogPath, async (line) =>
            {
                if (line != null)
                {
                    var parsedMessage = ParseLineToDiscordMessage(line);
                    if (!string.IsNullOrEmpty(parsedMessage))
                    {
                        try
                        {
                            await _discordService.SendAsync(_settings.WebhookUrl, parsedMessage);
                            _logger.Log($"Envoi du message [{parsedMessage}] à discord");
                        }
                        catch (Exception ex)
                        {
                            _logger.Log($"Impossible d'envoyer la notification discord à l'url {_settings.WebhookUrl} car : {ex.Message}");
                        }
                    }
                }
            });

            if (!fileWatcherStarted)
            {
                _logger.Error($"Impossible de démarrer la surveillance du fichier : {errorMessage}");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.Log("Le worker logs to discord tourne");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            await _discordService.SendAsync(_settings.WebhookUrl, $"Programme de surveillance des logs du fichier {_settings.LatestLogPath} arrêté.");
        }

        private string ParseLineToDiscordMessage(string line)
        {
            var rowTab = line.Split(":");
            if(rowTab.Length == 0)
            {
                return string.Empty;
            }

            var message = rowTab[rowTab.Length - 1];

            // Chat message : filtered
            if(message.Contains("<") && message.Contains(">"))
            {
                return string.Empty;
            }

            var deathPattern = Regex.Match(message, _settings.DeathMessagesPatterns);
            if (deathPattern.Success)
            {
                return $"{Emojis.Skull} {message}";
            }

            var advancementPattern = Regex.Match(message, _settings.AdvancementPatterns);
            if (advancementPattern.Success)
            {
                return $"{Emojis.Success} {message}";
            }

            var connexionPattern = Regex.Match(message, _settings.ConnexionPatterns);
            if (connexionPattern.Success)
            {
                return $"{Emojis.Connected} {message}";
            }

            var deconnexionPattern = Regex.Match(message, _settings.DeconnexionPattern);
            if (deconnexionPattern.Success)
            {
                return $"{Emojis.UnConnected} {message}";
            }

            var lagPattern = Regex.Match(message,_settings.LagPattern);
            if (lagPattern.Success)
            {
                return $"{Emojis.Wait} {message}";
            }

            var startPattern = Regex.Match(message, _settings.StartPattern);
            if(startPattern.Success)
            {
                return $"{Emojis.StartPlay} Démarrage du serveur";
            }

            var stopPattern = Regex.Match(message, _settings.StopPattern);
            if (stopPattern.Success)
            {
                return $"{Emojis.StopPlay} Arrêt du serveur";
            }

            return string.Empty;
        }
    }
}
