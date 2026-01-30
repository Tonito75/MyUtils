using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Common.Discord;
using Common.Hosting.Worker.Options;
using Common.Logger;

namespace Common.Hosting.Worker;

/// <summary>
/// Worker avec notification Discord intégrée. Envoie automatiquement des messages de démarrage/arrêt.
/// Réutilisable dans tous les projets nécessitant des notifications Discord.
/// </summary>
public abstract class DiscordWorker<TOptions> : BaseWorker<TOptions> where TOptions : DiscordWorkerOptions
{
    protected readonly IDiscordWebHookService DiscordService;

    protected DiscordWorker(
        IOptions<TOptions> options,
        IHostApplicationLifetime lifetime,
        ILogService logService,
        IDiscordWebHookService discordService)
        : base(options, lifetime, logService)
    {
        DiscordService = discordService;
    }

    protected override void OnStart()
    {
        base.OnStart();
        DiscordService.SendStartAsync($"Démarrage du worker {Name}").GetAwaiter().GetResult();
    }

    protected override void OnShutDown()
    {
        DiscordService.SendStopAsync($"Arrêt du worker {Name}").GetAwaiter().GetResult();
        base.OnShutDown();
    }
}
