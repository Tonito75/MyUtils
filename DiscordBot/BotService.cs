using Discord;
using Discord.Net;
using Discord.WebSocket;
using DiscordBot.DB;
using DiscordBot.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

public class BotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly string _token;
    private readonly TaskCompletionSource _readyTcs = new();

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    private readonly ulong _guildId;

    public Task Ready => _readyTcs.Task;

    public BotService(
        DiscordSocketClient client,
        IConfiguration config,
        IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _client = client;
        _token = config["Discord:Token"]!;
        _guildId = Convert.ToUInt64(config["Discord:ServerGuildId"]!);

        _dbContextFactory = dbContextFactory;

        client.SlashCommandExecuted += SlashCommandHandler;
    }

    public async Task ClientReadyAsync()
    {
        var guild = _client.GetGuild(_guildId);

        var guildCommand = new SlashCommandBuilder();

        guildCommand.WithName("list");
        guildCommand.WithDescription("Simple list of all devices known by the freebox.");
        guildCommand.AddOption("verbose", ApplicationCommandOptionType.String, "aa");

        var globalCommand = new SlashCommandBuilder();
        globalCommand.WithName("list");
        globalCommand.WithDescription("Simple list of all devices known by the freebox.");
        globalCommand.AddOption("verbose", ApplicationCommandOptionType.String, "aa");

        try
        {
            await guild.CreateApplicationCommandAsync(guildCommand.Build());
            await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case "list":
                await HandleListCommand(command);
                break;
        }
    }

    private async Task HandleListCommand(SocketSlashCommand command)
    {
        var param = command.Data.Options.FirstOrDefault()?.Value;
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var devices = await dbContext.LanDevices.OrderBy(d => d.IsConnected).OrderBy(d => d.LastConnected).ToListAsync();

        var nbDevicesByChunk = 10;

        var firstChunk = devices.Skip(0).Take(nbDevicesByChunk).ToList();

        await command.RespondAsync(param != null ? firstChunk.FormatVerboseList() : firstChunk.FormatSimpleList());

        var index = nbDevicesByChunk;

        var nextChunk = new List<LanDevice>();

        while(index < devices.Count)
        {
            if(nextChunk.Count == nbDevicesByChunk)
            {
                await command.FollowupAsync(param != null ? nextChunk.FormatVerboseList() : nextChunk.FormatSimpleList());
                nextChunk.Clear();
            }
            nextChunk.Add(devices[index]);
            index++;
        }

        if(nextChunk != null &&  nextChunk.Count > 0)
        {
            await command.FollowupAsync(param != null ? nextChunk.FormatVerboseList() : nextChunk.FormatSimpleList());
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += LogAsync;
        _client.MessageReceived += OnMessageReceivedAsync;
        _client.Ready += OnReadyAsync;

        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
    }

    private async Task OnReadyAsync()
    {
        _readyTcs.TrySetResult();
        await ClientReadyAsync();
        return;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
    }

    private Task LogAsync(LogMessage message)
    {
        Console.WriteLine(message);
        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot)
            return;

        // ...
    }
}
