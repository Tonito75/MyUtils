using Discord;
using Discord.WebSocket;
using DiscordBot;
using DiscordBot.DB;
using DiscordBot.Services.GetDevices;
using Common.Hosting.Extensions;
using Microsoft.EntityFrameworkCore;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .UseSerilogWithFileRotation("DiscordBot")
    .ConfigureServices((context, services) =>
    {
        var appSettings = context.Configuration.Get<AppSettings>()
            ?? throw new InvalidOperationException("Invalid appsettings.json");

        services.Configure<AppSettings>(context.Configuration);
        services.Configure<GetDevicesServiceOptions>(options =>
        {
            options.Url = appSettings.ApiFreeboxUrl;
        });

        services.AddScoped<IGetDevicesServices, GetDevicesServices>();
        services.AddHostedService<Worker>();

        services.AddSingleton<BotService>();
        services.AddHostedService<BotService>(sp => sp.GetRequiredService<BotService>());

        services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds |
                             GatewayIntents.GuildMessages |
                             GatewayIntents.MessageContent
        }));

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(appSettings.ConnectionString.DefaultConnection));

        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseSqlServer(appSettings.ConnectionString.DefaultConnection), ServiceLifetime.Scoped);
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
