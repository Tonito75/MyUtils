# MonsterBot Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Discord bot that detects Monster Energy can colors in images, returns them to the user, and persists one DB row per can color.

**Architecture:** Event-driven .NET 10 IHostedService bot using Discord.Net. `BotService` listens for messages on a configured channel, delegates AI vision to `IVisionService` (Mistral or Claude, chosen at startup via config), and persists results via EF Core to SQL Server.

**Tech Stack:** .NET 10, C#, Discord.Net 3.18.0, Entity Framework Core 10.0.1 (SQL Server), Mistral REST API, Anthropic.SDK 5.10.0, IHttpClientFactory, Generic Host.

---

## Chunk 1: Project scaffolding + configuration

### Task 1: Update .csproj with NuGet packages

**Files:**
- Modify: `Services/MonsterBot/MonsterBot.csproj`

- [ ] Replace the contents of `MonsterBot.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>MonsterBot</RootNamespace>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="..\..\.dockerignore">
      <Link>.dockerignore</Link>
    </Content>
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Anthropic.SDK" Version="5.10.0" />
    <PackageReference Include="Discord.Net" Version="3.18.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.1">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.1" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="10.0.1" />
  </ItemGroup>

</Project>
```

- [ ] Restore packages and verify build compiles:

```bash
cd C:/Users/antoi/Documents/Code/MyUtils/Services/MonsterBot
dotnet restore
dotnet build
```

Expected: build succeeds (warnings OK, errors not OK).

---

### Task 2: GlobalUsings + AppSettings + appsettings.json

**Files:**
- Modify: `Services/MonsterBot/Program.cs` (replace stub)
- Create: `Services/MonsterBot/GlobalUsings.cs`
- Create: `Services/MonsterBot/AppSettings.cs`
- Modify: `Services/MonsterBot/appsettings.json` (replace stub if present, else create)

- [ ] Create `GlobalUsings.cs`:

```csharp
global using Microsoft.EntityFrameworkCore;
global using MonsterBot.DB;
```

- [ ] Create `AppSettings.cs`:

```csharp
namespace MonsterBot;

public record DiscordSettings(string Token, ulong ChannelId, ulong ServerGuildId);
public record AiSettings(string Provider, string MistralApiKey, string ClaudeApiKey);
public record DbSettings(string DefaultConnection);

public class AppSettings
{
    public required DiscordSettings Discord { get; set; }
    public required AiSettings Ai { get; set; }
    public required DbSettings ConnectionString { get; set; }
}
```

- [ ] Create `appsettings.json`:

```json
{
  "Discord": {
    "Token": "YOUR_DISCORD_BOT_TOKEN",
    "ChannelId": "0",
    "ServerGuildId": "0"
  },
  "Ai": {
    "Provider": "mistral",
    "MistralApiKey": "YOUR_MISTRAL_API_KEY",
    "ClaudeApiKey": "YOUR_CLAUDE_API_KEY"
  },
  "ConnectionString": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MonsterBot;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

- [ ] Replace `Program.cs` stub with a minimal version that just builds the host (no services yet — will be completed in Task 8):

```csharp
// Program.cs — will be completed in Task 8
Console.WriteLine("MonsterBot starting...");
```

- [ ] Build to verify no compile errors:

```bash
dotnet build
```

Expected: builds clean.

- [ ] Commit:

```bash
git add Services/MonsterBot/MonsterBot.csproj Services/MonsterBot/GlobalUsings.cs Services/MonsterBot/AppSettings.cs Services/MonsterBot/appsettings.json
git commit -m "feat(monsterbot): scaffold project with packages and config"
```

---

## Chunk 2: DB layer

### Task 3: MonsterScan entity + DbContext + DbContextFactory

**Files:**
- Create: `Services/MonsterBot/DB/MonsterScan.cs`
- Create: `Services/MonsterBot/DB/ApplicationDbContext.cs`
- Create: `Services/MonsterBot/DB/ApplicationDbContextFactory.cs`

- [ ] Create `DB/MonsterScan.cs`:

```csharp
namespace MonsterBot.DB;

public class MonsterScan
{
    public int Id { get; set; }
    public required string Couleur { get; set; }
    public required string UtilisateurDiscord { get; set; }
    public DateTime Date { get; set; }
}
```

- [ ] Create `DB/ApplicationDbContext.cs`:

```csharp
namespace MonsterBot.DB;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<MonsterScan> MonsterScans { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MonsterScan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Couleur).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UtilisateurDiscord).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Date).HasColumnType("datetime2");
        });

        base.OnModelCreating(modelBuilder);
    }
}
```

- [ ] Create `DB/ApplicationDbContextFactory.cs` (required for `dotnet ef migrations add` to work without running the full host):

```csharp
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MonsterBot.DB;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = config["ConnectionString:DefaultConnection"]
            ?? throw new InvalidOperationException("Connection string not found in appsettings.json");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
```

- [ ] Build to verify:

```bash
dotnet build
```

Expected: builds clean.

---

### Task 4: EF Core migration

**Files:**
- Create: `Services/MonsterBot/Migrations/` (generated by EF tools)

- [ ] Generate the initial migration:

```bash
cd C:/Users/antoi/Documents/Code/MyUtils/Services/MonsterBot
dotnet ef migrations add Init
```

Expected: creates `Migrations/` folder with `Init.cs`, `Init.Designer.cs`, and `ApplicationDbContextModelSnapshot.cs`.

- [ ] Verify the migration looks correct — open `Migrations/<timestamp>_Init.cs` and confirm:
  - Table `MonsterScans` with columns `Id`, `Couleur`, `UtilisateurDiscord`, `Date`
  - `Id` is identity primary key
  - `Couleur` maxLength 100, `UtilisateurDiscord` maxLength 200

- [ ] Commit:

```bash
git add Services/MonsterBot/DB/ Services/MonsterBot/Migrations/
git commit -m "feat(monsterbot): add DB layer with MonsterScan entity and EF migration"
```

---

## Chunk 3: Vision services

### Task 5: IVisionService interface + JSON response parser

**Files:**
- Create: `Services/MonsterBot/Services/Vision/IVisionService.cs`
- Create: `Services/MonsterBot/Services/Vision/VisionResponseParser.cs`

- [ ] Create `Services/Vision/IVisionService.cs`:

```csharp
namespace MonsterBot.Services.Vision;

public interface IVisionService
{
    Task<List<string>> AnalyzeAsync(byte[] imageBytes, string mediaType);
}
```

- [ ] Create `Services/Vision/VisionResponseParser.cs`:

```csharp
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MonsterBot.Services.Vision;

public static class VisionResponseParser
{
    private static readonly Regex MarkdownFenceRegex =
        new(@"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.Compiled);

    public static List<string> Parse(string rawResponse, ILogger logger)
    {
        try
        {
            var json = rawResponse.Trim();

            var fenceMatch = MarkdownFenceRegex.Match(json);
            if (fenceMatch.Success)
                json = fenceMatch.Groups[1].Value.Trim();

            var result = JsonSerializer.Deserialize<List<string>>(json);
            return result ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse vision API response: {Raw}", rawResponse);
            return [];
        }
    }
}
```

- [ ] Build to verify:

```bash
dotnet build
```

---

### Task 6: MistralVisionService

**Files:**
- Create: `Services/MonsterBot/Services/Vision/MistralVisionService.cs`

- [ ] Create `Services/Vision/MistralVisionService.cs`:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MonsterBot.Services.Vision;

public class MistralVisionService(
    IHttpClientFactory httpClientFactory,
    IOptions<AppSettings> options,
    ILogger<MistralVisionService> logger) : IVisionService
{
    private const string Endpoint = "https://api.mistral.ai/v1/chat/completions";
    private const string Model = "pixtral-12b-2409";
    private const string Prompt =
        "List the colors of Monster Energy drink cans visible in this image. " +
        "Return ONLY a JSON array of color strings in French (e.g. [\"Verte\", \"Noire\"]). " +
        "If no Monster can is visible, return an empty array [].";

    public async Task<List<string>> AnalyzeAsync(byte[] imageBytes, string mediaType)
    {
        var base64 = Convert.ToBase64String(imageBytes);
        var imageUrl = $"data:{mediaType};base64,{base64}";

        var requestBody = new
        {
            model = Model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image_url", image_url = new { url = imageUrl } },
                        new { type = "text", text = Prompt }
                    }
                }
            }
        };

        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.Value.Ai.MistralApiKey);

        try
        {
            var response = await client.PostAsJsonAsync(Endpoint, requestBody);
            response.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            return VisionResponseParser.Parse(content, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Mistral vision API call failed");
            throw;
        }
    }
}
```

- [ ] Build to verify:

```bash
dotnet build
```

---

### Task 7: ClaudeVisionService

**Files:**
- Create: `Services/MonsterBot/Services/Vision/ClaudeVisionService.cs`

- [ ] Create `Services/Vision/ClaudeVisionService.cs`:

```csharp
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MonsterBot.Services.Vision;

public class ClaudeVisionService(
    IOptions<AppSettings> options,
    ILogger<ClaudeVisionService> logger) : IVisionService
{
    private const string Prompt =
        "List the colors of Monster Energy drink cans visible in this image. " +
        "Return ONLY a JSON array of color strings in French (e.g. [\"Verte\", \"Noire\"]). " +
        "If no Monster can is visible, return an empty array [].";

    private readonly AnthropicClient _client = new(options.Value.Ai.ClaudeApiKey);

    public async Task<List<string>> AnalyzeAsync(byte[] imageBytes, string mediaType)
    {
        var client = _client;

        var base64 = Convert.ToBase64String(imageBytes);

        var messages = new List<Message>
        {
            new()
            {
                Role = RoleType.User,
                Content = new List<ContentBase>
                {
                    new ImageContent
                    {
                        Source = new ImageSource
                        {
                            MediaType = mediaType,
                            Data = base64
                        }
                    },
                    new TextContent { Text = Prompt }
                }
            }
        };

        var request = new MessageParameters
        {
            Model = AnthropicModels.Claude46Sonnet,
            MaxTokens = 256,
            Messages = messages,
            Stream = false
        };

        try
        {
            var response = await client.Messages.GetClaudeMessageAsync(request);
            var text = response.Content.OfType<TextContent>().FirstOrDefault()?.Text
                       ?? response.FirstMessage?.Text
                       ?? string.Empty;
            return VisionResponseParser.Parse(text, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Claude vision API call failed");
            throw;
        }
    }
}
```

- [ ] Build to verify:

```bash
dotnet build
```

- [ ] Commit:

```bash
git add Services/MonsterBot/Services/
git commit -m "feat(monsterbot): add vision services (Mistral + Claude) with JSON parser"
```

---

## Chunk 4: BotService + Program.cs

### Task 8: BotService

**Files:**
- Create: `Services/MonsterBot/BotService.cs`

- [ ] Create `BotService.cs`:

```csharp
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
```

- [ ] Build to verify:

```bash
dotnet build
```

---

### Task 9: Program.cs (host, DI, migration)

**Files:**
- Modify: `Services/MonsterBot/Program.cs`

- [ ] Replace `Program.cs` with the full implementation:

```csharp
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
```

- [ ] Build final:

```bash
dotnet build
```

Expected: zero errors.

- [ ] Commit:

```bash
git add Services/MonsterBot/BotService.cs Services/MonsterBot/Program.cs
git commit -m "feat(monsterbot): add BotService and Program.cs host setup"
```

---

## Chunk 5: readme_ia.md + final wiring

### Task 10: readme_ia.md

**Files:**
- Create: `Services/MonsterBot/readme_ia.md`

- [ ] Create `readme_ia.md`:

```markdown
# MonsterBot — Configuration des API IA

MonsterBot utilise une API IA vision pour analyser les images et détecter les canettes Monster Energy.
Deux providers sont supportés : **Mistral** (par défaut) et **Claude**.

---

## Choisir le provider

Dans `appsettings.json`, modifie la valeur `Ai.Provider` :

```json
"Ai": {
  "Provider": "mistral"   // ou "claude"
}
```

---

## Mistral (défaut recommandé)

### 1. Obtenir une clé API

1. Va sur [https://console.mistral.ai](https://console.mistral.ai)
2. Crée un compte ou connecte-toi
3. Dans **API Keys**, génère une nouvelle clé
4. Copie la clé

### 2. Configurer

Dans `appsettings.json` :

```json
"Ai": {
  "Provider": "mistral",
  "MistralApiKey": "COLLE_TA_CLÉ_ICI"
}
```

### Modèle utilisé
`pixtral-12b-2409` — modèle vision multimodal de Mistral.

---

## Claude (Anthropic)

### 1. Obtenir une clé API

1. Va sur [https://console.anthropic.com](https://console.anthropic.com)
2. Crée un compte ou connecte-toi
3. Dans **API Keys**, génère une nouvelle clé
4. Copie la clé

### 2. Configurer

Dans `appsettings.json` :

```json
"Ai": {
  "Provider": "claude",
  "ClaudeApiKey": "COLLE_TA_CLÉ_ICI"
}
```

### Modèle utilisé
`claude-sonnet-4-6` — modèle vision multimodal d'Anthropic.

---

## Permissions Discord requises

Le bot doit avoir les permissions suivantes dans le salon surveillé :

| Permission | Raison |
|---|---|
| **Read Messages / View Channel** | Lire les messages postés |
| **Send Messages** | Répondre avec les couleurs détectées |
| **Manage Messages** | Supprimer les messages non-image |
| **Read Message History** | Accès à l'historique (requis par Discord.Net) |

> Pour activer ces permissions : va dans les paramètres de ton serveur Discord → Intégrations → MonsterBot → Modifier les permissions du salon.

Le bot doit aussi avoir l'intent **Message Content** activé sur le [portail développeur Discord](https://discord.com/developers/applications) (onglet Bot → Privileged Gateway Intents → Message Content Intent).

---

## Configuration complète (exemple)

```json
{
  "Discord": {
    "Token": "MON_TOKEN_DISCORD",
    "ChannelId": "1234567890123456789",
    "ServerGuildId": "9876543210987654321"
  },
  "Ai": {
    "Provider": "mistral",
    "MistralApiKey": "ma_clé_mistral",
    "ClaudeApiKey": ""
  },
  "ConnectionString": {
    "DefaultConnection": "Server=mon_serveur;Database=MonsterBot;User Id=user;Password=pass;"
  }
}
```

> **Sécurité :** Ne commite jamais `appsettings.json` avec de vraies clés API. Utilise des variables d'environnement en production.
```

- [ ] Commit:

```bash
git add Services/MonsterBot/readme_ia.md
git commit -m "docs(monsterbot): add readme_ia.md with API setup and Discord permissions guide"
```

---

### Task 11: Smoke test manuel

Avant de déclarer le bot fonctionnel, effectue ce smoke test minimal :

- [ ] Renseigne un vrai token Discord + channel ID dans `appsettings.json` (bot de test)
- [ ] Lance le bot localement :

```bash
cd C:/Users/antoi/Documents/Code/MyUtils/Services/MonsterBot
dotnet run
```

Expected: le bot se connecte et log `[Discord] Ready`.

- [ ] Dans le salon configuré, poste un message texte (sans image).
Expected: le message est automatiquement supprimé.

- [ ] Poste une image quelconque (pas forcément une canette Monster).
Expected: le bot répond `"Aucune canette Monster détectée."` ou liste des couleurs si une canette est présente.

- [ ] Vérifie en base que les lignes sont bien insérées (si des canettes ont été détectées) :

```sql
SELECT * FROM MonsterScans ORDER BY Date DESC;
```

- [ ] Commit final si tout fonctionne :

```bash
git add Services/MonsterBot/
git commit -m "feat(monsterbot): complete MonsterBot implementation"
```
