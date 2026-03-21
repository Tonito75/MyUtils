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
string RelatedEntityId // ex: UserId (string/GUID) de l'expéditeur de la demande d'ami
bool IsRead
DateTime CreatedAt
AppUser Recipient
```

### MonsterMapping
```csharp
int Id
string Name            // ex: "Ultra White"
string Emoji           // ex: "⚪"
string? Color          // ex: "#FFFFFF" — nullable, non utilisé en v1
string KeywordsJson    // JSON array: ["ultra white", "white monster"]
```

### Seed data MonsterMapping

Le matching keywords → MonsterMapping se fait par **premier match** : on itère les MonsterMapping dans l'ordre d'Id et on retourne le premier dont un keyword est contenu dans la réponse Mistral (case-insensitive).

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
POST /api/auth/register    body: { username, email, password }  → { token: "<jwt>" }
POST /api/auth/login       body: { username, password }         → { token: "<jwt>" }
```

### Users
```
GET    /api/users/me
  → { id, username, email, avatarUrl, photoCount, friendCount }

PUT    /api/users/me
  body: { currentPassword, newEmail?, newPassword? }
  → currentPassword toujours requis (même si seul newEmail est fourni)
  → si newPassword fourni : UserManager.ChangePasswordAsync(currentPassword, newPassword)
  → si newEmail fourni (sans newPassword) : vérifier currentPassword via
    UserManager.CheckPasswordAsync, puis UserManager.SetEmailAsync + SetUserNameAsync si besoin
  → retourne 400 si currentPassword incorrect
  → retourne { email, username } mis à jour

PUT    /api/users/me/avatar
  body: multipart/form-data (file)
  → retourne { avatarUrl } (nouvelle URL à jour pour mise à jour UI immédiate)

GET    /api/users/search?q=
  → recherche par username (exclude self + amis existants + demandes en cours)
  → retourne au maximum 20 résultats

GET    /api/users/{id}/photos?cursor=&limit=10
  → photos paginées d'un utilisateur
```

### Photos
```
GET    /api/photos/feed?cursor=&limit=10
  → photos des amis (cursor = id de la dernière photo reçue, absent pour première page)

GET    /api/photos/explore?offset=0&limit=20&seed=<int>&monsterIds=1,2
  → toutes les photos, ordre pseudo-random
  → seed: entier fourni par le client (généré au chargement initial de la page, stable
    pour toute la session de scroll), permet un ORDER BY NEWID() avec seed côté SQL Server
    ou un tri reproductible. En pratique: ORDER BY (Id * seed) % largeNumber.
  → monsterIds: filtre optionnel multi-valeur

POST   /api/photos/analyze
  body: multipart/form-data (file, max 10 MB)
  → 200 { monsterId, monsterName, emoji }  si canette détectée
  → 422  si aucune canette détectée ou réponse Mistral vide

POST   /api/photos
  body: multipart/form-data (file, max 10 MB) + monsterId: int
  → 201 { photoId, imageUrl }

DELETE /api/photos/{id}          → supprime (owner uniquement, 403 sinon)
POST   /api/photos/{id}/like     → 200 { likesCount }
DELETE /api/photos/{id}/like     → 200 { likesCount }

GET    /api/images/{*path}
  → proxy FTP → bytes image
  → Cache-Control: public, max-age=86400
  → {*path} correspond à la valeur de FilePath stockée en base
    (ex: photos/userId/guid.jpg)
  → le frontend construit l'URL: /api/images/{photo.filePath}
```

### Friends
```
GET    /api/friends                         → liste amis acceptés
GET    /api/friends/requests                → demandes reçues en attente
POST   /api/friends/{userId}                → envoyer demande (Status=Pending, crée Notification)
PUT    /api/friends/{userId}/accept         → accepter (Status → Accepted)
DELETE /api/friends/{userId}                → décliner ou supprimer amitié (supprime la ligne en base)
```
Note : un refus de demande **supprime la ligne** `UserFriendship` (pas de statut `Declined`).
L'expéditeur peut renvoyer une demande ultérieurement.

### Notifications
```
GET    /api/notifications
  → retourne la liste des notifs du user courant
  → marque TOUTES les notifs comme IsRead=true dans la même transaction
  → note: GET avec side-effect write est intentionnel ici (simplicité v1)
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
- `🔔` : ouvre MUI Popover, marque tout lu, liste les demandes d'amis avec boutons Accepter/Décliner inline. Si aucune notif : affiche "Aucune nouvelle notification"
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
  - Après envoi de la demande, le bouton "Ajouter" devient "En attente" (désactivé) sans rechargement de page

### MyProfilePage (`/profile`)
- En-tête : avatar (cliquable pour changer), username, nombre de photos, nombre d'amis
- Grille de mes photos (même style explore), infinite scroll
- Bouton "Modifier mon profil" → MUI Dialog avec champs email + password (optionnels)

### Auth (JWT)
- Token stocké en `localStorage`
- Axios interceptor : injecte `Authorization: Bearer <token>` sur chaque requête
- Si 401 reçu : redirect vers `/login`
- **Pas de refresh token en v1** : les tokens ont une durée de vie de 30 jours. À expiration, l'utilisateur est redirigé vers `/login`. C'est intentionnel pour simplifier l'implémentation.

---

## 7. Stockage des images

- **Upload** : image reçue par le backend en multipart → envoyée sur le NAS via `FTPService.Send(byte[], remotePath)`
- **Chemin NAS complet** : `{BaseRemotePath}/{photos|avatars}/{userId}/{guid}.{ext}` (ex: `/monsterhub/photos/abc123/photo.jpg`)
- **FilePath stocké en base** : chemin relatif à `BaseRemotePath`, ex: `photos/abc123/photo.jpg`
- **Construction URL frontend** : `/api/images/photos/abc123/photo.jpg` (le frontend concatène `/api/images/` + `photo.filePath`)
- **Lecture** : l'endpoint `GET /api/images/{*path}` préfixe `{*path}` avec `BaseRemotePath`, télécharge via `FTPService` et retourne les bytes avec le bon `Content-Type`
- **Cache** : `Cache-Control: public, max-age=86400` sur les réponses images
- **Taille max** : 10 MB par image (configuré dans ASP.NET `MaxRequestBodySize` et Caddyfile `request_body max_size 10MB`)
- **Evolution future** : pour passer à HTTP direct depuis le NAS, seul le service de stockage et la valeur de `FilePath` sont à modifier

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
    request_body max_size 10MB
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
