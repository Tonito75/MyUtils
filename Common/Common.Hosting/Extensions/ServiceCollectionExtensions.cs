using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common.Discord;
using Common.Hosting.Worker.Options;
using Common.Logger;

namespace Common.Hosting.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute les services de base pour un worker (logging).
    /// </summary>
    public static IServiceCollection AddWorkerServices(this IServiceCollection services)
    {
        services.AddSingleton<ILogService, WorkerLogService>();
        return services;
    }

    /// <summary>
    /// Ajoute les services Discord pour un worker avec webhook.
    /// </summary>
    public static IServiceCollection AddDiscordWorkerServices(this IServiceCollection services, IConfiguration configuration, string sectionName = "Worker")
    {
        services.AddWorkerServices();
        services.AddSingleton<IDiscordWebHookService, DiscordWebHookService>();

        // Configure DiscordWebHookServiceOptions depuis la section Worker
        services.Configure<DiscordWebHookServiceOptions>(options =>
        {
            var webhookUrl = configuration.GetSection(sectionName)["WebHookUrl"];
            if (!string.IsNullOrEmpty(webhookUrl))
            {
                options.WebHookUrl = webhookUrl;
            }
        });

        return services;
    }

    /// <summary>
    /// Configure les options du worker depuis une section de configuration.
    /// </summary>
    public static IServiceCollection ConfigureWorkerOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Worker") where TOptions : WorkerOptions
    {
        services.Configure<TOptions>(configuration.GetSection(sectionName));
        return services;
    }

    /// <summary>
    /// Ajoute un worker hébergé avec ses options configurées.
    /// </summary>
    public static IServiceCollection AddWorker<TWorker, TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string workerSectionName = "Worker")
        where TWorker : class, IHostedService
        where TOptions : WorkerOptions
    {
        services.ConfigureWorkerOptions<TOptions>(configuration, workerSectionName);
        services.AddHostedService<TWorker>();
        return services;
    }

    /// <summary>
    /// Ajoute un worker Discord avec toutes ses dépendances configurées.
    /// </summary>
    public static IServiceCollection AddDiscordWorker<TWorker, TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string workerSectionName = "Worker")
        where TWorker : class, IHostedService
        where TOptions : DiscordWorkerOptions
    {
        services.AddDiscordWorkerServices(configuration, workerSectionName);
        services.AddWorker<TWorker, TOptions>(configuration, workerSectionName);
        return services;
    }
}
