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

1. Va sur https://console.mistral.ai
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

1. Va sur https://console.anthropic.com
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

Le bot doit aussi avoir l'intent **Message Content** activé sur le portail développeur Discord https://discord.com/developers/applications (onglet Bot → Privileged Gateway Intents → Message Content Intent).

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

> **Sécurité :** Ne commite jamais `appsettings.json` avec de vraies clés API. Utilise `appsettings.Local.json` (déjà ignoré par `.gitignore`) pour tes vraies clés en local, ou des variables d'environnement en production.
