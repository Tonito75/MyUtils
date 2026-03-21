# Monster Hub — Design Spec

**Date:** 2026-03-21
**Status:** Approved

---

## 1. Vue d'ensemble

Monster Hub est une application web de partage de photos de Monster Energy, style Instagram ultra-simplifié. Les utilisateurs publient des photos de canettes de Monster, voient les photos de leurs amis, explorent celles de tous les utilisateurs, et peuvent liker les publications. Une IA (Mistral Pixtral) identifie automatiquement le type de Monster présent sur chaque photo avant publication.

---

## 2. Architecture globale

```
┌─────────────┐     HTTPS      ┌───────────────────────────────┐
│   Browser   │ ◄────────────► │          Caddy (reverse proxy) │
└─────────────┘                └──────┬────────────────┬────────┘
                                      │                │
                              /api/*  │        /*      │
                                      ▼                ▼
                             ┌──────────────┐  ┌──────────────┐
                             │  Backend     │  │  Frontend    │
                             │  .NET 10     │  │  React/Vite  │
                             │  Minimal API │  │  MUI + nginx │
                             │  JWT Auth    │  └──────────────┘
                             └──────┬───────┘
                                    │
                     ┌──────────────┼──────────────┐
                     ▼              ▼               ▼
              ┌────────────┐ ┌──────────┐  ┌──────────────┐
              │ SQL Server │ │  NAS FTP │  │ Mistral API  │
              │  (Docker)  │ │ (LAN/TS) │  │  (internet)  │
              └────────────┘ └──────────┘  └──────────────┘
```

### Environnements

- **Dev** : front sur `localhost:5173` (Vite dev server), back sur `localhost:5000`. CORS autorise `localhost:5173`. Caddy n'est pas utilisé.
- **Prod** : tout sous Docker Compose. Caddy route `/api/*` vers le backend, `/*` vers le front (nginx statique). SQL Server dockerisé séparément par l'utilisateur.

---

## 3. Stack technique

| Couche | Technologie |
|---|---|
| Frontend | React + Vite, MUI (Material UI) |
| Frontend container | nginx (serve static build) |
| Backend | .NET 10, ASP.NET Minimal API |
| Auth | ASP.NET Identity + JWT Bearer |
| ORM | Entity Framework Core |
| Base de données | SQL Server |
| Stockage photos | NAS via FTP (FluentFTP) |
| Vision IA | Mistral Pixtral (`pixtral-large-2411`) |
| Reverse proxy (prod) | Caddy |

---

## 4. Modèle de données (Entity Framework)

### AppUser *(extends IdentityUser)*
```csharp
string? ProfilePicturePath
DateTime CreatedAt
```

### Photo
```csharp
int Id
string UserId          // FK → AppUser
AppUser User
string FilePath        // chemin sur le NAS
DateTime CreatedAt
int MonsterId          // FK → MonsterMapping
MonsterMapping Monster
int LikesCount         // dénormalisé pour perf
ICollection<PhotoLike> Likes
```

### PhotoLike
```csharp
int PhotoId            // PK composite
string UserId          // PK composite
Photo Photo
AppUser User
```

### UserFriendship
```csharp
string RequesterId     // PK composite + FK → AppUser
string AddresseeId     // PK composite + FK → AppUser
FriendshipStatus Status  // enum: Pending, Accepted
DateTime CreatedAt
AppUser Requester
AppUser Addressee
```

### Notification
```csharp
int Id
string RecipientId     // FK → AppUser
NotificationType Type  // enum: FriendRequest
int RelatedEntityId    // ex: UserId de l'expéditeur
bool IsRead
DateTime CreatedAt
AppUser Recipient
```

### MonsterMapping
```csharp
int Id
string Name            // ex: "Ultra White"
string Emoji           // ex: "⚪"
string Color           // ex: "#FFFFFF"
string KeywordsJson    // JSON array: ["ultra white", "white monster"]
```

### Seed data MonsterMapping

| Keywords | Name | Emoji |
|---|---|---|
| ultra white, white monster, the white monster | Ultra White | ⚪ |
| ultra paradise | Ultra Paradise | 🟢 |
| ultra black | Ultra Black | ⚫ |
| ultra fiesta | Ultra Fiesta | 🔵 |
| ultra red, red | Ultra Red | 🔴 |
| ultra violet, purple monster, the purple monster | Ultra Violet | 🟣 |
| ultra gold, gold | Ultra Gold | 🟡 |
| ultra blue, blue monster, the blue monster, blue | Ultra Blue | 🔵 |
| ultra watermelon | Ultra Watermelon | 🔴 |
| ultra rosé, ultra rose | Ultra Rosé | 🟣 |
| ultra strawberry, strawberry dreams | Ultra Strawberry Dreams | 🟣 |
| ultra fantasy | Ultra Fantasy | 🟣 |
| nitro | Energy Nitro | ⚫ |
| vr46, the doctor, valentino | VR46 | 🟡 |
| full throttle | Full Throttle | 🔵 |
| lando norris, lambo norris, norris | Lando Norris | 🟣 |
| original green, the original, original energy | The Original | 🟢 |
| aussie lemonade, aussie | Aussie Lemonade | 🟡 |
| pacific | Pacific Punch | 🟤 |
| mango loco, mango | Mango Loco | 🔵 |
| khaos | Khaos | 🟠 |
| ripper | Ripper | 🩷 |
| pipeline | Pipeline Punch | 🩷 |
| mixxd, mixd | Mixxd | 🟣 |

---

## 5. API Endpoints

### Auth
```
POST /api/auth/register    body: { username, email, password }  → JWT
POST /api/auth/login       body: { username, password }         → JWT
```

### Users
```
GET    /api/users/me                  → profil courant (id, username, email, avatarUrl)
PUT    /api/users/me                  → update email et/ou password
PUT    /api/users/me/avatar           → multipart: upload photo de profil
GET    /api/users/search?q=           → recherche par username (exclude self + amis existants)
GET    /api/users/{id}/photos         → photos paginées d'un utilisateur
```

### Photos
```
GET    /api/photos/feed               → photos des amis (cursor pagination, 10/page)
GET    /api/photos/explore            → toutes les photos (offset, 20/page, ?monsterIds=1,2)
POST   /api/photos/analyze            → multipart image → { monsterId, monsterName, emoji } ou 404
POST   /api/photos                    → multipart image + monsterId → publie
DELETE /api/photos/{id}               → supprime (owner uniquement)
POST   /api/photos/{id}/like          → like
DELETE /api/photos/{id}/like          → unlike
GET    /api/images/{*path}            → proxy FTP → bytes image (Cache-Control: max-age=86400)
```

### Friends
```
GET    /api/friends                         → liste amis acceptés
GET    /api/friends/requests                → demandes reçues en attente
POST   /api/friends/{userId}                → envoyer demande (crée Notification)
PUT    /api/friends/{userId}/accept         → accepter (Status → Accepted)
DELETE /api/friends/{userId}                → décliner ou supprimer amitié
```

### Notifications
```
GET    /api/notifications             → liste notifs (marque tout IsRead=true au passage)
```

### Monsters
```
GET    /api/monsters                  → liste complète MonsterMapping
```

---

## 6. Frontend — Pages et navigation

### Routes
```
/login        → LoginPage
/register     → RegisterPage
/             → FeedPage          [auth required]
/explore      → ExplorePage       [auth required]
/upload       → UploadPage        [auth required]
/friends      → FriendsPage       [auth required]
/profile      → MyProfilePage     [auth required]
```

### Header commun (toutes pages auth)
```
[Logo Monster Hub]  [Mon fil]  [Explorer]  [+]  ────  [🔔 badge]  [Avatar]
```
- Logo : lien vers `/`
- `+` : lien vers `/upload`
- `🔔` : ouvre MUI Popover, marque tout lu, liste les demandes d'amis avec boutons Accepter/Décliner inline
- `Avatar` : lien vers `/profile`, affiche initiales si pas de photo de profil

### FeedPage (`/`)
- Infinite scroll, 10 photos par page, cursor-based (id du dernier élément)
- Photos des amis acceptés uniquement, ordre antéchronologique
- Chaque carte : avatar + username en haut, photo, bouton ❤️ like (count), type de monster détecté (emoji + nom)

### ExplorePage (`/explore`)
- Mosaïque (CSS grid masonry ou MUI ImageList), 20 photos par page, infinite scroll
- Ordre pseudo-random (seed par session pour éviter doublons au scroll)
- Filtre multi-select par type de monster (Chips MUI, appel à `/api/monsters`)

### UploadPage (`/upload`)
**Étape 1 — Sélection**
- Dropzone ou bouton "Choisir une photo"
- Au choix : appel `POST /api/photos/analyze`
- Loader pendant l'analyse

**Étape 2a — Monster détecté**
- Affiche : `"Monster détecté : Ultra White ⚪"`
- Bouton **Publier** → `POST /api/photos` → redirect vers `/`
- Bouton **Reprendre** → retour étape 1

**Étape 2b — Rien détecté**
- Message : `"Aucune canette de Monster détectée sur cette photo."`
- Bouton **Réessayer** → retour étape 1

### FriendsPage (`/friends`)
- Onglet "Mes amis" : liste avec bouton Supprimer
- Onglet "Demandes reçues" : liste avec boutons Accepter / Décliner
- Barre de recherche : appel `/api/users/search?q=`, résultats avec bouton "Ajouter"

### MyProfilePage (`/profile`)
- En-tête : avatar (cliquable pour changer), username, nombre de photos, nombre d'amis
- Grille de mes photos (même style explore), infinite scroll
- Bouton "Modifier mon profil" → MUI Dialog avec champs email + password (optionnels)

### Auth (JWT)
- Token stocké en `localStorage`
- Axios interceptor : injecte `Authorization: Bearer <token>` sur chaque requête
- Si 401 reçu : redirect vers `/login`

---

## 7. Stockage des images

- **Upload** : image reçue par le backend en multipart → envoyée sur le NAS via `FTPService.Send(byte[], remotePath)`
- **Chemin NAS** : `/{photos|avatars}/{userId}/{guid}.{ext}`
- **Lecture** : endpoint `GET /api/images/{*path}` télécharge via `FTPService` et retourne les bytes avec le bon `Content-Type`
- **Cache** : `Cache-Control: public, max-age=86400` sur les réponses images
- **Evolution future** : pour passer à HTTP direct depuis le NAS, seul le `FilePath` stocké en base et l'endpoint `/api/images` sont à modifier

---

## 8. Intégration Mistral Vision

Réutilise le pattern de `MistralVisionService.cs` existant :
- Modèle : `pixtral-large-2411`
- Image envoyée en base64 dans le body JSON
- Le prompt demande de retourner le nom exact du produit (ou chaîne vide)
- Le nom retourné est matché contre les `KeywordsJson` de `MonsterMapping` (case-insensitive, contains)
- Si match : retourne `{ monsterId, monsterName, emoji }`
- Si pas de match ou réponse vide : retourne erreur 422

---

## 9. Dockerisation

### Structure
```
WebApps/PortalMonster/
├── back/                  → .NET 10 Minimal API
│   └── Dockerfile
├── front/                 → React + Vite
│   └── Dockerfile
├── docker-compose.yml     → back + front + caddy
└── Caddyfile
```

### docker-compose.yml (prod)
- Service `back` : .NET, expose port interne, variables d'env pour JWT secret, Mistral API key, SQL Server conn string, FTP config
- Service `front` : nginx, sert le build Vite statique
- Service `caddy` : reverse proxy, lit `Caddyfile`

### Caddyfile (prod)
```
monsterhub.example.com {
    reverse_proxy /api/* back:5000
    reverse_proxy * front:80
}
```

### Dev
Pas de Docker pour le dev. `dotnet run` + `npm run dev` suffisent.

---

## 10. Configuration (appsettings.json backend)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=MonsterHub;..."
  },
  "Jwt": {
    "Secret": "...",
    "Issuer": "monsterhub",
    "Audience": "monsterhub",
    "ExpiresInDays": 30
  },
  "Mistral": {
    "ApiKey": "..."
  },
  "Ftp": {
    "Host": "...",
    "Port": "21",
    "UserName": "...",
    "Password": "...",
    "BaseRemotePath": "/monsterhub"
  }
}
```

---

## 11. Points hors scope (v1)

- Commentaires sur les photos
- Stories / reels
- Messages privés
- Notifications push
- Modération de contenu
- Suppression de compte
