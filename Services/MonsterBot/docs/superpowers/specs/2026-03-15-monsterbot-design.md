# MonsterBot — Design Spec
Date: 2026-03-15

## Overview

Discord bot (.NET 10, C#) qui surveille un salon configuré, analyse les images postées via une IA vision (Mistral par défaut, Claude en option), détecte les canettes Monster Energy présentes, retourne leurs couleurs à l'utilisateur et les persiste en base SQL Server.

---

## Architecture

Projet autonome (pas de dépendance vers Common.*), cible Linux via Docker.

```
MonsterBot/
├── Program.cs                        # DI, host, migration EF au démarrage
├── AppSettings.cs                    # POCO de configuration
├── appsettings.json                  # Token, channel, AI provider, SQL
├── GlobalUsings.cs                   # using globaux EF + namespace
├── BotService.cs                     # IHostedService Discord (lifecycle + gestion messages)
├── DB/
│   ├── ApplicationDbContext.cs       # DbContext EF
│   ├── ApplicationDbContextFactory.cs # IDesignTimeDbContextFactory (migrations CLI)
│   └── MonsterScan.cs                # Entité (Id, Couleur, UtilisateurDiscord, Date)
├── Services/Vision/
│   ├── IVisionService.cs             # interface : AnalyzeAsync(byte[], string mediaType) -> List<string>
│   ├── MistralVisionService.cs       # implémentation Mistral (HTTP REST)
│   └── ClaudeVisionService.cs        # implémentation Claude (Anthropic SDK)
└── readme_ia.md                      # guide utilisateur pour configurer les API
```

**Note :** Pas de `Worker.cs` séparé — `BotService` gère à la fois le cycle de vie du client Discord et les événements de messages. C'est intentionnel : le bot est purement event-driven, pas de polling périodique.

---

## Configuration (`appsettings.json`)

```json
{
  "Discord": {
    "Token": "...",
    "ChannelId": "0",
    "ServerGuildId": "0"
  },
  "Ai": {
    "Provider": "mistral",
    "MistralApiKey": "...",
    "ClaudeApiKey": "..."
  },
  "ConnectionString": {
    "DefaultConnection": "Server=...;Database=MonsterBot;..."
  }
}
```

- `Ai.Provider` : `"mistral"` (défaut) ou `"claude"` — détermine quel service est injecté au démarrage.
- `Discord.ChannelId` et `ServerGuildId` sont des `ulong` dans `AppSettings.cs`. Le binder .NET résout la chaîne JSON vers `ulong` sans parse manuel.
- `ServerGuildId` est conservé pour usage futur (ex: slash commands guild-scoped) mais n'est pas utilisé dans le flow actuel.
- La clé `"ConnectionString"` (singulier) est une convention intentionnelle du projet, différente du standard .NET `"ConnectionStrings"`. **`IConfiguration.GetConnectionString()` ne fonctionnera pas** — `Program.cs` doit utiliser `configuration["ConnectionString:DefaultConnection"]` directement.

---

## Flow principal

1. `BotService` s'abonne à `MessageReceived` sur le canal `ChannelId`.
2. Messages du bot ignorés.
3. Détection image : une pièce jointe est considérée image si son `ContentType` commence par `image/`, **ou** si son extension est `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp` (fallback si `ContentType` est null/vide).
4. Si le message ne contient **aucune pièce jointe image** → suppression du message → fin.
5. Pour chaque image attachée :
   a. Vérification taille : si > 10 MB → réponse Discord "Image trop volumineuse." → passer à l'image suivante.
   b. Téléchargement en `byte[]` via `HttpClient` avec timeout de 10 secondes.
   c. Détermination du `mediaType` réel (depuis `ContentType` ou extension).
   d. Appel `IVisionService.AnalyzeAsync(bytes, mediaType)` → `List<string> couleurs`.
6. Agrégation de toutes les couleurs de toutes les images du message.
7. Persistance : une ligne `MonsterScan` par couleur détectée (toutes images confondues).
8. **Une seule réponse** dans le canal pour l'ensemble du message :
   - 0 couleur → `"Aucune canette Monster détectée."`
   - 1+ couleurs → liste bulleted des couleurs (ex: `• Verte\n• Noire`).

---

## Base de données

Table `MonsterScans` (une ligne par canette détectée) :

| Colonne              | Type           | Notes                        |
|----------------------|----------------|------------------------------|
| `Id`                 | int (PK, auto) | clé primaire générée par EF  |
| `Couleur`            | nvarchar(100)  | ex: "Verte", "Noire"         |
| `UtilisateurDiscord` | nvarchar(200)  | username (ou username#discriminator pour les comptes legacy) |
| `Date`               | datetime2      | UTC au moment du scan        |

Migration EF Code First. Pattern de migration dans `Program.cs` :

```csharp
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}
```

`ApplicationDbContext` est résolu directement depuis le scope pour la migration. Cela n'interfère pas avec `IDbContextFactory` utilisé dans `BotService`.

**DI / Scopes :** `BotService` est un Singleton. Pour consommer le DbContext (Scoped) depuis un Singleton, `BotService` reçoit `IDbContextFactory<ApplicationDbContext>` par injection et appelle `CreateDbContextAsync()` à chaque traitement de message.

`Program.cs` doit enregistrer la factory avec `services.AddDbContextFactory<ApplicationDbContext>(...)` (pas `AddDbContext`). Note : `AddDbContextFactory` enregistre aussi `ApplicationDbContext` comme service Scoped, ce qui permet au pattern de migration (`GetRequiredService<ApplicationDbContext>`) de fonctionner correctement au démarrage.

---

## Service Vision

Interface commune :

```csharp
public interface IVisionService
{
    Task<List<string>> AnalyzeAsync(byte[] imageBytes, string mediaType);
}
```

### Prompt envoyé aux deux API

> "List the colors of Monster Energy drink cans visible in this image. Return ONLY a JSON array of color strings in French (e.g. [\"Verte\", \"Noire\"]). If no Monster can is visible, return an empty array []."

### Parsing de la réponse (commun aux deux services)

Le texte retourné par l'API peut contenir des balises markdown (` ```json ... ``` `). Le parser doit :
1. Extraire le contenu entre les balises ` ``` ` si présentes (via regex).
2. Désérialiser le JSON array résultant.
3. En cas d'échec → log warning → retourner `[]`.

### MistralVisionService

- Endpoint : `https://api.mistral.ai/v1/chat/completions`
- Modèle : `pixtral-12b-2409` (vision)
- Auth : header `Authorization: Bearer {MistralApiKey}`
- Image transmise en base64 : format `data:{mediaType};base64,{base64String}` (le `mediaType` réel est utilisé, pas hardcodé `image/jpeg`).
- Timeout `HttpClient` : 30 secondes.

### ClaudeVisionService

- SDK : `Anthropic.SDK` NuGet (version `5.10.0`)
- Modèle : `claude-sonnet-4-6`
- Image transmise comme bloc `image` avec `media_type` issu du paramètre `mediaType`.
- Parsing identique (markdown fence stripping + JSON array).

---

## Packages NuGet

| Package | Version | Usage |
|---|---|---|
| `Discord.Net` | 3.18.0 | Client Discord |
| `Microsoft.EntityFrameworkCore` | 10.0.1 | ORM |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.1 | Driver SQL Server |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.1 | Migrations CLI |
| `Microsoft.Extensions.Hosting` | 10.0.1 | Generic Host |
| `Anthropic.SDK` | 5.10.0 | API Claude |

**Note :** `Microsoft.Extensions.Hosting.WindowsServices` est **exclu** — le projet cible Linux/Docker.

**HttpClient :** Les deux usages (téléchargement d'images dans `BotService`, appels REST dans `MistralVisionService`) doivent passer par `IHttpClientFactory` pour éviter l'épuisement de sockets. Dans `Program.cs` :
- `services.AddHttpClient()` (client nommé ou non nommé) — `BotService` et `MistralVisionService` reçoivent `IHttpClientFactory` par injection et appellent `CreateClient()`.
- Les timeouts sont configurés au niveau du `HttpClient` créé, pas sur la factory.

---

## Gestion d'erreurs

- Image > 10 MB → réponse Discord "Image trop volumineuse." (pas de crash).
- Timeout téléchargement → log error + réponse Discord "Erreur lors du téléchargement de l'image."
- Parsing JSON échoué → log warning + liste vide (pas de crash).
- API vision en erreur → log error + réponse Discord "Erreur lors de l'analyse de l'image."
- Message non-image → supprimé silencieusement.

---

## readme_ia.md

Fichier à la racine expliquant :
- Comment obtenir une clé API Mistral (console.mistral.ai)
- Comment obtenir une clé API Claude (console.anthropic.com)
- Où les renseigner dans `appsettings.json`
- Comment changer de provider (`"mistral"` / `"claude"`)
- Permissions Discord requises pour le bot : **Manage Messages** (pour supprimer), **Send Messages**, **Read Message History** dans le salon cible
