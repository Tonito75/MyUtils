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

    private readonly HashSet<ulong> _channelIds = new(options.Value.Discord.ChannelIds);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client.Log += LogAsync;
        client.MessageReceived += OnMessageReceivedAsync;
        client.Ready += OnReadyAsync;
        client.SlashCommandExecuted += OnSlashCommandAsync;

        await client.LoginAsync(TokenType.Bot, options.Value.Discord.Token);
        await client.StartAsync();

        _ = RunReminderLoopAsync(cancellationToken);
    }

    private async Task RunReminderLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try { await CheckInactiveUsersAsync(); }
            catch (Exception ex) { logger.LogError(ex, "Erreur lors du check d'inactivité."); }
        }
    }

    private async Task CheckInactiveUsersAsync()
    {
        var channelId = _channelIds.FirstOrDefault();
        if (channelId == 0 || client.GetChannel(channelId) is not IMessageChannel channel)
            return;

        await using var db = await dbContextFactory.CreateDbContextAsync();

        var threshold = DateTime.UtcNow.AddDays(-3);

        var inactiveUsers = await db.MonsterScans
            .GroupBy(s => new { s.DiscordUserId, s.UtilisateurDiscord })
            .Select(g => new { g.Key.DiscordUserId, g.Key.UtilisateurDiscord, LastScan = g.Max(s => s.Date) })
            .Where(u => u.LastScan < threshold)
            .ToListAsync();

        foreach (var user in inactiveUsers)
        {
            var mention = user.DiscordUserId != 0 ? $"<@{user.DiscordUserId}>" : $"**{user.UtilisateurDiscord}**";
            await channel.SendMessageAsync($"{mention}, you didn't unleash the beast in the last 3 days. Are you ok bro? 🥤");
            logger.LogInformation("Reminder envoyé à {User}.", user.UtilisateurDiscord);
        }
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

            var cancel = new SlashCommandBuilder()
                .WithName("cancel")
                .WithDescription("Supprime le dernier scan enregistré")
                .Build();

            var help = new SlashCommandBuilder()
                .WithName("help")
                .WithDescription("Affiche les commandes disponibles")
                .Build();

            await guild.CreateApplicationCommandAsync(unleash);
            await guild.CreateApplicationCommandAsync(top);
            await guild.CreateApplicationCommandAsync(cancel);
            await guild.CreateApplicationCommandAsync(help);

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
                "cancel"  => await CancelLastAsync(),
                "help"    => BuildHelp(),
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

    private static string BuildHelp() =>
        """
        🥤 **MonsterBot**
        • Envoie une photo de canette → le bot l'identifie et l'enregistre
        `/top` — classement global des Monsters les plus bus
        `/unleash` — top 2 de chaque user
        `/cancel` — annule le dernier scan
        """;

    private async Task<string> CancelLastAsync()
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var last = await db.MonsterScans
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();

        if (last is null)
            return "Aucun scan à annuler.";

        db.MonsterScans.Remove(last);
        await db.SaveChangesAsync();

        return $"❌ Dernier scan supprimé : **{last.Nom}** de **{last.UtilisateurDiscord}** ({last.Date:dd/MM/yyyy HH:mm})";
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
            var favorites = string.Join(", ", u.Top.Select(t =>
            {
                var emoji = MonsterCatalog.Resolve(t.Nom)?.Emoji ?? "🟢";
                return $"{emoji} **{t.Nom}** ({t.Count}x)";
            }));
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

        var lines = top.Select((t, i) =>
        {
            var emoji = MonsterCatalog.Resolve(t.Nom)?.Emoji ?? "🟢";
            return $"{i + 1}. {emoji} **{t.Nom}** — {t.Count}x";
        });
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
        if (!_channelIds.Contains(message.Channel.Id)) return;
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

            string? rawName;
            try
            {
                rawName = await visionService.AnalyzeAsync(bytes, mediaType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Vision API failed for attachment {Filename}", attachment.Filename);
                await userMessage.Channel.SendMessageAsync($"Erreur lors de l'analyse de l'image : {ex.Message}");
                continue;
            }

            var entry = MonsterCatalog.Resolve(rawName);
            if (entry is not null)
            {
                logger.LogInformation("Monster identifié : {Raw} → {Canonical}", rawName, entry.CanonicalName);
                detectedNames.Add($"{entry.Emoji} {entry.CanonicalName}");
            }
            else
            {
                logger.LogInformation("Aucune correspondance dans le catalogue pour : {Raw}", rawName);
            }
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
            // Retire l'emoji (format "🟢 Monster Ultra Paradise") avant de persister
            var cleanName = name.Contains(' ') ? name[(name.IndexOf(' ') + 1)..] : name;
            db.MonsterScans.Add(new MonsterScan
            {
                Nom = cleanName,
                UtilisateurDiscord = username,
                DiscordUserId = author.Id,
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
