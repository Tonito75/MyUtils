# Design — Migration Blazor → .NET 10 Minimal API + React TypeScript

**Date :** 2026-03-14
**Statut :** Approuvé

---

## Contexte

L'application Blazor existante (PortalCameras) est migrée vers une architecture découplée :
- **BACK/** : C# .NET 10 Minimal API
- **FRONT/** : React TypeScript + Vite + MUI v6

---

## Architecture globale

```
FRONT (Vite + React TS + MUI)          BACK (.NET 10 Minimal API)
  http://localhost:5173          <-->    http://localhost:5000
                                              │
                                    ┌─────────┴──────────┐
                                    │   Minimal API       │
                                    │   Cookie Auth       │
                                    │   YARP Proxy        │
                                    │   Scalar docs       │
                                    └─────────────────────┘
                                              │
                               ┌──────────────┼──────────────┐
                            Caméras IP    NAS (images)   API IA détection
```

- En développement, Vite proxie `/api` vers `localhost:5000` (pas de CORS)
- En production, FRONT buildé servi séparément (nginx, IIS…)
- CORS configuré sur le BACK pour autoriser l'origine du FRONT

---

## Backend (BACK/)

### Structure
```
BACK/
├── Program.cs
├── PortalCameras.csproj
├── appsettings.json
├── Models/
│   └── CameraConfig.cs
├── Services/
│   ├── PingService.cs
│   ├── DetectThingsService.cs
│   ├── DiscordService.cs
│   ├── IOService.cs
│   └── DateService.cs
└── Endpoints/
    ├── AuthEndpoints.cs
    ├── CameraEndpoints.cs
    └── ImageEndpoints.cs
```

### Endpoints

| Méthode | Route | Auth | Description |
|---|---|---|---|
| `POST` | `/api/login` | Public | Login → cookie HttpOnly |
| `GET` | `/api/logout` | Auth | Logout, supprime le cookie |
| `GET` | `/api/cameras` | Auth | Liste caméras + statut ping |
| `GET` | `/api/cameras/{name}/history` | Auth | Images récentes (`?useAI=true`) |
| `GET` | `/camera-history/{**path}` | Auth | Sert les fichiers images du disque |
| `*` | `/asnieres/{**}`, `/villy/{**}` | Auth | YARP reverse proxy |
| `GET` | `/scalar` | Public | Documentation Scalar |

### Authentification
- Cookie HttpOnly, SameSite=Strict, Secure
- Mot de passe seul (pas de username), configuré dans `appsettings.json`
- Durée de session : 24h avec sliding expiration

### Packages
- `Yarp.ReverseProxy`
- `Serilog.AspNetCore` + `Serilog.Sinks.File`
- `Scalar.AspNetCore`

### appsettings.json (structure conservée)
```json
{
  "BaseHistoryFolder": "...",
  "ApiDetectThingsUrl": "http://localhost:8000/detect-animal",
  "WebHookUrl": "https://discord.com/api/webhooks/...",
  "Cameras": [...],
  "Authentication": { "Password": "..." },
  "ReverseProxy": { "Routes": {...}, "Clusters": {...} }
}
```

---

## Frontend (FRONT/)

### Structure
```
FRONT/
├── index.html
├── vite.config.ts
├── package.json
├── tsconfig.json
└── src/
    ├── main.tsx
    ├── App.tsx
    ├── api/
    │   └── client.ts
    ├── pages/
    │   ├── LoginPage.tsx
    │   ├── HomePage.tsx
    │   ├── CameraHistoryPage.tsx
    │   └── AllStreamsPage.tsx
    ├── components/
    │   └── CameraCard.tsx
    └── types/
        └── camera.ts
```

### Points clés
- Vite proxy `/api` → `localhost:5000` en développement
- `fetch` avec `credentials: 'include'` sur toutes les requêtes
- React Router v6 pour la navigation
- Auth guard : non authentifié → redirect `/login`
- MUI v6 pour les composants (design fidèle au Blazor existant)

### Pages
- **LoginPage** : formulaire mot de passe, appel `POST /api/login`
- **HomePage** : liste des caméras groupées, statut ping, liens flux live + historique
- **CameraHistoryPage** : grille d'images récentes, toggle IA
- **AllStreamsPage** : grille d'iframes pour tous les flux

---

## Décisions techniques

| Décision | Choix | Raison |
|---|---|---|
| Auth | Cookie HttpOnly | Plus sécurisé qu'un JWT en localStorage, protégé contre XSS |
| Dépendance Common | Aucune | Services réimplémentés directement dans BACK pour indépendance |
| Déploiement | Séparé | BACK port 5000, FRONT port 5173 (dev) |
| API docs | Scalar | Moderne, remplace Swagger UI |
| UI Framework | MUI v6 | Équivalent Material de MudBlazor |
