using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonsterBot.Services;
using MonsterBot.Services.Vision;

namespace MonsterBot;

public class BotService(
    DiscordSocketClient client,
    IOptions<AppSettings> options,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IVisionService visionService,
    IHttpClientFactory httpClientFactory,
    ILogger<BotService> logger) : IHostedService
{
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly ulong _channelId = options.Value.Discord.ChannelId;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client.Log += LogAsync;
        client.MessageReceived += OnMessageReceivedAsync;
        client.Ready += OnReadyAsync;
        client.SlashCommandExecuted += OnSlashCommandAsync;

        await client.LoginAsync(TokenType.Bot, options.Value.Discord.Token);
        await client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        client.Log -= LogAsync;
        client.MessageReceived -= OnMessageReceivedAsync;
        client.Ready -= OnReadyAsync;
        client.SlashCommandExecuted -= OnSlashCommandAsync;
        await client.StopAsync();
    }

    private async Task OnReadyAsync()
    {
        try
        {
            var guild = client.GetGuild(options.Value.Discord.ServerGuildId);
            if (guild is null)
            {
                logger.LogError("Guild {GuildId} introuvable.", options.Value.Discord.ServerGuildId);
                return;
            }

            var unleash = new SlashCommandBuilder()
                .WithName("unleash")
                .WithDescription("Top 2 des Monsters préférés par user")
                .Build();

            var top = new SlashCommandBuilder()
                .WithName("top")
                .WithDescription("Les Monsters les plus bus au total")
                .Build();

            await guild.CreateApplicationCommandAsync(unleash);
            await guild.CreateApplicationCommandAsync(top);

            logger.LogInformation("Slash commands enregistrées sur le serveur {GuildId}.", guild.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Échec de l'enregistrement des slash commands.");
        }
    }

    private async Task OnSlashCommandAsync(SocketSlashCommand command)
    {
        await command.DeferAsync();

        try
        {
            var text = command.CommandName switch
            {
                "unleash" => await BuildListAsync(),
                "top"     => await BuildTopAsync(),
                _         => "Commande inconnue."
            };

            await command.FollowupAsync(text);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Slash command /{Command} failed", command.CommandName);
            await command.FollowupAsync($"Erreur lors de l'exécution de la commande : {ex.Message}");
        }
    }

    private async Task<string> BuildListAsync()
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var byUser = await db.MonsterScans
            .GroupBy(s => s.UtilisateurDiscord)
            .Select(g => new
            {
                User = g.Key,
                Top = g.GroupBy(s => s.Nom)
                        .Select(ng => new { Nom = ng.Key, Count = ng.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(2)
                        .ToList()
            })
            .OrderBy(x => x.User)
            .ToListAsync();

        if (byUser.Count == 0)
            return "Aucun scan enregistré.";

        var lines = byUser.Select(u =>
        {
            var favorites = string.Join(", ", u.Top.Select(t => $"**{t.Nom}** ({t.Count}x)"));
            return $"**{u.User}** → {favorites}";
        });

        return string.Join("\n", lines);
    }

    private async Task<string> BuildTopAsync()
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var top = await db.MonsterScans
            .GroupBy(s => s.Nom)
            .Select(g => new { Nom = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        if (top.Count == 0)
            return "Aucun scan enregistré.";

        var lines = top.Select((t, i) => $"{i + 1}. **{t.Nom}** — {t.Count}x");
        return string.Join("\n", lines);
    }

    private Task LogAsync(LogMessage message)
    {
        logger.LogInformation("[Discord] {Message}", message.ToString());
        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;
        if (message.Channel.Id != _channelId) return;
        if (message is not SocketUserMessage userMessage) return;

        var imageAttachments = userMessage.Attachments
            .Where(IsImage)
            .ToList();

        if (imageAttachments.Count == 0)
            return;

        if (imageAttachments.Count > 1)
        {
            try { await userMessage.DeleteAsync(); }
            catch (Exception ex) { logger.LogWarning(ex, "Could not delete multi-image message {Id}", userMessage.Id); }
            await userMessage.Channel.SendMessageAsync("⚠️ Attention à ne pas unleash trop à la fois.");
            return;
        }

        var detectedNames = new List<string>();

        foreach (var attachment in imageAttachments)
        {
            if (attachment.Size > 10 * 1024 * 1024)
            {
                await userMessage.Channel.SendMessageAsync("Image trop volumineuse.");
                continue;
            }

            var mediaType = ResolveMediaType(attachment);
            byte[] bytes;

            try
            {
                using var http = httpClientFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(10);
                bytes = await http.GetByteArrayAsync(attachment.Url);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to download image {Url}", attachment.Url);
                await userMessage.Channel.SendMessageAsync($"Erreur lors du téléchargement de l'image : {ex.Message}");
                continue;
            }

            (bytes, mediaType) = ImageCompressor.Compress(bytes);
            logger.LogInformation("Image compressée : {Size} bytes ({MediaType})", bytes.Length, mediaType);

            string? name;
            try
            {
                name = await visionService.AnalyzeAsync(bytes, mediaType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Vision API failed for attachment {Filename}", attachment.Filename);
                await userMessage.Channel.SendMessageAsync($"Erreur lors de l'analyse de l'image : {ex.Message}");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(name))
                detectedNames.Add(name);
        }

        if (detectedNames.Count == 0)
        {
            await userMessage.Channel.SendMessageAsync("Aucune canette Monster détectée.");
            return;
        }

        await PersistScansAsync(detectedNames, userMessage.Author);

        var reply = string.Join("\n", detectedNames.Select(n => $"**{userMessage.Author.Username}** just unleashed the beast with **{n}**"));
        await userMessage.Channel.SendMessageAsync(reply);
    }

    private async Task PersistScansAsync(List<string> names, IUser author)
    {
        var username = author.Discriminator != "0000" && !string.IsNullOrEmpty(author.Discriminator)
            ? $"{author.Username}#{author.Discriminator}"
            : author.Username;

        await using var db = await dbContextFactory.CreateDbContextAsync();

        foreach (var name in names)
        {
            db.MonsterScans.Add(new MonsterScan
            {
                Nom = name,
                UtilisateurDiscord = username,
                Date = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    private static bool IsImage(IAttachment attachment)
    {
        if (!string.IsNullOrEmpty(attachment.ContentType) &&
            attachment.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return true;

        var ext = Path.GetExtension(attachment.Filename);
        return ImageExtensions.Contains(ext);
    }

    private static string ResolveMediaType(IAttachment attachment)
    {
        if (!string.IsNullOrEmpty(attachment.ContentType) &&
            attachment.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return attachment.ContentType;

        return Path.GetExtension(attachment.Filename).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
    }
}
