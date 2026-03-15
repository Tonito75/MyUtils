using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        await client.LoginAsync(TokenType.Bot, options.Value.Discord.Token);
        await client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        client.Log -= LogAsync;
        client.MessageReceived -= OnMessageReceivedAsync;
        await client.StopAsync();
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
        {
            try { await userMessage.DeleteAsync(); }
            catch (Exception ex) { logger.LogWarning(ex, "Could not delete non-image message {Id}", userMessage.Id); }
            return;
        }

        var allColors = new List<string>();

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
                await userMessage.Channel.SendMessageAsync("Erreur lors du téléchargement de l'image.");
                continue;
            }

            List<string> colors;
            try
            {
                colors = await visionService.AnalyzeAsync(bytes, mediaType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Vision API failed for attachment {Filename}", attachment.Filename);
                await userMessage.Channel.SendMessageAsync("Erreur lors de l'analyse de l'image.");
                continue;
            }

            allColors.AddRange(colors);
        }

        await PersistScansAsync(allColors, message.Author);

        var reply = allColors.Count == 0
            ? "Aucune canette Monster détectée."
            : string.Join("\n", allColors.Select(c => $"• {c}"));

        await userMessage.Channel.SendMessageAsync(reply);
    }

    private async Task PersistScansAsync(List<string> colors, IUser author)
    {
        if (colors.Count == 0) return;

        var username = author.Discriminator != "0000" && !string.IsNullOrEmpty(author.Discriminator)
            ? $"{author.Username}#{author.Discriminator}"
            : author.Username;

        await using var db = await dbContextFactory.CreateDbContextAsync();

        foreach (var color in colors)
        {
            db.MonsterScans.Add(new MonsterScan
            {
                Couleur = color,
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
