# Migration Blazor → .NET 10 Minimal API + React Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remplacer l'application Blazor par un backend C# .NET 10 Minimal API (BACK/) et un frontend React TypeScript + Vite + MUI (FRONT/).

**Architecture:** Le BACK expose une API REST avec cookie auth HttpOnly, YARP proxy, et Scalar docs. Le FRONT est une SPA React qui appelle le BACK via Vite proxy en dev. Les deux projets sont indépendants et déployés séparément.

**Tech Stack:** .NET 10, Minimal API, YARP 2.x, Serilog, Scalar.AspNetCore / React 18, TypeScript, Vite, MUI v6, React Router v6

---

## PARTIE 1 — BACK

---

### Task 1: Scaffolder le projet .NET 10 Minimal API

**Files:**
- Create: `BACK/PortalCameras.csproj`
- Create: `BACK/Program.cs`
- Create: `BACK/appsettings.json`

**Step 1: Créer le projet**

```bash
cd WebApps/PortalCameras
dotnet new webapi -minimal --no-openapi -n PortalCameras -o BACK --framework net10.0
```

**Step 2: Supprimer les fichiers générés inutiles**

Supprimer `BACK/Controllers/` s'il existe. Supprimer tout exemple de WeatherForecast.

**Step 3: Ajouter les packages NuGet**

```bash
cd BACK
dotnet add package Yarp.ReverseProxy --version 2.3.0
dotnet add package Serilog.AspNetCore --version 9.0.0
dotnet add package Serilog.Sinks.File --version 6.0.0
dotnet add package Scalar.AspNetCore
dotnet add package Microsoft.AspNetCore.OpenApi
```

**Step 4: Remplacer appsettings.json**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AllowedOrigins": "http://localhost:5173",
  "WebHookUrl": "https://discord.com/api/webhooks/url",
  "BaseHistoryFolder": "C:\\Users\\antoi\\Documents\\Code\\MyUtils\\PortalCameras",
  "ApiDetectThingsUrl": "http://localhost:8000/detect-animal",
  "Cameras": [
    {
      "Name": "Toto",
      "Ip": "192.18.2.00",
      "Url": "https://google.com",
      "HistoryFolder": "TestFiles",
      "Group": "Maison"
    },
    {
      "Name": "Titi",
      "Ip": "192.18.2.01",
      "Url": "https://google.com",
      "HistoryFolder": "TestFiles",
      "Group": "Villy"
    },
    {
      "Name": "Tutu",
      "Ip": "192.18.2.02",
      "Url": "https://google.com",
      "HistoryFolder": "TestFiles",
      "Group": "Villy"
    }
  ],
  "Authentication": {
    "Password": "a"
  },
  "ReverseProxy": {
    "Routes": {
      "asnieres-route": {
        "ClusterId": "asnieres",
        "Match": { "Path": "/asnieres/{**catch-all}" },
        "AuthorizationPolicy": "RequireAuth"
      },
      "villy-route": {
        "ClusterId": "villy",
        "Match": { "Path": "/villy/{**catch-all}" },
        "AuthorizationPolicy": "RequireAuth"
      }
    },
    "Clusters": {
      "asnieres": {
        "Destinations": {
          "main": { "Address": "http://127.0.0.1:8889" }
        }
      },
      "villy": {
        "Destinations": {
          "main": { "Address": "http://127.0.0.1:8884" }
        }
      }
    }
  }
}
```

**Step 5: Commit**

```bash
git add BACK/
git commit -m "feat(back): scaffold .NET 10 Minimal API project"
```

---

### Task 2: Créer le modèle CameraConfig

**Files:**
- Create: `BACK/Models/CameraConfig.cs`

**Step 1: Créer le fichier**

```csharp
namespace PortalCameras.Models;

public class CameraConfig
{
    public string Name { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string HistoryFolder { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
}
```

**Step 2: Commit**

```bash
git add BACK/Models/
git commit -m "feat(back): add CameraConfig model"
```

---

### Task 3: Créer les services

**Files:**
- Create: `BACK/Services/PingService.cs`
- Create: `BACK/Services/IOService.cs`
- Create: `BACK/Services/DateService.cs`
- Create: `BACK/Services/DiscordService.cs`
- Create: `BACK/Services/DetectThingsService.cs`

**Step 1: PingService.cs**

```csharp
namespace PortalCameras.Services;

public class PingService
{
    private readonly ILogger<PingService> _logger;

    public PingService(ILogger<PingService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> PingAsync(string ip)
    {
        try
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = await ping.SendPingAsync(ip, timeout: 2000);
            return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Ping failed for {Ip}: {Message}", ip, ex.Message);
            return false;
        }
    }
}
```

**Step 2: IOService.cs**

```csharp
namespace PortalCameras.Services;

public class IOService
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public List<(string FileName, DateTime CreationDate)> ListFileNames(
        string directory, string pattern, bool descending)
    {
        if (!Directory.Exists(directory))
            return [];

        var files = Directory.GetFiles(directory, pattern)
            .Select(f => (
                FileName: Path.GetFileName(f),
                CreationDate: File.GetLastWriteTime(f)
            ));

        return descending
            ? files.OrderByDescending(f => f.CreationDate).ToList()
            : files.OrderBy(f => f.CreationDate).ToList();
    }
}
```

**Step 3: DateService.cs**

```csharp
namespace PortalCameras.Services;

public class DateService
{
    /// <summary>
    /// Retourne le chemin de dossier pour la date d'aujourd'hui moins `days` jours.
    /// Format : "YYYY/MM/DD"
    /// </summary>
    public string GetDateFolderDaysAgo(int days)
    {
        var date = DateTime.Now.AddDays(-days);
        return date.ToString("yyyy/MM/dd");
    }

    public string FormatTimeAgoFrench(DateTime date)
    {
        var diff = DateTime.Now - date;

        if (diff.TotalMinutes < 1) return "à l'instant";
        if (diff.TotalMinutes < 60) return $"il y a {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24) return $"il y a {(int)diff.TotalHours} h";
        if (diff.TotalDays < 7) return $"il y a {(int)diff.TotalDays} jour(s)";
        return date.ToString("dd/MM/yyyy");
    }
}
```

**Step 4: DiscordService.cs**

```csharp
namespace PortalCameras.Services;

public class DiscordService
{
    private readonly ILogger<DiscordService> _logger;
    private readonly string _webHookUrl;
    private readonly HttpClient _client = new();

    public DiscordService(IConfiguration config, ILogger<DiscordService> logger)
    {
        _logger = logger;
        _webHookUrl = config["WebHookUrl"] ?? string.Empty;
    }

    public async Task SendAsync(string message)
    {
        if (string.IsNullOrEmpty(_webHookUrl)) return;

        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new { content = message });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            await _client.PostAsync(_webHookUrl, content);
        }
        catch (Exception ex)
        {
            _logger.LogError("Discord webhook error: {Message}", ex.Message);
        }
    }
}
```

**Step 5: DetectThingsService.cs**

```csharp
namespace PortalCameras.Services;

public class DetectThingsService
{
    private readonly ILogger<DetectThingsService> _logger;
    private readonly HttpClient _client = new();

    public DetectThingsService(ILogger<DetectThingsService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool Detected, string Error)> DetectAsync(string imagePath, string apiUrl)
    {
        try
        {
            var fileBytes = await File.ReadAllBytesAsync(imagePath);
            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            form.Add(fileContent, "file", Path.GetFileName(imagePath));

            var response = await _client.PostAsync(apiUrl, form);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (false, $"API {response.StatusCode}: {err}");
            }

            var body = await response.Content.ReadAsStringAsync();
            var detected = System.Text.Json.JsonSerializer.Deserialize<bool>(body);
            return (detected, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError("DetectThings error: {Message}", ex.Message);
            return (false, ex.Message);
        }
    }
}
```

**Step 6: Commit**

```bash
git add BACK/Services/
git commit -m "feat(back): add PingService, IOService, DateService, DiscordService, DetectThingsService"
```

---

### Task 4: Créer les endpoints Auth

**Files:**
- Create: `BACK/Endpoints/AuthEndpoints.cs`

**Step 1: Créer AuthEndpoints.cs**

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace PortalCameras.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/login", async (HttpContext ctx, IConfiguration config) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var password = form["password"].ToString();
            var validPassword = config["Authentication:Password"] ?? string.Empty;

            if (password != validPassword)
                return Results.Unauthorized();

            var claims = new[] { new Claim(ClaimTypes.Name, "user") };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return Results.Ok(new { success = true });
        }).AllowAnonymous();

        app.MapPost("/api/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok(new { success = true });
        }).RequireAuthorization();

        app.MapGet("/api/me", (HttpContext ctx) =>
        {
            var isAuth = ctx.User.Identity?.IsAuthenticated ?? false;
            return Results.Ok(new { isAuthenticated = isAuth });
        }).AllowAnonymous();
    }
}
```

**Step 2: Commit**

```bash
git add BACK/Endpoints/AuthEndpoints.cs
git commit -m "feat(back): add auth endpoints (login, logout, me)"
```

---

### Task 5: Créer les endpoints Camera

**Files:**
- Create: `BACK/Endpoints/CameraEndpoints.cs`

**Step 1: Créer CameraEndpoints.cs**

```csharp
using Microsoft.Extensions.Options;
using PortalCameras.Models;
using PortalCameras.Services;

namespace PortalCameras.Endpoints;

public static class CameraEndpoints
{
    public static void MapCameraEndpoints(this WebApplication app)
    {
        // GET /api/cameras — liste les caméras (sans ping, le ping est séparé)
        app.MapGet("/api/cameras", (IOptions<List<CameraConfig>> cameras) =>
        {
            var result = cameras.Value.Select(c => new
            {
                c.Name,
                c.Ip,
                c.Url,
                c.Group
            });
            return Results.Ok(result);
        }).RequireAuthorization();

        // GET /api/cameras/{name}/ping — ping une caméra
        app.MapGet("/api/cameras/{name}/ping", async (
            string name,
            IOptions<List<CameraConfig>> cameras,
            PingService pingService) =>
        {
            var camera = cameras.Value.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (camera is null) return Results.NotFound();

            var isOnline = await pingService.PingAsync(camera.Ip);
            return Results.Ok(new { name = camera.Name, isOnline });
        }).RequireAuthorization();

        // GET /api/cameras/{name}/history?useAI=false — images récentes
        app.MapGet("/api/cameras/{name}/history", async (
            string name,
            bool? useAI,
            IOptions<List<CameraConfig>> cameras,
            IConfiguration config,
            IOService ioService,
            DateService dateService,
            DetectThingsService detectService,
            DiscordService discordService,
            ILogger<Program> logger) =>
        {
            var camera = cameras.Value.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (camera is null || string.IsNullOrEmpty(camera.HistoryFolder))
                return Results.NotFound();

            var baseFolder = config["BaseHistoryFolder"] ?? string.Empty;
            var apiDetectUrl = config["ApiDetectThingsUrl"] ?? string.Empty;
            var physicalBase = Path.Combine(baseFolder, camera.HistoryFolder);
            var urlBase = $"/camera-history/{camera.HistoryFolder.Replace("\\", "/")}";

            await discordService.SendAsync($"⚠️ Quelqu'un consulte l'historique de {name}.");

            var images = new List<object>();
            const int maxImages = 10;
            const int maxIterations = 200;
            const int maxApiFails = 5;

            int days = 0, found = 0, iterations = 0, apiFails = 0;

            while (found < maxImages && iterations < maxIterations)
            {
                var dateFolder = dateService.GetDateFolderDaysAgo(days);
                var physicalDir = Path.Combine(physicalBase, dateFolder);

                if (ioService.DirectoryExists(physicalDir))
                {
                    var files = ioService.ListFileNames(physicalDir, "*.jpg", true);

                    foreach (var (fileName, creationDate) in files)
                    {
                        if (found >= maxImages) break;

                        var imageUrl = $"{urlBase}/{dateFolder}/{fileName}";

                        if (useAI == true)
                        {
                            var physicalFile = Path.Combine(physicalDir, fileName);
                            var (detected, error) = await detectService.DetectAsync(physicalFile, apiDetectUrl);

                            if (!string.IsNullOrEmpty(error))
                            {
                                logger.LogError("Detect API error for {File}: {Error}", fileName, error);
                                apiFails++;
                                if (apiFails > maxApiFails) goto Done;
                                continue;
                            }

                            if (!detected) continue;
                        }

                        images.Add(new
                        {
                            url = imageUrl,
                            date = creationDate,
                            timeAgo = dateService.FormatTimeAgoFrench(creationDate)
                        });
                        found++;
                    }
                }

                days++;
                iterations++;
            }

            Done:
            return Results.Ok(images);
        }).RequireAuthorization();
    }
}
```

**Step 2: Commit**

```bash
git add BACK/Endpoints/CameraEndpoints.cs
git commit -m "feat(back): add camera endpoints (list, ping, history)"
```

---

### Task 6: Créer l'endpoint pour servir les images

**Files:**
- Create: `BACK/Endpoints/ImageEndpoints.cs`

**Step 1: Créer ImageEndpoints.cs**

```csharp
namespace PortalCameras.Endpoints;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this WebApplication app)
    {
        app.MapGet("/camera-history/{**path}", (HttpContext ctx, string path, IConfiguration config) =>
        {
            var baseFolder = config["BaseHistoryFolder"];
            if (string.IsNullOrEmpty(baseFolder)) return Results.NotFound();

            var fullPath = Path.Combine(baseFolder, path);
            if (!File.Exists(fullPath)) return Results.NotFound();

            var contentType = Path.GetExtension(fullPath).ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

            ctx.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            return Results.File(fullPath, contentType);
        }).RequireAuthorization();
    }
}
```

**Step 2: Commit**

```bash
git add BACK/Endpoints/ImageEndpoints.cs
git commit -m "feat(back): add image serving endpoint"
```

---

### Task 7: Configurer Program.cs

**Files:**
- Modify: `BACK/Program.cs`

**Step 1: Réécrire Program.cs complet**

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using PortalCameras.Endpoints;
using PortalCameras.Models;
using PortalCameras.Services;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Démarrage de PortalCameras API");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // CORS
    var allowedOrigins = builder.Configuration["AllowedOrigins"] ?? "http://localhost:5173";
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());
    });

    // Auth cookies
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/api/login";
            options.ExpireTimeSpan = TimeSpan.FromHours(24);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
        options.AddPolicy("RequireAuth", policy => policy.RequireAuthenticatedUser()));

    // YARP
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    // OpenAPI + Scalar
    builder.Services.AddOpenApi();

    // Services
    builder.Services.Configure<List<CameraConfig>>(builder.Configuration.GetSection("Cameras"));
    builder.Services.AddScoped<PingService>();
    builder.Services.AddScoped<IOService>();
    builder.Services.AddScoped<DateService>();
    builder.Services.AddScoped<DiscordService>();
    builder.Services.AddScoped<DetectThingsService>();

    var app = builder.Build();

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    // Scalar
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Endpoints
    app.MapAuthEndpoints();
    app.MapCameraEndpoints();
    app.MapImageEndpoints();

    // YARP
    app.MapReverseProxy().RequireAuthorization("RequireAuth");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application crash");
}
finally
{
    Log.CloseAndFlush();
}
```

**Step 2: Vérifier que le projet compile**

```bash
cd BACK
dotnet build
```

Expected: Build succeeded, 0 errors.

**Step 3: Commit**

```bash
git add BACK/Program.cs
git commit -m "feat(back): configure Program.cs with auth, CORS, YARP, Scalar, Serilog"
```

---

### Task 8: Test manuel du backend

**Step 1: Lancer le backend**

```bash
cd BACK
dotnet run
```

Expected: serveur démarré sur `http://localhost:5000`

**Step 2: Tester Scalar**

Ouvrir `http://localhost:5000/scalar` dans le navigateur.
Expected: Interface Scalar avec tous les endpoints listés.

**Step 3: Tester le login**

```bash
curl -c cookies.txt -X POST http://localhost:5000/api/login \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "password=a"
```

Expected: `{"success":true}` + cookie set.

**Step 4: Tester /api/cameras**

```bash
curl -b cookies.txt http://localhost:5000/api/cameras
```

Expected: JSON array des caméras.

**Step 5: Commit si tout fonctionne**

```bash
git commit --allow-empty -m "test(back): manual API validation passed"
```

---

## PARTIE 2 — FRONT

---

### Task 9: Scaffolder le projet Vite + React TypeScript

**Files:**
- Create: `FRONT/` (tout le projet)

**Step 1: Créer le projet Vite**

```bash
cd WebApps/PortalCameras
npm create vite@latest FRONT -- --template react-ts
cd FRONT
npm install
```

**Step 2: Installer les dépendances**

```bash
npm install @mui/material @mui/icons-material @emotion/react @emotion/styled
npm install react-router-dom
npm install @types/react-router-dom
```

**Step 3: Commit**

```bash
cd ..
git add FRONT/
git commit -m "feat(front): scaffold Vite React TypeScript project"
```

---

### Task 10: Configurer Vite proxy et TypeScript

**Files:**
- Modify: `FRONT/vite.config.ts`
- Modify: `FRONT/tsconfig.json`

**Step 1: vite.config.ts**

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
      },
      '/camera-history': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
```

**Step 2: Commit**

```bash
git add FRONT/vite.config.ts
git commit -m "feat(front): configure Vite proxy to BACK on port 5000"
```

---

### Task 11: Créer les types et le client API

**Files:**
- Create: `FRONT/src/types/camera.ts`
- Create: `FRONT/src/api/client.ts`

**Step 1: types/camera.ts**

```typescript
export interface CameraConfig {
  name: string;
  ip: string;
  url: string;
  group: string;
}

export interface CameraImage {
  url: string;
  date: string;
  timeAgo: string;
}

export interface PingResult {
  name: string;
  isOnline: boolean;
}
```

**Step 2: api/client.ts**

```typescript
const BASE = '';  // Vite proxy gère la redirection

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(BASE + path, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  });

  if (res.status === 401) {
    window.location.href = '/login';
    throw new Error('Unauthorized');
  }

  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
}

export const api = {
  login: (password: string) =>
    fetch('/api/login', {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: `password=${encodeURIComponent(password)}`,
    }),

  logout: () =>
    fetch('/api/logout', { method: 'POST', credentials: 'include' }),

  me: () => request<{ isAuthenticated: boolean }>('/api/me'),

  getCameras: () => request<import('../types/camera').CameraConfig[]>('/api/cameras'),

  pingCamera: (name: string) =>
    request<import('../types/camera').PingResult>(`/api/cameras/${name}/ping`),

  getCameraHistory: (name: string, useAI: boolean) =>
    request<import('../types/camera').CameraImage[]>(
      `/api/cameras/${name}/history?useAI=${useAI}`
    ),
};
```

**Step 3: Commit**

```bash
git add FRONT/src/types/ FRONT/src/api/
git commit -m "feat(front): add camera types and API client"
```

---

### Task 12: Créer le theme MUI et configurer App.tsx

**Files:**
- Modify: `FRONT/src/main.tsx`
- Create: `FRONT/src/App.tsx`

**Step 1: main.tsx**

```tsx
import React from 'react'
import ReactDOM from 'react-dom/client'
import { ThemeProvider, createTheme, CssBaseline } from '@mui/material'
import App from './App'

const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: { main: '#7c4dff' },
    secondary: { main: '#b39ddb' },
    background: { default: '#0f0f1a', paper: '#1a1a2e' },
  },
  shape: { borderRadius: 12 },
})

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <App />
    </ThemeProvider>
  </React.StrictMode>
)
```

**Step 2: App.tsx**

```tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { CircularProgress, Box } from '@mui/material'
import { api } from './api/client'
import LoginPage from './pages/LoginPage'
import HomePage from './pages/HomePage'
import CameraHistoryPage from './pages/CameraHistoryPage'
import AllStreamsPage from './pages/AllStreamsPage'

function RequireAuth({ children }: { children: React.ReactNode }) {
  const [auth, setAuth] = useState<boolean | null>(null)

  useEffect(() => {
    api.me().then(r => setAuth(r.isAuthenticated)).catch(() => setAuth(false))
  }, [])

  if (auth === null)
    return (
      <Box display="flex" justifyContent="center" alignItems="center" height="100vh">
        <CircularProgress />
      </Box>
    )

  return auth ? <>{children}</> : <Navigate to="/login" replace />
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<RequireAuth><HomePage /></RequireAuth>} />
        <Route path="/camerahistory/:name" element={<RequireAuth><CameraHistoryPage /></RequireAuth>} />
        <Route path="/streams" element={<RequireAuth><AllStreamsPage /></RequireAuth>} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
```

**Step 3: Commit**

```bash
git add FRONT/src/main.tsx FRONT/src/App.tsx
git commit -m "feat(front): add MUI theme, Router, and auth guard"
```

---

### Task 13: Créer LoginPage

**Files:**
- Create: `FRONT/src/pages/LoginPage.tsx`

**Step 1: LoginPage.tsx**

```tsx
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Container, Stack, Avatar, Typography, Paper,
  TextField, Button, Alert
} from '@mui/material'
import { Home, Lock, Login } from '@mui/icons-material'
import { api } from '../api/client'

export default function LoginPage() {
  const [password, setPassword] = useState('')
  const [error, setError] = useState(false)
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError(false)

    try {
      const res = await api.login(password)
      if (res.ok) {
        navigate('/')
      } else {
        setError(true)
      }
    } catch {
      setError(true)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Container maxWidth="xs" sx={{ py: 8 }}>
      <Stack spacing={4} alignItems="center">
        <Stack alignItems="center" spacing={1}>
          <Stack direction="row" alignItems="center" spacing={2}>
            <Avatar sx={{ bgcolor: 'primary.main', width: 48, height: 48 }}>
              <Home />
            </Avatar>
            <Typography variant="h5" fontWeight="bold">The house of tonito !</Typography>
          </Stack>
          <Typography variant="subtitle1" color="text.secondary">Connexion requise</Typography>
        </Stack>

        <Paper
          elevation={0}
          sx={{
            p: 4, width: '100%',
            background: 'rgba(255,255,255,0.05)',
            backdropFilter: 'blur(10px)',
            border: '1px solid rgba(255,255,255,0.1)'
          }}
        >
          <Stack spacing={3}>
            <Stack alignItems="center" spacing={1}>
              <Typography variant="h5" color="primary">Authentification</Typography>
              <Typography variant="body2" color="text.secondary" align="center">
                Veuillez entrer vos identifiants pour accéder aux caméras.
              </Typography>
            </Stack>

            {error && (
              <Alert severity="error">Identifiants incorrects. Veuillez réessayer.</Alert>
            )}

            <form onSubmit={handleSubmit}>
              <Stack spacing={3}>
                <TextField
                  label="Mot de passe"
                  type="password"
                  variant="outlined"
                  fullWidth
                  required
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  autoComplete="current-password"
                  InputProps={{ startAdornment: <Lock sx={{ mr: 1, color: 'text.secondary' }} /> }}
                />
                <Button
                  type="submit"
                  variant="contained"
                  size="large"
                  fullWidth
                  disabled={loading}
                  startIcon={<Login />}
                  sx={{ mt: 1 }}
                >
                  Se connecter
                </Button>
              </Stack>
            </form>
          </Stack>
        </Paper>

        <Typography variant="caption" color="text.secondary">© Tonito</Typography>
      </Stack>
    </Container>
  )
}
```

**Step 2: Commit**

```bash
git add FRONT/src/pages/LoginPage.tsx
git commit -m "feat(front): add LoginPage"
```

---

### Task 14: Créer HomePage

**Files:**
- Create: `FRONT/src/pages/HomePage.tsx`
- Create: `FRONT/src/components/CameraCard.tsx`

**Step 1: CameraCard.tsx**

```tsx
import { Stack, Paper, Typography, IconButton, Button, CircularProgress } from '@mui/material'
import { Videocam, Circle, PlayCircle, History } from '@mui/icons-material'
import { CameraConfig } from '../types/camera'

interface Props {
  camera: CameraConfig
  isOnline: boolean | null
  isLoading: boolean
  onHistory: (name: string) => void
}

export default function CameraCard({ camera, isOnline, isLoading, onHistory }: Props) {
  const statusColor = isLoading ? 'warning' : isOnline === true ? 'success' : isOnline === false ? 'error' : 'disabled'

  return (
    <Paper
      elevation={0}
      sx={{
        p: 2,
        background: 'rgba(255,255,255,0.05)',
        border: '1px solid rgba(255,255,255,0.08)',
        borderRadius: 2,
      }}
    >
      <Stack direction="row" alignItems="center" spacing={2}>
        <Videocam color="primary" />
        <Stack flex={1}>
          <Stack direction="row" alignItems="center" spacing={1}>
            <Typography variant="h6">{camera.name}</Typography>
            {isLoading
              ? <CircularProgress size={12} color="warning" />
              : <Circle sx={{ fontSize: 12 }} color={statusColor} />
            }
          </Stack>
        </Stack>
        <Stack spacing={0}>
          <Button
            size="small"
            color="secondary"
            startIcon={<PlayCircle />}
            href={camera.url}
            target="_blank"
          >
            Flux live
          </Button>
          <Button
            size="small"
            color="primary"
            startIcon={<History />}
            onClick={() => onHistory(camera.name)}
          >
            Historique
          </Button>
        </Stack>
      </Stack>
    </Paper>
  )
}
```

**Step 2: HomePage.tsx**

```tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Container, Stack, Avatar, Typography, Paper,
  Button, Divider, Alert
} from '@mui/material'
import { Home, GridView, Logout } from '@mui/icons-material'
import { api } from '../api/client'
import { CameraConfig } from '../types/camera'
import CameraCard from '../components/CameraCard'

export default function HomePage() {
  const navigate = useNavigate()
  const [cameras, setCameras] = useState<CameraConfig[]>([])
  const [pingResults, setPingResults] = useState<Record<string, boolean | null>>({})
  const [pingLoading, setPingLoading] = useState<Record<string, boolean>>({})

  useEffect(() => {
    api.getCameras().then(data => {
      setCameras(data)
      const loading: Record<string, boolean> = {}
      const results: Record<string, boolean | null> = {}
      data.forEach(c => { loading[c.name] = true; results[c.name] = null })
      setPingLoading(loading)
      setPingResults(results)

      // Ping chaque caméra en parallèle
      data.forEach(camera => {
        api.pingCamera(camera.name).then(r => {
          setPingResults(prev => ({ ...prev, [camera.name]: r.isOnline }))
          setPingLoading(prev => ({ ...prev, [camera.name]: false }))
        })
      })
    })
  }, [])

  const handleLogout = async () => {
    await api.logout()
    navigate('/login')
  }

  const groups = cameras.reduce<Record<string, CameraConfig[]>>((acc, cam) => {
    const key = cam.group || 'Autres'
    acc[key] = [...(acc[key] || []), cam]
    return acc
  }, {})

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Stack spacing={4} alignItems="center">
        <Stack alignItems="center" spacing={1}>
          <Stack direction="row" alignItems="center" spacing={2}>
            <Avatar sx={{ bgcolor: 'primary.main', width: 48, height: 48 }}>
              <Home />
            </Avatar>
            <Typography variant="h5" fontWeight="bold">The house of tonito !</Typography>
          </Stack>
          <Typography variant="subtitle1" color="text.secondary">Les caméras</Typography>
        </Stack>

        <Stack direction="row" spacing={2}>
          <Button variant="outlined" startIcon={<GridView />} size="small" onClick={() => navigate('/streams')}>
            Tous les flux
          </Button>
          <Button variant="outlined" color="error" startIcon={<Logout />} size="small" onClick={handleLogout}>
            Déconnexion
          </Button>
        </Stack>

        <Paper
          elevation={0}
          sx={{
            p: 3, width: '100%',
            background: 'rgba(255,255,255,0.05)',
            backdropFilter: 'blur(10px)',
            border: '1px solid rgba(255,255,255,0.1)'
          }}
        >
          <Stack spacing={3}>
            <Typography variant="body2" color="text.secondary" align="center">
              Système de surveillance de renards, pigeons, pies, chats et autres trucs très utiles.
            </Typography>

            {Object.entries(groups).map(([group, cams]) => (
              <Stack key={group} spacing={2}>
                <Stack direction="row" alignItems="center" spacing={2}>
                  <Divider sx={{ flex: 1 }} />
                  <Typography variant="subtitle2" color="text.secondary">{group}</Typography>
                  <Divider sx={{ flex: 1 }} />
                </Stack>
                {cams.map(cam => (
                  <CameraCard
                    key={cam.name}
                    camera={cam}
                    isOnline={pingResults[cam.name] ?? null}
                    isLoading={pingLoading[cam.name] ?? false}
                    onHistory={name => navigate(`/camerahistory/${name.toLowerCase()}`)}
                  />
                ))}
              </Stack>
            ))}

            <Alert severity="info" variant="outlined">
              <Typography variant="body2">Le chargement du flux peut prendre quelques secondes</Typography>
            </Alert>
          </Stack>
        </Paper>

        <Typography variant="caption" color="text.secondary">© Tonito</Typography>
      </Stack>
    </Container>
  )
}
```

**Step 3: Commit**

```bash
git add FRONT/src/pages/HomePage.tsx FRONT/src/components/CameraCard.tsx
git commit -m "feat(front): add HomePage with camera list and ping status"
```

---

### Task 15: Créer CameraHistoryPage

**Files:**
- Create: `FRONT/src/pages/CameraHistoryPage.tsx`

**Step 1: CameraHistoryPage.tsx**

```tsx
import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Container, Stack, Avatar, Typography, Paper,
  Button, CircularProgress, Alert, Switch,
  FormControlLabel, Box
} from '@mui/material'
import { History, ArrowBack } from '@mui/icons-material'
import { api } from '../api/client'
import { CameraImage } from '../types/camera'

const MAX_IMAGES = 10

export default function CameraHistoryPage() {
  const { name } = useParams<{ name: string }>()
  const navigate = useNavigate()
  const [images, setImages] = useState<CameraImage[]>([])
  const [loading, setLoading] = useState(true)
  const [useAI, setUseAI] = useState(false)

  const load = async (ai: boolean) => {
    setLoading(true)
    setImages([])
    try {
      const data = await api.getCameraHistory(name!, ai)
      setImages(data)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load(false) }, [name])

  const handleAIToggle = (checked: boolean) => {
    setUseAI(checked)
    load(checked)
  }

  return (
    <Container maxWidth="lg" sx={{ py: 8 }}>
      <Stack spacing={4} alignItems="center">
        <Stack alignItems="center" spacing={1}>
          <Stack direction="row" alignItems="center" spacing={2}>
            <Avatar sx={{ bgcolor: 'primary.main', width: 48, height: 48 }}>
              <History />
            </Avatar>
            <Typography variant="h5" fontWeight="bold">Historique - {name}</Typography>
          </Stack>
          <Typography variant="subtitle1" color="text.secondary">
            Les {MAX_IMAGES} dernières captures
          </Typography>
        </Stack>

        <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ width: '100%' }}>
          <Button
            variant="outlined"
            startIcon={<ArrowBack />}
            size="small"
            onClick={() => navigate('/')}
            sx={{ minWidth: 180 }}
          >
            Retour
          </Button>
          <Stack direction="row" alignItems="center" spacing={1}>
            <Typography variant="caption" color="text.secondary">
              Détection intelligente (expérimental)
            </Typography>
            <Switch
              size="small"
              checked={useAI}
              onChange={e => handleAIToggle(e.target.checked)}
              color="secondary"
            />
          </Stack>
        </Stack>

        <Paper
          elevation={0}
          sx={{
            p: 3, width: '100%',
            background: 'rgba(255,255,255,0.05)',
            border: '1px solid rgba(255,255,255,0.1)'
          }}
        >
          {loading && (
            <Stack alignItems="center" spacing={2} py={8}>
              <CircularProgress color="primary" />
              <Typography variant="body2" color="text.secondary">Chargement des images...</Typography>
            </Stack>
          )}

          {!loading && images.length === 0 && (
            <Alert severity="info" variant="outlined">
              Aucune image trouvée pour cette caméra.
            </Alert>
          )}

          {!loading && images.length > 0 && (
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: {
                  xs: '1fr',
                  sm: 'repeat(2, 1fr)',
                  md: 'repeat(3, 1fr)',
                  lg: 'repeat(auto-fill, minmax(400px, 1fr))',
                },
                gap: 2,
              }}
            >
              {images.map((img, i) => (
                <Paper
                  key={i}
                  elevation={2}
                  sx={{ p: 2, background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.08)' }}
                >
                  <Stack spacing={1} alignItems="center">
                    <Typography variant="caption" color="text.secondary">
                      {new Date(img.date).toLocaleString('fr-FR')} — {img.timeAgo}
                    </Typography>
                    <Box
                      component="a"
                      href={img.url}
                      target="_blank"
                      sx={{ display: 'block', width: '100%' }}
                    >
                      <Box
                        component="img"
                        src={img.url}
                        alt="Capture"
                        sx={{
                          width: '100%',
                          height: 300,
                          objectFit: 'cover',
                          borderRadius: 1,
                          transition: 'transform 0.2s',
                          '&:hover': { transform: 'scale(1.02)' },
                        }}
                      />
                    </Box>
                  </Stack>
                </Paper>
              ))}
            </Box>
          )}
        </Paper>

        <Typography variant="caption" color="text.secondary">
          {images.length} image(s) trouvée(s)
        </Typography>
      </Stack>
    </Container>
  )
}
```

**Step 2: Commit**

```bash
git add FRONT/src/pages/CameraHistoryPage.tsx
git commit -m "feat(front): add CameraHistoryPage with AI toggle"
```

---

### Task 16: Créer AllStreamsPage

**Files:**
- Create: `FRONT/src/pages/AllStreamsPage.tsx`

**Step 1: AllStreamsPage.tsx**

```tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Container, Stack, Avatar, Typography, Paper, Button, Box
} from '@mui/material'
import { GridView, ArrowBack, Videocam } from '@mui/icons-material'
import { api } from '../api/client'
import { CameraConfig } from '../types/camera'

export default function AllStreamsPage() {
  const navigate = useNavigate()
  const [cameras, setCameras] = useState<CameraConfig[]>([])

  useEffect(() => {
    api.getCameras().then(setCameras)
  }, [])

  return (
    <Container maxWidth="xl" sx={{ py: 8 }}>
      <Stack spacing={4} alignItems="center">
        <Stack alignItems="center" spacing={1}>
          <Stack direction="row" alignItems="center" spacing={2}>
            <Avatar sx={{ bgcolor: 'primary.main', width: 48, height: 48 }}>
              <GridView />
            </Avatar>
            <Typography variant="h5" fontWeight="bold">Tous les flux</Typography>
          </Stack>
        </Stack>

        <Button variant="outlined" startIcon={<ArrowBack />} size="small" onClick={() => navigate('/')}>
          Retour
        </Button>

        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(400px, 1fr))',
            gap: 2,
            width: '100%',
          }}
        >
          {cameras.map(camera => (
            <Paper
              key={camera.name}
              elevation={0}
              sx={{
                p: 2,
                background: 'rgba(255,255,255,0.05)',
                border: '1px solid rgba(255,255,255,0.1)',
                overflow: 'hidden',
              }}
            >
              <Stack spacing={1}>
                <Stack direction="row" alignItems="center" spacing={1}>
                  <Videocam fontSize="small" />
                  <Typography variant="subtitle1" fontWeight="bold">{camera.name}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {camera.group || 'Autres'}
                  </Typography>
                </Stack>
                <Box
                  component="iframe"
                  src={`${camera.url}_low`}
                  allowFullScreen
                  sx={{
                    width: '100%',
                    aspectRatio: '16/9',
                    border: 'none',
                    borderRadius: 1,
                    background: 'rgba(0,0,0,0.3)',
                  }}
                />
              </Stack>
            </Paper>
          ))}
        </Box>

        <Typography variant="caption" color="text.secondary">© Tonito</Typography>
      </Stack>
    </Container>
  )
}
```

**Step 2: Commit**

```bash
git add FRONT/src/pages/AllStreamsPage.tsx
git commit -m "feat(front): add AllStreamsPage with iframe grid"
```

---

### Task 17: Nettoyage et test final

**Step 1: Supprimer les fichiers de template Vite inutiles**

Supprimer ou vider :
- `FRONT/src/App.css`
- `FRONT/src/index.css` (garder uniquement `body { margin: 0; }`)
- `FRONT/src/assets/react.svg`
- `FRONT/public/vite.svg`

**Step 2: Mettre à jour index.html**

```html
<!doctype html>
<html lang="fr">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Cameras Monitor</title>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>
```

**Step 3: Lancer BACK et FRONT en même temps**

Terminal 1 :
```bash
cd BACK && dotnet run
```

Terminal 2 :
```bash
cd FRONT && npm run dev
```

**Step 4: Tester le flux complet**

1. Ouvrir `http://localhost:5173`
2. Vérifier la redirection vers `/login`
3. Se connecter avec le mot de passe `a`
4. Vérifier l'affichage de la HomePage avec les caméras
5. Cliquer "Historique" sur une caméra → vérifier CameraHistoryPage
6. Cliquer "Tous les flux" → vérifier AllStreamsPage
7. Cliquer "Déconnexion" → vérifier redirection vers `/login`

**Step 5: Commit final**

```bash
git add FRONT/
git commit -m "feat(front): finalize all pages and cleanup"
```
