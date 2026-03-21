# Monster Hub — Backend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the full Monster Hub .NET 10 Minimal API backend with ASP.NET Identity, EF Core, JWT auth, FTP image storage, and Mistral vision integration.

**Architecture:** ASP.NET Minimal API with endpoint classes grouped by feature. EF Core + SQL Server with ASP.NET Identity for users. Images stored on a NAS via FTP and proxied through a `/api/images/{*path}` endpoint. Mistral Pixtral detects Monster can types before photo publication.

**Tech Stack:** .NET 10, ASP.NET Core Minimal API, Entity Framework Core, ASP.NET Identity, SQL Server, FluentFTP, Mistral Pixtral API, JWT Bearer

**Spec:** `docs/superpowers/specs/2026-03-21-monster-hub-design.md`

---

## File Map

```
back/
├── MonsterHub.Api.csproj
├── Program.cs                          ← DI, middleware, endpoint registration
├── appsettings.json
├── appsettings.Development.json
├── Dockerfile
│
├── Settings/
│   ├── JwtSettings.cs
│   ├── MistralSettings.cs
│   └── FtpSettings.cs
│
├── Data/
│   ├── AppDbContext.cs                 ← DbContext + model config + seed
│   └── Migrations/                     ← auto-generated
│
├── Models/
│   ├── AppUser.cs
│   ├── Photo.cs
│   ├── PhotoLike.cs
│   ├── UserFriendship.cs
│   ├── Notification.cs
│   └── MonsterMapping.cs
│
├── Dtos/
│   ├── Auth/RegisterRequest.cs
│   ├── Auth/LoginRequest.cs
│   ├── Users/UpdateProfileRequest.cs
│   ├── Users/UserDto.cs
│   ├── Photos/PhotoDto.cs
│   ├── Photos/AnalyzeResultDto.cs
│   ├── Friends/FriendDto.cs
│   └── Notifications/NotificationDto.cs
│
├── Services/
│   ├── IVisionService.cs
│   ├── MistralVisionService.cs         ← copied/adapted from MonsterBot
│   ├── IStorageService.cs
│   ├── FtpStorageService.cs            ← wraps Common FTPService
│   └── MonsterMatchingService.cs       ← matches Mistral output to MonsterMapping
│
└── Endpoints/
    ├── AuthEndpoints.cs
    ├── UserEndpoints.cs
    ├── PhotoEndpoints.cs
    ├── ImageEndpoints.cs
    ├── FriendEndpoints.cs
    ├── NotificationEndpoints.cs
    └── MonsterEndpoints.cs

back.Tests/
├── MonsterHub.Tests.csproj
└── Services/
    └── MonsterMatchingServiceTests.cs
```

---

## Task 1: Project scaffold

**Files:**
- Create: `back/MonsterHub.Api.csproj`
- Create: `back/Program.cs`
- Create: `back/appsettings.json`
- Create: `back/appsettings.Development.json`
- Create: `back.Tests/MonsterHub.Tests.csproj`

- [ ] **Step 1: Create the backend project**

```bash
cd WebApps/PortalMonster
dotnet new webapi -n MonsterHub.Api -o back --use-minimal-apis --no-openapi false
cd back
```

- [ ] **Step 2: Add required NuGet packages**

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package FluentFTP
dotnet add package System.IdentityModel.Tokens.Jwt
```

- [ ] **Step 3: Create the test project**

```bash
cd ..
dotnet new xunit -n MonsterHub.Tests -o back.Tests
cd back.Tests
dotnet add reference ../back/MonsterHub.Api.csproj
dotnet add package Microsoft.NET.Test.Sdk
```

- [ ] **Step 4: Create solution and add projects**

```bash
cd ../..
# from WebApps/PortalMonster
dotnet new sln -n MonsterHub
dotnet sln add back/MonsterHub.Api.csproj
dotnet sln add back.Tests/MonsterHub.Tests.csproj
```

- [ ] **Step 5: Replace `back/appsettings.json` with**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MonsterHub;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True"
  },
  "Jwt": {
    "Secret": "CHANGE_ME_TO_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS",
    "Issuer": "monsterhub",
    "Audience": "monsterhub",
    "ExpiresInDays": 30
  },
  "Mistral": {
    "ApiKey": "CHANGE_ME"
  },
  "Ftp": {
    "Host": "192.168.1.X",
    "Port": "21",
    "UserName": "ftpuser",
    "Password": "ftppassword",
    "BaseRemotePath": "/monsterhub"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 6: Create `back/appsettings.Development.json`**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MonsterHubDev;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True"
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  }
}
```

- [ ] **Step 7: Verify the project builds**

```bash
cd back
dotnet build
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 8: Commit**

```bash
git add back/ back.Tests/ MonsterHub.sln
git commit -m "chore(back): scaffold .NET 10 Minimal API project with packages"
```

---

## Task 2: Settings classes

**Files:**
- Create: `back/Settings/JwtSettings.cs`
- Create: `back/Settings/MistralSettings.cs`
- Create: `back/Settings/FtpSettings.cs`

- [ ] **Step 1: Create `back/Settings/JwtSettings.cs`**

```csharp
namespace MonsterHub.Api.Settings;

public class JwtSettings
{
    public string Secret { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpiresInDays { get; set; } = 30;
}
```

- [ ] **Step 2: Create `back/Settings/MistralSettings.cs`**

```csharp
namespace MonsterHub.Api.Settings;

public class MistralSettings
{
    public string ApiKey { get; set; } = "";
}
```

- [ ] **Step 3: Create `back/Settings/FtpSettings.cs`**

```csharp
namespace MonsterHub.Api.Settings;

public class FtpSettings
{
    public string Host { get; set; } = "";
    public string Port { get; set; } = "21";
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string BaseRemotePath { get; set; } = "/monsterhub";
}
```

- [ ] **Step 4: Commit**

```bash
git add back/Settings/
git commit -m "feat(back): add settings classes for JWT, Mistral, FTP"
```

---

## Task 3: Domain models

**Files:**
- Create: `back/Models/AppUser.cs`
- Create: `back/Models/Photo.cs`
- Create: `back/Models/PhotoLike.cs`
- Create: `back/Models/UserFriendship.cs`
- Create: `back/Models/Notification.cs`
- Create: `back/Models/MonsterMapping.cs`

- [ ] **Step 1: Create `back/Models/AppUser.cs`**

```csharp
using Microsoft.AspNetCore.Identity;

namespace MonsterHub.Api.Models;

public class AppUser : IdentityUser
{
    public string? ProfilePicturePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Photo> Photos { get; set; } = [];
    public ICollection<PhotoLike> Likes { get; set; } = [];
}
```

- [ ] **Step 2: Create `back/Models/MonsterMapping.cs`**

```csharp
namespace MonsterHub.Api.Models;

public class MonsterMapping
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Emoji { get; set; } = "";
    public string? Color { get; set; }
    public string KeywordsJson { get; set; } = "[]";
    public ICollection<Photo> Photos { get; set; } = [];
}
```

- [ ] **Step 3: Create `back/Models/Photo.cs`**

```csharp
namespace MonsterHub.Api.Models;

public class Photo
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    public string FilePath { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int MonsterId { get; set; }
    public MonsterMapping Monster { get; set; } = null!;
    public int LikesCount { get; set; }
    public ICollection<PhotoLike> Likes { get; set; } = [];
}
```

- [ ] **Step 4: Create `back/Models/PhotoLike.cs`**

```csharp
namespace MonsterHub.Api.Models;

public class PhotoLike
{
    public int PhotoId { get; set; }
    public Photo Photo { get; set; } = null!;
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
}
```

- [ ] **Step 5: Create `back/Models/UserFriendship.cs`**

```csharp
namespace MonsterHub.Api.Models;

public enum FriendshipStatus { Pending, Accepted }

public class UserFriendship
{
    public string RequesterId { get; set; } = "";
    public AppUser Requester { get; set; } = null!;
    public string AddresseeId { get; set; } = "";
    public AppUser Addressee { get; set; } = null!;
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 6: Create `back/Models/Notification.cs`**

```csharp
namespace MonsterHub.Api.Models;

public enum NotificationType { FriendRequest }

public class Notification
{
    public int Id { get; set; }
    public string RecipientId { get; set; } = "";
    public AppUser Recipient { get; set; } = null!;
    public NotificationType Type { get; set; }
    public string RelatedEntityId { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 7: Commit**

```bash
git add back/Models/
git commit -m "feat(back): add domain models (AppUser, Photo, MonsterMapping, etc.)"
```

---

## Task 4: DbContext + seed data

**Files:**
- Create: `back/Data/AppDbContext.cs`

- [ ] **Step 1: Create `back/Data/AppDbContext.cs`**

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonsterHub.Api.Models;
using System.Text.Json;

namespace MonsterHub.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<PhotoLike> PhotoLikes => Set<PhotoLike>();
    public DbSet<UserFriendship> UserFriendships => Set<UserFriendship>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<MonsterMapping> MonsterMappings => Set<MonsterMapping>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PhotoLike>()
            .HasKey(l => new { l.PhotoId, l.UserId });

        builder.Entity<UserFriendship>()
            .HasKey(f => new { f.RequesterId, f.AddresseeId });

        builder.Entity<UserFriendship>()
            .HasOne(f => f.Requester)
            .WithMany()
            .HasForeignKey(f => f.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserFriendship>()
            .HasOne(f => f.Addressee)
            .WithMany()
            .HasForeignKey(f => f.AddresseeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Photo>()
            .HasOne(p => p.User)
            .WithMany(u => u.Photos)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PhotoLike>()
            .HasOne(l => l.Photo)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PhotoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PhotoLike>()
            .HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed MonsterMapping
        builder.Entity<MonsterMapping>().HasData(GetMonsterSeed());
    }

    private static MonsterMapping[] GetMonsterSeed() =>
    [
        new() { Id = 1,  Name = "Ultra White",            Emoji = "⚪", KeywordsJson = """["ultra white","white monster","the white monster"]""" },
        new() { Id = 2,  Name = "Ultra Paradise",         Emoji = "🟢", KeywordsJson = """["ultra paradise"]""" },
        new() { Id = 3,  Name = "Ultra Black",            Emoji = "⚫", KeywordsJson = """["ultra black"]""" },
        new() { Id = 4,  Name = "Ultra Fiesta",           Emoji = "🔵", KeywordsJson = """["ultra fiesta"]""" },
        new() { Id = 5,  Name = "Ultra Red",              Emoji = "🔴", KeywordsJson = """["ultra red","red"]""" },
        new() { Id = 6,  Name = "Ultra Violet",           Emoji = "🟣", KeywordsJson = """["ultra violet","purple monster","the purple monster"]""" },
        new() { Id = 7,  Name = "Ultra Gold",             Emoji = "🟡", KeywordsJson = """["ultra gold","gold"]""" },
        new() { Id = 8,  Name = "Ultra Blue",             Emoji = "🔵", KeywordsJson = """["ultra blue","blue monster","the blue monster","blue"]""" },
        new() { Id = 9,  Name = "Ultra Watermelon",       Emoji = "🔴", KeywordsJson = """["ultra watermelon"]""" },
        new() { Id = 10, Name = "Ultra Rosé",             Emoji = "🟣", KeywordsJson = """["ultra rosé","ultra rose"]""" },
        new() { Id = 11, Name = "Ultra Strawberry Dreams",Emoji = "🟣", KeywordsJson = """["ultra strawberry","strawberry dreams"]""" },
        new() { Id = 12, Name = "Ultra Fantasy",          Emoji = "🟣", KeywordsJson = """["ultra fantasy"]""" },
        new() { Id = 13, Name = "Energy Nitro",           Emoji = "⚫", KeywordsJson = """["nitro"]""" },
        new() { Id = 14, Name = "VR46",                   Emoji = "🟡", KeywordsJson = """["vr46","the doctor","valentino"]""" },
        new() { Id = 15, Name = "Full Throttle",          Emoji = "🔵", KeywordsJson = """["full throttle"]""" },
        new() { Id = 16, Name = "Lando Norris",           Emoji = "🟣", KeywordsJson = """["lando norris","lambo norris","norris"]""" },
        new() { Id = 17, Name = "The Original",           Emoji = "🟢", KeywordsJson = """["original green","the original","original energy"]""" },
        new() { Id = 18, Name = "Aussie Lemonade",        Emoji = "🟡", KeywordsJson = """["aussie lemonade","aussie"]""" },
        new() { Id = 19, Name = "Pacific Punch",          Emoji = "🟤", KeywordsJson = """["pacific"]""" },
        new() { Id = 20, Name = "Mango Loco",             Emoji = "🔵", KeywordsJson = """["mango loco","mango"]""" },
        new() { Id = 21, Name = "Khaos",                  Emoji = "🟠", KeywordsJson = """["khaos"]""" },
        new() { Id = 22, Name = "Ripper",                 Emoji = "🩷", KeywordsJson = """["ripper"]""" },
        new() { Id = 23, Name = "Pipeline Punch",         Emoji = "🩷", KeywordsJson = """["pipeline"]""" },
        new() { Id = 24, Name = "Mixxd",                  Emoji = "🟣", KeywordsJson = """["mixxd","mixd"]""" },
    ];
}
```

- [ ] **Step 2: Commit**

```bash
git add back/Data/
git commit -m "feat(back): add AppDbContext with model config and MonsterMapping seed"
```

---

## Task 5: EF migrations

**Files:**
- Create: `back/Data/Migrations/` (auto-generated)

Prerequisites: SQL Server must be running and accessible with the connection string in `appsettings.json`.

- [ ] **Step 1: Install EF tools (if not already installed)**

```bash
dotnet tool install --global dotnet-ef
# or update:
dotnet tool update --global dotnet-ef
```

- [ ] **Step 2: Add initial migration from `back/` folder**

```bash
cd back
dotnet ef migrations add InitialCreate --output-dir Data/Migrations
```
Expected: Migration file created in `Data/Migrations/`.

- [ ] **Step 3: Apply migration to create the database**

```bash
dotnet ef database update
```
Expected: Database `MonsterHubDev` created with all tables.

- [ ] **Step 4: Commit**

```bash
git add back/Data/Migrations/
git commit -m "feat(back): add initial EF Core migration"
```

---

## Task 6: Program.cs — DI, auth, middleware

**Files:**
- Modify: `back/Program.cs`

- [ ] **Step 1: Replace `back/Program.cs` with full setup**

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MonsterHub.Api.Data;
using MonsterHub.Api.Endpoints;
using MonsterHub.Api.Models;
using MonsterHub.Api.Services;
using MonsterHub.Api.Settings;

var builder = WebApplication.CreateBuilder(args);

// Settings
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
var ftpSettings = builder.Configuration.GetSection("Ftp").Get<FtpSettings>()!;
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<MistralSettings>(builder.Configuration.GetSection("Mistral"));
builder.Services.Configure<FtpSettings>(builder.Configuration.GetSection("Ftp"));

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Secret))
    };
});
builder.Services.AddAuthorization();

// CORS (dev only — prod uses Caddy)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? [];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Max request body size (10 MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(o =>
    o.Limits.MaxRequestBodySize = 10 * 1024 * 1024);

// Application services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IStorageService, FtpStorageService>();
builder.Services.AddScoped<IVisionService, MistralVisionService>();
builder.Services.AddScoped<MonsterMatchingService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Run migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevCors");
}

app.UseAuthentication();
app.UseAuthorization();

// Register endpoint groups
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapPhotoEndpoints();
app.MapImageEndpoints();
app.MapFriendEndpoints();
app.MapNotificationEndpoints();
app.MapMonsterEndpoints();

app.Run();
```

- [ ] **Step 2: Verify the project still builds**

```bash
cd back
dotnet build
```
Expected: Build succeeded (some missing endpoint classes will cause errors — fix by creating empty extension methods as stubs if needed, or continue to next tasks).

- [ ] **Step 3: Commit**

```bash
git add back/Program.cs
git commit -m "feat(back): configure DI, JWT auth, Identity, CORS in Program.cs"
```

---

## Task 7: DTOs

**Files:**
- Create: `back/Dtos/Auth/RegisterRequest.cs`
- Create: `back/Dtos/Auth/LoginRequest.cs`
- Create: `back/Dtos/Users/UserDto.cs`
- Create: `back/Dtos/Users/UpdateProfileRequest.cs`
- Create: `back/Dtos/Photos/PhotoDto.cs`
- Create: `back/Dtos/Photos/AnalyzeResultDto.cs`
- Create: `back/Dtos/Friends/FriendDto.cs`
- Create: `back/Dtos/Notifications/NotificationDto.cs`

- [ ] **Step 1: Create auth DTOs**

`back/Dtos/Auth/RegisterRequest.cs`:
```csharp
namespace MonsterHub.Api.Dtos.Auth;
public record RegisterRequest(string Username, string Email, string Password);
```

`back/Dtos/Auth/LoginRequest.cs`:
```csharp
namespace MonsterHub.Api.Dtos.Auth;
public record LoginRequest(string Username, string Password);
```

- [ ] **Step 2: Create user DTOs**

`back/Dtos/Users/UserDto.cs`:
```csharp
namespace MonsterHub.Api.Dtos.Users;
public record UserDto(string Id, string Username, string Email, string? AvatarUrl,
    int PhotoCount, int FriendCount);
```

`back/Dtos/Users/UpdateProfileRequest.cs`:
```csharp
namespace MonsterHub.Api.Dtos.Users;
public record UpdateProfileRequest(string CurrentPassword, string? NewEmail, string? NewPassword);
```

- [ ] **Step 3: Create photo DTOs**

`back/Dtos/Photos/PhotoDto.cs`:
```csharp
namespace MonsterHub.Api.Dtos.Photos;
public record PhotoDto(int Id, string ImageUrl, string UserId, string Username,
    string? AvatarUrl, DateTime CreatedAt, int MonsterId, string MonsterName,
    string MonsterEmoji, int LikesCount, bool LikedByMe);
```

`back/Dtos/Photos/AnalyzeResultDto.cs`:
```csharp
namespace MonsterHub.Api.Dtos.Photos;
public record AnalyzeResultDto(int MonsterId, string MonsterName, string Emoji);
```

- [ ] **Step 4: Create friend and notification DTOs**

`back/Dtos/Friends/FriendDto.cs`:
```csharp
namespace MonsterHub.Api.Dtos.Friends;
public record FriendDto(string UserId, string Username, string? AvatarUrl);
```

`back/Dtos/Notifications/NotificationDto.cs`:
```csharp
using MonsterHub.Api.Models;
namespace MonsterHub.Api.Dtos.Notifications;
public record NotificationDto(int Id, NotificationType Type, string RelatedUserId,
    string RelatedUsername, string? RelatedAvatarUrl, DateTime CreatedAt);
```

- [ ] **Step 5: Commit**

```bash
git add back/Dtos/
git commit -m "feat(back): add all DTOs"
```

---

## Task 8: MonsterMatchingService (TDD)

**Files:**
- Create: `back/Services/MonsterMatchingService.cs`
- Create: `back.Tests/Services/MonsterMatchingServiceTests.cs`

- [ ] **Step 1: Write failing tests in `back.Tests/Services/MonsterMatchingServiceTests.cs`**

```csharp
using MonsterHub.Api.Models;
using MonsterHub.Api.Services;
using System.Text.Json;

namespace MonsterHub.Tests.Services;

public class MonsterMatchingServiceTests
{
    private static List<MonsterMapping> BuildMappings() =>
    [
        new() { Id = 1, Name = "Ultra White", Emoji = "⚪",
            KeywordsJson = """["ultra white","white monster"]""" },
        new() { Id = 2, Name = "The Original", Emoji = "🟢",
            KeywordsJson = """["the original","original energy"]""" },
        new() { Id = 3, Name = "Ultra Gold", Emoji = "🟡",
            KeywordsJson = """["ultra gold","gold"]""" },
    ];

    [Fact]
    public void Match_ExactKeyword_ReturnsCorrectMapping()
    {
        var svc = new MonsterMatchingService();
        var result = svc.Match("ultra white", BuildMappings());
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void Match_CaseInsensitive_ReturnsMatch()
    {
        var svc = new MonsterMatchingService();
        var result = svc.Match("ULTRA WHITE", BuildMappings());
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void Match_PartialContains_ReturnsMatch()
    {
        var svc = new MonsterMatchingService();
        // Mistral might return "Monster Ultra Gold can"
        var result = svc.Match("Monster Ultra Gold can", BuildMappings());
        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
    }

    [Fact]
    public void Match_NoMatch_ReturnsNull()
    {
        var svc = new MonsterMatchingService();
        var result = svc.Match("some random text", BuildMappings());
        Assert.Null(result);
    }

    [Fact]
    public void Match_EmptyString_ReturnsNull()
    {
        var svc = new MonsterMatchingService();
        var result = svc.Match("", BuildMappings());
        Assert.Null(result);
    }

    [Fact]
    public void Match_FirstMatchWins_WhenAmbiguous()
    {
        var svc = new MonsterMatchingService();
        // "gold" matches both "ultra gold" (id=3) via contains — first match by Id order
        var result = svc.Match("gold", BuildMappings());
        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
cd back.Tests
dotnet test --filter "MonsterMatchingServiceTests"
```
Expected: Build error — `MonsterMatchingService` not found.

- [ ] **Step 3: Create `back/Services/MonsterMatchingService.cs`**

```csharp
using MonsterHub.Api.Models;
using System.Text.Json;

namespace MonsterHub.Api.Services;

public class MonsterMatchingService
{
    public MonsterMapping? Match(string mistralOutput, IEnumerable<MonsterMapping> mappings)
    {
        if (string.IsNullOrWhiteSpace(mistralOutput))
            return null;

        var lower = mistralOutput.ToLowerInvariant();

        foreach (var mapping in mappings.OrderBy(m => m.Id))
        {
            var keywords = JsonSerializer.Deserialize<string[]>(mapping.KeywordsJson) ?? [];
            if (keywords.Any(k => lower.Contains(k.ToLowerInvariant())))
                return mapping;
        }

        return null;
    }
}
```

- [ ] **Step 4: Run tests to confirm they pass**

```bash
cd back.Tests
dotnet test --filter "MonsterMatchingServiceTests"
```
Expected: 6 tests pass.

- [ ] **Step 5: Commit**

```bash
git add back/Services/MonsterMatchingService.cs back.Tests/Services/MonsterMatchingServiceTests.cs
git commit -m "feat(back): add MonsterMatchingService with tests"
```

---

## Task 9: Storage service (FTP)

**Files:**
- Create: `back/Services/IStorageService.cs`
- Create: `back/Services/FtpStorageService.cs`

Note: This service uses `FluentFTP` directly (same dependency as `Common.FTPService`). Since the `Common` project uses `OneOf` and other dependencies that add complexity, we reimplement the relevant subset using FluentFTP directly.

- [ ] **Step 1: Create `back/Services/IStorageService.cs`**

```csharp
namespace MonsterHub.Api.Services;

public interface IStorageService
{
    /// <summary>Saves bytes to the NAS. Returns the relative path (without BaseRemotePath).</summary>
    Task<string> SaveAsync(byte[] data, string folder, string userId, string extension);

    /// <summary>Downloads bytes from the NAS by relative path.</summary>
    Task<(byte[] Data, string ContentType)> GetAsync(string relativePath);

    /// <summary>Deletes a file from the NAS by relative path.</summary>
    Task DeleteAsync(string relativePath);
}
```

- [ ] **Step 2: Create `back/Services/FtpStorageService.cs`**

```csharp
using FluentFTP;
using Microsoft.Extensions.Options;
using MonsterHub.Api.Settings;

namespace MonsterHub.Api.Services;

public class FtpStorageService(IOptions<FtpSettings> options, ILogger<FtpStorageService> logger)
    : IStorageService
{
    private readonly FtpSettings _settings = options.Value;

    private AsyncFtpClient CreateClient() =>
        new(_settings.Host, _settings.UserName, _settings.Password, int.Parse(_settings.Port));

    public async Task<string> SaveAsync(byte[] data, string folder, string userId, string extension)
    {
        var relativePath = $"{folder}/{userId}/{Guid.NewGuid():N}.{extension.TrimStart('.')}";
        var remotePath = $"{_settings.BaseRemotePath}/{relativePath}";

        using var client = CreateClient();
        await client.AutoConnect();
        using var stream = new MemoryStream(data);
        await client.UploadStream(stream, remotePath, FtpRemoteExists.OverwriteInPlace, true);
        await client.Disconnect();

        logger.LogInformation("Uploaded {RelativePath}", relativePath);
        return relativePath;
    }

    public async Task<(byte[] Data, string ContentType)> GetAsync(string relativePath)
    {
        var remotePath = $"{_settings.BaseRemotePath}/{relativePath}";
        var ext = Path.GetExtension(relativePath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        using var client = CreateClient();
        await client.AutoConnect();
        using var ms = new MemoryStream();
        await client.DownloadStream(ms, remotePath);
        await client.Disconnect();

        return (ms.ToArray(), contentType);
    }

    public async Task DeleteAsync(string relativePath)
    {
        var remotePath = $"{_settings.BaseRemotePath}/{relativePath}";
        using var client = CreateClient();
        await client.AutoConnect();
        await client.DeleteFile(remotePath);
        await client.Disconnect();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add back/Services/IStorageService.cs back/Services/FtpStorageService.cs
git commit -m "feat(back): add FTP storage service"
```

---

## Task 10: Mistral vision service

**Files:**
- Create: `back/Services/IVisionService.cs`
- Create: `back/Services/MistralVisionService.cs`

- [ ] **Step 1: Create `back/Services/IVisionService.cs`**

```csharp
namespace MonsterHub.Api.Services;

public interface IVisionService
{
    Task<string?> AnalyzeAsync(byte[] imageBytes, string mediaType);
}
```

- [ ] **Step 2: Create `back/Services/MistralVisionService.cs`**

Adapté depuis `Services/MonsterBot/Services/Vision/MistralVisionService.cs` :

```csharp
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MonsterHub.Api.Settings;

namespace MonsterHub.Api.Services;

public class MistralVisionService(
    IHttpClientFactory httpClientFactory,
    IOptions<MistralSettings> options,
    ILogger<MistralVisionService> logger) : IVisionService
{
    private const string Endpoint = "https://api.mistral.ai/v1/chat/completions";
    private const string Model = "pixtral-large-2411";

    private const string Prompt =
        "You are a Monster Energy can identifier. Your output is always one of two things:\n" +
        "1. The exact product name from the list below\n" +
        "2. An empty string\n" +
        "Nothing else. Ever. No explanation. No apology. No sentence. Just the name or nothing.\n" +
        "\n" +
        "Known products:\n" +
        "Monster Ultra White, Monster Ultra Paradise, Monster Ultra Black, Monster Ultra Fiesta,\n" +
        "Monster Ultra Red, Monster Ultra Violet, Monster Ultra Gold, Monster Ultra Blue,\n" +
        "Monster Ultra Watermelon, Monster Ultra Rosé, Monster Ultra Strawberry Dreams, Monster Ultra Fantasy,\n" +
        "Monster Energy Nitro, Monster Energy VR46, Monster Energy The Original, Monster Energy Full Throttle,\n" +
        "Monster Energy Lando Norris, Monster Energy Pipeline Punch,\n" +
        "Juiced Monster Aussie Lemonade, Juiced Monster Pacific Punch, Juiced Monster Mango Loco,\n" +
        "Juiced Monster Khaos, Juiced Monster Ripper, Juiced Monster Mixxd\n" +
        "\n" +
        "If the image contains no Monster Energy can: output an empty string.";

    public async Task<string?> AnalyzeAsync(byte[] imageBytes, string mediaType)
    {
        var base64 = Convert.ToBase64String(imageBytes);
        var imageUrl = $"data:{mediaType};base64,{base64}";

        var requestBody = new
        {
            model = Model,
            messages = new object[]
            {
                new { role = "system", content = Prompt },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image_url", image_url = new { url = imageUrl } }
                    }
                }
            }
        };

        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);

        var json = JsonSerializer.Serialize(requestBody);
        using var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync(Endpoint, httpContent);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            logger.LogError("Mistral API error {Status}: {Body}", (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        logger.LogInformation("Mistral response: {Content}", content);
        return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add back/Services/IVisionService.cs back/Services/MistralVisionService.cs
git commit -m "feat(back): add Mistral vision service (adapted from MonsterBot)"
```

---

## Task 11: Auth endpoints

**Files:**
- Create: `back/Endpoints/AuthEndpoints.cs`

- [ ] **Step 1: Create `back/Endpoints/AuthEndpoints.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MonsterHub.Api.Dtos.Auth;
using MonsterHub.Api.Models;
using MonsterHub.Api.Settings;
using Microsoft.Extensions.Options;

namespace MonsterHub.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (
            RegisterRequest req,
            UserManager<AppUser> userManager,
            IOptions<JwtSettings> jwtOptions) =>
        {
            var user = new AppUser
            {
                UserName = req.Username,
                Email = req.Email,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded)
                return Results.BadRequest(result.Errors.Select(e => e.Description));

            return Results.Ok(new { token = GenerateToken(user, jwtOptions.Value) });
        });

        group.MapPost("/login", async (
            LoginRequest req,
            UserManager<AppUser> userManager,
            IOptions<JwtSettings> jwtOptions) =>
        {
            var user = await userManager.FindByNameAsync(req.Username);
            if (user == null || !await userManager.CheckPasswordAsync(user, req.Password))
                return Results.Unauthorized();

            return Results.Ok(new { token = GenerateToken(user, jwtOptions.Value) });
        });
    }

    private static string GenerateToken(AppUser user, JwtSettings settings)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!)
        };
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(settings.ExpiresInDays),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

- [ ] **Step 2: Verify build**

```bash
cd back && dotnet build
```

- [ ] **Step 3: Manual smoke test**

```bash
dotnet run
# POST http://localhost:5000/api/auth/register
# body: { "username": "test", "email": "test@test.com", "password": "secret" }
# Expected: { "token": "eyJ..." }
```

- [ ] **Step 4: Commit**

```bash
git add back/Endpoints/AuthEndpoints.cs
git commit -m "feat(back): add auth endpoints (register/login)"
```

---

## Task 12: Image proxy endpoint

**Files:**
- Create: `back/Endpoints/ImageEndpoints.cs`

- [ ] **Step 1: Create `back/Endpoints/ImageEndpoints.cs`**

```csharp
using MonsterHub.Api.Services;

namespace MonsterHub.Api.Endpoints;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this WebApplication app)
    {
        app.MapGet("/api/images/{*path}", async (
            string path,
            HttpContext ctx,
            IStorageService storage) =>
        {
            try
            {
                var (data, contentType) = await storage.GetAsync(path);
                // Set Cache-Control for browsers and CDNs (spec §7)
                ctx.Response.Headers.CacheControl = "public, max-age=86400";
                return Results.File(data, contentType,
                    enableRangeProcessing: false,
                    lastModified: null,
                    entityTag: null);
            }
            catch
            {
                return Results.NotFound();
            }
        })
        .CacheOutput(policy => policy
            .Expire(TimeSpan.FromDays(1))
            .SetVaryByRouteValue("path"));
    }
}
```

Note: If `CacheOutput` is not available (requires `builder.Services.AddOutputCache()`), add it to Program.cs and call `app.UseOutputCache()` before endpoint registration.

- [ ] **Step 2: Add OutputCache to Program.cs**

In `Program.cs`, after `builder.Services.AddAuthorization()`:
```csharp
builder.Services.AddOutputCache();
```

And after `app.UseAuthorization()`:
```csharp
app.UseOutputCache();
```

- [ ] **Step 3: Commit**

```bash
git add back/Endpoints/ImageEndpoints.cs back/Program.cs
git commit -m "feat(back): add image proxy endpoint with output cache"
```

---

## Task 13: User endpoints

**Files:**
- Create: `back/Endpoints/UserEndpoints.cs`

- [ ] **Step 1: Create `back/Endpoints/UserEndpoints.cs`**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonsterHub.Api.Data;
using MonsterHub.Api.Dtos.Users;
using MonsterHub.Api.Models;
using MonsterHub.Api.Services;

namespace MonsterHub.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization();

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            UserManager<AppUser> userManager,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return Results.NotFound();

            var photoCount = await db.Photos.CountAsync(p => p.UserId == userId);
            var friendCount = await db.UserFriendships.CountAsync(f =>
                (f.RequesterId == userId || f.AddresseeId == userId)
                && f.Status == FriendshipStatus.Accepted);

            return Results.Ok(new UserDto(user.Id, user.UserName!, user.Email!,
                user.ProfilePicturePath != null ? $"/api/images/{user.ProfilePicturePath}" : null,
                photoCount, friendCount));
        });

        group.MapPut("/me", async (
            UpdateProfileRequest req,
            ClaimsPrincipal principal,
            UserManager<AppUser> userManager) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return Results.NotFound();

            if (!await userManager.CheckPasswordAsync(user, req.CurrentPassword))
                return Results.BadRequest(new { error = "Current password is incorrect." });

            if (!string.IsNullOrWhiteSpace(req.NewPassword))
            {
                var pwResult = await userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
                if (!pwResult.Succeeded)
                    return Results.BadRequest(pwResult.Errors.Select(e => e.Description));
            }

            if (!string.IsNullOrWhiteSpace(req.NewEmail) && req.NewEmail != user.Email)
            {
                // Use Identity's proper pipeline (updates SecurityStamp + NormalizedEmail)
                await userManager.SetEmailAsync(user, req.NewEmail);
            }

            return Results.Ok(new { email = user.Email, username = user.UserName });
        });

        group.MapPut("/me/avatar", async (
            HttpRequest request,
            ClaimsPrincipal principal,
            UserManager<AppUser> userManager,
            IStorageService storage) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
                return Results.BadRequest("No file provided.");

            var file = request.Form.Files[0];
            var ext = Path.GetExtension(file.FileName).TrimStart('.');
            if (ext is not ("jpg" or "jpeg" or "png" or "gif" or "webp"))
                return Results.BadRequest("Unsupported image format.");

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var relativePath = await storage.SaveAsync(ms.ToArray(), "avatars", userId, ext);

            var user = await userManager.FindByIdAsync(userId);
            user!.ProfilePicturePath = relativePath;
            await userManager.UpdateAsync(user);

            return Results.Ok(new { avatarUrl = $"/api/images/{relativePath}" });
        });

        group.MapGet("/search", async (
            string q,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Get IDs of users already in a friendship with current user
            var relatedIds = await db.UserFriendships
                .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
                .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
                .ToListAsync();

            var users = await db.Users
                .Where(u => u.Id != userId
                    && !relatedIds.Contains(u.Id)
                    && u.UserName!.Contains(q))
                .Take(20)
                .Select(u => new FriendSearchResult(
                    u.Id, u.UserName!,
                    u.ProfilePicturePath != null ? $"/api/images/{u.ProfilePicturePath}" : null))
                .ToListAsync();

            return Results.Ok(users);
        });

        group.MapGet("/{id}/photos", async (
            string id,
            int limit,
            int? cursor,
            AppDbContext db,
            ClaimsPrincipal principal) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var pageSize = Math.Clamp(limit == 0 ? 10 : limit, 1, 50);

            var query = db.Photos
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.Id);

            if (cursor.HasValue)
                query = (IOrderedQueryable<Photo>)query.Where(p => p.Id < cursor.Value);

            var photos = await query
                .Take(pageSize)
                .Include(p => p.User)
                .Include(p => p.Monster)
                .Include(p => p.Likes)
                .ToListAsync();

            var likedIds = photos.Select(p => p.Id).ToList();
            var myLikes = await db.PhotoLikes
                .Where(l => l.UserId == currentUserId && likedIds.Contains(l.PhotoId))
                .Select(l => l.PhotoId)
                .ToHashSetAsync();

            return Results.Ok(photos.Select(p => ToDto(p, myLikes.Contains(p.Id))));
        });
    }

    internal static object ToDto(Models.Photo p, bool likedByMe) => new Dtos.Photos.PhotoDto(
        p.Id,
        $"/api/images/{p.FilePath}",
        p.UserId,
        p.User.UserName!,
        p.User.ProfilePicturePath != null ? $"/api/images/{p.User.ProfilePicturePath}" : null,
        p.CreatedAt,
        p.MonsterId,
        p.Monster.Name,
        p.Monster.Emoji,
        p.LikesCount,
        likedByMe);

    private record FriendSearchResult(string UserId, string Username, string? AvatarUrl);
}
```

- [ ] **Step 2: Commit**

```bash
git add back/Endpoints/UserEndpoints.cs
git commit -m "feat(back): add user endpoints (me, avatar, search, user photos)"
```

---

## Task 14: Monster endpoint

**Files:**
- Create: `back/Endpoints/MonsterEndpoints.cs`

- [ ] **Step 1: Create `back/Endpoints/MonsterEndpoints.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using MonsterHub.Api.Data;

namespace MonsterHub.Api.Endpoints;

public static class MonsterEndpoints
{
    public static void MapMonsterEndpoints(this WebApplication app)
    {
        app.MapGet("/api/monsters", async (AppDbContext db) =>
        {
            var monsters = await db.MonsterMappings
                .OrderBy(m => m.Id)
                .Select(m => new { m.Id, m.Name, m.Emoji })
                .ToListAsync();
            return Results.Ok(monsters);
        }).RequireAuthorization();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add back/Endpoints/MonsterEndpoints.cs
git commit -m "feat(back): add monsters endpoint"
```

---

## Task 15: Photo endpoints (analyze, publish, feed, explore, like)

**Files:**
- Create: `back/Endpoints/PhotoEndpoints.cs`

- [ ] **Step 1: Create `back/Endpoints/PhotoEndpoints.cs`**

```csharp
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MonsterHub.Api.Data;
using MonsterHub.Api.Models;
using MonsterHub.Api.Services;

namespace MonsterHub.Api.Endpoints;

public static class PhotoEndpoints
{
    public static void MapPhotoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/photos").RequireAuthorization();

        // POST /api/photos/analyze — detect Monster, do NOT publish
        group.MapPost("/analyze", async (
            HttpRequest request,
            IVisionService vision,
            MonsterMatchingService matching,
            AppDbContext db) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
                return Results.BadRequest("No file provided.");

            var file = request.Form.Files[0];
            var contentType = file.ContentType;
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var mistralOutput = await vision.AnalyzeAsync(ms.ToArray(), contentType);
            if (string.IsNullOrWhiteSpace(mistralOutput))
                return Results.UnprocessableEntity(new { error = "No Monster can detected." });

            var mappings = await db.MonsterMappings.ToListAsync();
            var match = matching.Match(mistralOutput, mappings);
            if (match == null)
                return Results.UnprocessableEntity(new { error = "No Monster can detected." });

            return Results.Ok(new Dtos.Photos.AnalyzeResultDto(match.Id, match.Name, match.Emoji));
        });

        // POST /api/photos — publish photo (after analyze confirmation)
        group.MapPost("/", async (
            HttpRequest request,
            ClaimsPrincipal principal,
            IStorageService storage,
            AppDbContext db) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
                return Results.BadRequest("No file provided.");
            if (!int.TryParse(request.Form["monsterId"], out var monsterId))
                return Results.BadRequest("monsterId is required.");

            var monster = await db.MonsterMappings.FindAsync(monsterId);
            if (monster == null) return Results.BadRequest("Invalid monsterId.");

            var file = request.Form.Files[0];
            var ext = Path.GetExtension(file.FileName).TrimStart('.');
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var relativePath = await storage.SaveAsync(ms.ToArray(), "photos", userId, ext);

            var photo = new Photo
            {
                UserId = userId,
                FilePath = relativePath,
                MonsterId = monsterId,
                CreatedAt = DateTime.UtcNow
            };
            db.Photos.Add(photo);
            await db.SaveChangesAsync();

            return Results.Created($"/api/photos/{photo.Id}",
                new { photoId = photo.Id, imageUrl = $"/api/images/{relativePath}" });
        });

        // DELETE /api/photos/{id}
        group.MapDelete("/{id:int}", async (
            int id,
            ClaimsPrincipal principal,
            IStorageService storage,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var photo = await db.Photos.FindAsync(id);
            if (photo == null) return Results.NotFound();
            if (photo.UserId != userId) return Results.Forbid();

            await storage.DeleteAsync(photo.FilePath);
            db.Photos.Remove(photo);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // POST /api/photos/{id}/like
        group.MapPost("/{id:int}/like", async (
            int id,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var photo = await db.Photos.FindAsync(id);
            if (photo == null) return Results.NotFound();

            var exists = await db.PhotoLikes.AnyAsync(l => l.PhotoId == id && l.UserId == userId);
            if (!exists)
            {
                db.PhotoLikes.Add(new PhotoLike { PhotoId = id, UserId = userId });
                photo.LikesCount++;
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { likesCount = photo.LikesCount });
        });

        // DELETE /api/photos/{id}/like
        group.MapDelete("/{id:int}/like", async (
            int id,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var photo = await db.Photos.FindAsync(id);
            if (photo == null) return Results.NotFound();

            var like = await db.PhotoLikes.FindAsync(id, userId);
            if (like != null)
            {
                db.PhotoLikes.Remove(like);
                photo.LikesCount = Math.Max(0, photo.LikesCount - 1);
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { likesCount = photo.LikesCount });
        });

        // GET /api/photos/feed
        group.MapGet("/feed", async (
            ClaimsPrincipal principal,
            AppDbContext db,
            int limit = 10,
            int? cursor = null) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var pageSize = Math.Clamp(limit, 1, 50);

            // Get IDs of accepted friends
            var friendIds = await db.UserFriendships
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId)
                    && f.Status == FriendshipStatus.Accepted)
                .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
                .ToListAsync();

            var query = db.Photos
                .Where(p => friendIds.Contains(p.UserId))
                .OrderByDescending(p => p.Id);

            if (cursor.HasValue)
                query = (IOrderedQueryable<Photo>)query.Where(p => p.Id < cursor.Value);

            var photos = await query
                .Take(pageSize)
                .Include(p => p.User)
                .Include(p => p.Monster)
                .ToListAsync();

            var photoIds = photos.Select(p => p.Id).ToList();
            var myLikes = await db.PhotoLikes
                .Where(l => l.UserId == userId && photoIds.Contains(l.PhotoId))
                .Select(l => l.PhotoId)
                .ToHashSetAsync();

            return Results.Ok(photos.Select(p => UserEndpoints.ToDto(p, myLikes.Contains(p.Id))));
        });

        // GET /api/photos/explore
        group.MapGet("/explore", async (
            ClaimsPrincipal principal,
            AppDbContext db,
            int offset = 0,
            int limit = 20,
            int seed = 1,
            string? monsterIds = null) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var pageSize = Math.Clamp(limit, 1, 50);

            var monsterIdList = monsterIds?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var i) ? (int?)i : null)
                .Where(i => i.HasValue)
                .Select(i => i!.Value)
                .ToList();

            var query = db.Photos.AsQueryable();

            if (monsterIdList?.Count > 0)
                query = query.Where(p => monsterIdList.Contains(p.MonsterId));

            // Deterministic pseudo-random order using seed
            var photos = await query
                .OrderBy(p => (p.Id * seed) % 999983) // large prime for distribution
                .Skip(offset)
                .Take(pageSize)
                .Include(p => p.User)
                .Include(p => p.Monster)
                .ToListAsync();

            var photoIds = photos.Select(p => p.Id).ToList();
            var myLikes = await db.PhotoLikes
                .Where(l => l.UserId == userId && photoIds.Contains(l.PhotoId))
                .Select(l => l.PhotoId)
                .ToHashSetAsync();

            return Results.Ok(photos.Select(p => UserEndpoints.ToDto(p, myLikes.Contains(p.Id))));
        });
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add back/Endpoints/PhotoEndpoints.cs
git commit -m "feat(back): add photo endpoints (analyze, publish, feed, explore, like)"
```

---

## Task 16: Friend endpoints

**Files:**
- Create: `back/Endpoints/FriendEndpoints.cs`

- [ ] **Step 1: Create `back/Endpoints/FriendEndpoints.cs`**

```csharp
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MonsterHub.Api.Data;
using MonsterHub.Api.Dtos.Friends;
using MonsterHub.Api.Models;

namespace MonsterHub.Api.Endpoints;

public static class FriendEndpoints
{
    public static void MapFriendEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/friends").RequireAuthorization();

        // GET /api/friends — accepted friends
        group.MapGet("/", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var friendships = await db.UserFriendships
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId)
                    && f.Status == FriendshipStatus.Accepted)
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .ToListAsync();

            var friends = friendships.Select(f =>
            {
                var friend = f.RequesterId == userId ? f.Addressee : f.Requester;
                return new FriendDto(friend.Id, friend.UserName!,
                    friend.ProfilePicturePath != null ? $"/api/images/{friend.ProfilePicturePath}" : null);
            });

            return Results.Ok(friends);
        });

        // GET /api/friends/requests — pending requests received
        group.MapGet("/requests", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var requests = await db.UserFriendships
                .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                .Include(f => f.Requester)
                .ToListAsync();

            return Results.Ok(requests.Select(f => new FriendDto(
                f.Requester.Id, f.Requester.UserName!,
                f.Requester.ProfilePicturePath != null
                    ? $"/api/images/{f.Requester.ProfilePicturePath}"
                    : null)));
        });

        // POST /api/friends/{userId} — send request
        group.MapPost("/{targetUserId}", async (
            string targetUserId,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (userId == targetUserId) return Results.BadRequest("Cannot add yourself.");

            var existing = await db.UserFriendships.FindAsync(userId, targetUserId)
                ?? await db.UserFriendships.FindAsync(targetUserId, userId);
            if (existing != null) return Results.Conflict("Friendship already exists or pending.");

            var target = await db.Users.FindAsync(targetUserId);
            if (target == null) return Results.NotFound();

            db.UserFriendships.Add(new UserFriendship
            {
                RequesterId = userId,
                AddresseeId = targetUserId,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });

            db.Notifications.Add(new Notification
            {
                RecipientId = targetUserId,
                Type = NotificationType.FriendRequest,
                RelatedEntityId = userId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return Results.Ok();
        });

        // PUT /api/friends/{requesterId}/accept
        group.MapPut("/{requesterId}/accept", async (
            string requesterId,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var friendship = await db.UserFriendships.FindAsync(requesterId, userId);
            if (friendship == null) return Results.NotFound();

            friendship.Status = FriendshipStatus.Accepted;
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        // DELETE /api/friends/{otherUserId} — decline or remove
        group.MapDelete("/{otherUserId}", async (
            string otherUserId,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var friendship = await db.UserFriendships.FindAsync(userId, otherUserId)
                ?? await db.UserFriendships.FindAsync(otherUserId, userId);

            if (friendship == null) return Results.NotFound();

            db.UserFriendships.Remove(friendship);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add back/Endpoints/FriendEndpoints.cs
git commit -m "feat(back): add friend endpoints (list, requests, send, accept, delete)"
```

---

## Task 17: Notification endpoint

**Files:**
- Create: `back/Endpoints/NotificationEndpoints.cs`

- [ ] **Step 1: Create `back/Endpoints/NotificationEndpoints.cs`**

```csharp
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MonsterHub.Api.Data;
using MonsterHub.Api.Dtos.Notifications;

namespace MonsterHub.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        app.MapGet("/api/notifications", async (
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var notifications = await db.Notifications
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Include(n => n.Recipient)
                .ToListAsync();

            // Intentional: GET with write side-effect for simplicity (spec §5)
            var unread = notifications.Where(n => !n.IsRead).ToList();
            if (unread.Count > 0)
            {
                unread.ForEach(n => n.IsRead = true);
                await db.SaveChangesAsync();
            }

            // Load related user info for each notification
            var senderIds = notifications.Select(n => n.RelatedEntityId).Distinct().ToList();
            var senders = await db.Users
                .Where(u => senderIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            return Results.Ok(notifications.Select(n =>
            {
                senders.TryGetValue(n.RelatedEntityId, out var sender);
                return new NotificationDto(
                    n.Id, n.Type, n.RelatedEntityId,
                    sender?.UserName ?? "Unknown",
                    sender?.ProfilePicturePath != null
                        ? $"/api/images/{sender.ProfilePicturePath}"
                        : null,
                    n.CreatedAt);
            }));
        }).RequireAuthorization();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add back/Endpoints/NotificationEndpoints.cs
git commit -m "feat(back): add notifications endpoint"
```

---

## Task 18: Full build verification

- [ ] **Step 1: Build and run all tests**

```bash
cd back && dotnet build
cd ../back.Tests && dotnet test
```
Expected: Build succeeded, all tests pass.

- [ ] **Step 2: Run the API and check Swagger**

```bash
cd back && dotnet run
# Open http://localhost:5000/swagger
```
Expected: All endpoints appear in Swagger UI.

- [ ] **Step 3: Run a smoke test sequence**

```
1. POST /api/auth/register → get token
2. POST /api/auth/login → get token
3. GET /api/users/me (with Bearer token) → get user profile
4. GET /api/monsters → list monsters
```

---

## Task 19: Dockerfile

**Files:**
- Create: `back/Dockerfile`

- [ ] **Step 1: Create `back/Dockerfile`**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["MonsterHub.Api.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MonsterHub.Api.dll"]
```

- [ ] **Step 2: Commit**

```bash
git add back/Dockerfile
git commit -m "feat(back): add Dockerfile for production build"
```
