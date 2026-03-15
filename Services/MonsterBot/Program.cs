using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonsterBot;
using MonsterBot.DB;
using MonsterBot.Services.Vision;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var appSettings = context.Configuration.Get<AppSettings>()
            ?? throw new InvalidOperationException("Invalid appsettings.json");

        services.Configure<AppSettings>(context.Configuration);

        services.AddHttpClient();

        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseSqlServer(
                context.Configuration["ConnectionString:DefaultConnection"]
                ?? throw new InvalidOperationException("Connection string not found")));

        var provider = appSettings.Ai.Provider.ToLowerInvariant();
        if (provider == "claude")
            services.AddSingleton<IVisionService, ClaudeVisionService>();
        else
            services.AddSingleton<IVisionService, MistralVisionService>();

        services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds |
                             GatewayIntents.GuildMessages |
                             GatewayIntents.MessageContent
        }));

        services.AddHostedService<BotService>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
