using Discord;
using Discord.WebSocket;
using DiscordBot;
using DiscordBot.DB;
using DiscordBot.Services.GetDevices;
using Common.Hosting.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .UseSerilogWithFileRotation("DiscordBot")
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("appsettings.json", optional: false);
    })
    .ConfigureServices((context, services) =>
    {
        var appSettings = context.Configuration.Get<AppSettings>();

        services.AddHostedService<Worker>();
        services.AddScoped<IGetDevicesServices, GetDevicesServices>();
        services.Configure<GetDevicesServiceOptions>(options =>
        {
            options.Url = appSettings.ApiFreeboxUrl;
        });

        services.Configure<AppSettings>(context.Configuration);
        services.AddSingleton<BotService>();
        services.AddHostedService<BotService>(sp => sp.GetRequiredService<BotService>());
        services.AddDbContextFactory<ApplicationDbContext>(options =>
        {

            if (appSettings == null)
            {
                throw new InvalidProgramException("Invalid appsettings.json");
            }
            options.UseSqlServer(appSettings.ConnectionString.DefaultConnection);
        });
        // Also register DbContext directly for scoped services (like Worker)
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (appSettings == null)
            {
                throw new InvalidProgramException("Invalid appsettings.json");
            }
            options.UseSqlServer(appSettings.ConnectionString.DefaultConnection);
        });

        services.AddSingleton(new DiscordSocketClient(
            new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds |
                                 GatewayIntents.GuildMessages |
                                 GatewayIntents.MessageContent
            }
));
    })
    .Build()
    .RunAsync(); // Le Host démarre et exécute automatiquement tous les IHostedService