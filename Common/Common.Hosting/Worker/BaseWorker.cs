using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Common.Hosting.Worker.Options;
using Common.Logger;

namespace Common.Hosting.Worker;

/// <summary>
/// Worker de base avec logging simple. Réutilisable pour tout type de worker Windows Service.
/// </summary>
public abstract class BaseWorker<TOptions> : BackgroundService where TOptions : WorkerOptions
{
    protected readonly TOptions Options;
    protected readonly ILogService LogService;
    protected readonly IHostApplicationLifetime Lifetime;

    private bool _isShuttingDown = false;

    protected string Name => Options.Name;
    protected int DelayInSeconds => Options.DelayInSeconds;

    protected BaseWorker(IOptions<TOptions> options, IHostApplicationLifetime lifetime, ILogService logService)
    {
        Options = options.Value;
        Lifetime = lifetime;
        LogService = logService;

        Lifetime.ApplicationStarted.Register(OnStart);
        Lifetime.ApplicationStopping.Register(OnShutDown);

        if (Environment.UserInteractive)
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                if (!_isShuttingDown)
                {
                    e.Cancel = true;
                    LogService.Log($"SIGINT reçu pour le worker {Name}");
                    _isShuttingDown = true;
                    Lifetime.StopApplication();
                }
            };
        }
    }

    protected virtual void OnShutDown()
    {
        LogService.Log($"Arrêt du worker {Name}");
    }

    protected virtual void OnStart()
    {
        LogService.Log($"Démarrage du worker {Name}");
    }

    protected override abstract Task ExecuteAsync(CancellationToken stoppingToken);
}
