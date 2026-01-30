using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logger;

namespace Common.FileWatcher
{
    public class PollingFileWatcherService : IFileWatcherService, IDisposable
    {
        private readonly ILogService _logger;
        private string? _filePath;
        private CancellationTokenSource? _cancellationTokenSource;
        private Action<string>? _onNewLine;
        private Task? _pollingTask;
        private DateTime _lastFileWriteTime;
        private long _lastPosition;
        private long _lastFileSize;
        private bool _disposed = false;

        public PollingFileWatcherService(ILogService logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _cancellationTokenSource?.Cancel();

            try
            {
                _pollingTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.Error($"Erreur lors de l'attente de fin de la tâche de surveillance : {ex.Message}");
            }

            _cancellationTokenSource?.Dispose();
            _disposed = true;
        }

        public (bool, string) Init(string filePath, Action<string> onNewLine)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return (false, "Le chemin du fichier du FileWatcher Service doit être renseigné");

                }


                _onNewLine = onNewLine;
                _filePath = filePath;

                // Initialize file tracking properties
                if (File.Exists(_filePath))
                {
                    var fileInfo = new FileInfo(_filePath);
                    _lastFileWriteTime = fileInfo.LastWriteTimeUtc;
                    _lastPosition = fileInfo.Length;
                    _lastFileSize = fileInfo.Length;
                    _logger.Log($"Surveillance de fichier initialisée pour {_filePath}. Taille initiale : {_lastFileSize} octets");
                }
                else
                {
                    _logger.Log($"Le fichier {_filePath} n'existe pas. La tâche commencera quand ce sera le cas");
                    _lastFileWriteTime = DateTime.MinValue;
                    _lastPosition = 0;
                    _lastFileSize = 0;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _pollingTask = Task.Run(() => PollFileAsync(_cancellationTokenSource.Token));

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.Error($"Erreur inattendue lors de l'initialisation du filewatcher : {ex.Message}");
                return (false, ex.Message);
            }
        }

        private async Task PollFileAsync(CancellationToken cancellationToken)
        {
            _logger.Log("FileWatcher démarré");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (File.Exists(_filePath))
                    {
                        await CheckForFileChangesAsync();
                    }
                    else
                    {
                        // File doesn't exist - reset tracking and wait for it to appear
                        if (_lastFileSize > 0) // Only log if we were previously tracking a file
                        {
                            _logger.Log("Le fichier de log Minecraft a disparu, en attente de sa réapparition...");
                            ResetFileTracking();
                        }
                    }

                    await Task.Delay(100, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.Log("Surveillance de fichier annulée");
                    break;
                }
                catch (Exception e)
                {
                    _logger.Error($"Erreur lors de la surveillance du fichier : {e.Message}");
                    await Task.Delay(5000, cancellationToken);
                }
            }

            _logger.Log("Surveillance de fichier arrêtée");
        }

        private async Task CheckForFileChangesAsync()
        {
            var fileInfo = new FileInfo(_filePath!);
            var currentWriteTime = fileInfo.LastWriteTimeUtc;
            var currentSize = fileInfo.Length;

            // Check if file was rotated/recreated
            bool fileWasRotated = DetectFileRotation(currentWriteTime, currentSize);

            if (fileWasRotated)
            {
                _logger.Log("Le fichier de log Minecraft a été pivoté/recréé, redémarrage depuis le début");
                ResetFileTracking();
                _lastFileWriteTime = currentWriteTime;
                _lastFileSize = currentSize;
            }

            // Check if there's new content to read
            if (currentSize > _lastPosition)
            {
                await ReadNewLinesAsync();
                _lastFileWriteTime = currentWriteTime;
                _lastFileSize = currentSize;
            }
            else if (currentSize < _lastPosition)
            {
                // File was truncated - this shouldn't happen with Minecraft logs normally,
                // but handle it just in case
                _logger.Log("Le fichier semble avoir été tronqué, redémarrage depuis le début");
                ResetFileTracking();
                _lastFileWriteTime = currentWriteTime;
                _lastFileSize = currentSize;
            }
        }

        private bool DetectFileRotation(DateTime currentWriteTime, long currentSize)
        {
            // Multiple indicators that the file might have been rotated:

            // 1. File size significantly smaller than our last position (strong indicator)
            if (currentSize < _lastPosition - 1000) // Allow small variance for partial writes
            {
                return true;
            }

            // 2. File size is much smaller than what we last saw AND write time changed
            if (currentSize < _lastFileSize / 2 && currentWriteTime != _lastFileWriteTime)
            {
                return true;
            }

            // 3. File size is very small (likely new file) and we were tracking a larger file
            if (currentSize < 1000 && _lastFileSize > 10000)
            {
                return true;
            }

            return false;
        }

        private void ResetFileTracking()
        {
            _lastPosition = 0;
            _lastFileSize = 0;
            _lastFileWriteTime = DateTime.MinValue;
        }

        private async Task ReadNewLinesAsync()
        {
            try
            {
                using var fileStream = new FileStream(_filePath!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream, Encoding.UTF8);

                // Seek to last known position
                fileStream.Seek(_lastPosition, SeekOrigin.Begin);

                string? line;
                var linesRead = 0;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _onNewLine?.Invoke(line.Trim());
                        linesRead++;
                    }
                }

                _lastPosition = fileStream.Position;

                if (linesRead > 0)
                {
                    _logger.Log($"{linesRead} nouvelles lignes lues du log Minecraft");
                }
            }
            catch (FileNotFoundException)
            {
                _logger.Log("Le fichier a disparu pendant la tentative de lecture");
                ResetFileTracking();
            }
            catch (IOException ex)
            {
                _logger.Error($"Erreur d'E/S lors de la lecture du fichier (le fichier pourrait être verrouillé) : {ex.Message}");
                // Don't reset position on IO errors - the file might just be temporarily locked
            }
            catch (Exception e)
            {
                _logger.Error($"Erreur lors de la lecture des nouvelles lignes du fichier : {e.Message}");
            }
        }
    }
}