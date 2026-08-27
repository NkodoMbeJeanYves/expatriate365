# Expatriate365 — Ordre d'exécution du déploiement

## Prérequis (à faire avant tout)

- VPS Ubuntu 22.04 LTS avec accès SSH root
- DNS `acm365hub.poweryoursaas.com` pointant vers l'IP du VPS
- dotnet installé localement (machine de dev)
- Node.js / npm installés localement

---

## Étape 1 — Setup du VPS (une seule fois)

Depuis votre machine Windows (Git Bash / WSL), à la racine du projet :

```bash
scp scripts/setup-vps-expatriate.sh root@acm365hub.poweryoursaas.com:/tmp/
ssh root@acm365hub.poweryoursaas.com "sed -i 's/\r//' /tmp/setup-vps-expatriate.sh && sudo bash /tmp/setup-vps-expatriate.sh"
```

Le script demande interactivement (valeurs modifiables à chaque exécution) :

| Paramètre | Valeur par défaut | Stocké dans |
|---|---|---|
| **APP_NAME** | `expatriate365` | `/etc/<APP_NAME>/env` → `_DEPLOY_APP_NAME` |
| Domaine | `acm365hub.poweryoursaas.com` | `/etc/<APP_NAME>/env` → `FrontendBaseUrl` |
| Port interne API | `5001` | `/etc/<APP_NAME>/env` → `_DEPLOY_API_PORT` + `ASPNETCORE_URLS` |
| Nom de la base MySQL | `<APP_NAME>_prod` | `/etc/<APP_NAME>/env` → `ConnectionStrings__MySql` |
| DLL principale | `server.dll` | `/etc/<APP_NAME>/env` → `_DEPLOY_APP_DLL` |
| Channel .NET | `9.0` | Non stocké (vérification à chaque run) |
| Mot de passe root MySQL | *(toujours demandé)* | **Non stocké** |
| Mot de passe DB applicatif | *(saisi une fois)* | `/etc/<APP_NAME>/env` → `ConnectionStrings__MySql` |
| Clé JWT | *(auto-générée si vide)* | `/etc/<APP_NAME>/env` → `Jwt__Key` |
| SMTP (hôte, port, user, pass) | *(saisis une fois)* | `/etc/<APP_NAME>/env` → `Email__*` |

> **APP_NAME** conditionne tous les chemins : `/var/www/<APP_NAME>/`, `/etc/<APP_NAME>/env`, le service `<APP_NAME>-api`, le vhost Nginx, l'utilisateur système, et le script `/usr/local/bin/deploy-<APP_NAME>-api.sh`.

> **Ré-exécution** : si `/etc/<APP_NAME>/env` existe, les valeurs déjà définies sont proposées par défaut (Entrée = conserver). Seul le mot de passe root MySQL est redemandé à chaque fois.

> **Scripts de déploiement** : `APP_NAME` et `DOMAIN` sont déclarés en tête de `deploy-backend-expatriate.sh` et `deploy-frontend-expatriate.sh` — les mettre à jour si vous changez de cible.

---

## Étape 2 — Déploiement du backend

### Mode migrations EF Core (défaut)

```bash
bash scripts/deploy-backend-expatriate.sh
```

Ce script :
1. `dotnet publish` → `./publish/api/`
2. Crée `./publish/expatriate365-api.zip`
3. `scp` vers le VPS
4. Exécute `/usr/local/bin/deploy-expatriate365-api.sh` sur le VPS

### Mode schema SQL (alternative — si Pomelo/EF Core indisponible)

Utiliser ce mode si les migrations EF Core échouent (ex: Pomelo pas encore compatible .NET 10).

```bash
bash scripts/deploy-backend-expatriate.sh --schema-only
```

Ce script fait en plus :
- Génère `./publish/schema.sql` via `dotnet ef migrations script --idempotent`
- Transfère `schema.sql` vers le VPS (`/tmp/expatriate365-schema.sql`)
- Applique le schéma directement en MySQL au lieu d'`ef database update`

> `--idempotent` : le script SQL vérifie chaque migration avant de l'appliquer — safe pour les redéploiements successifs.

---

## Étape 3 — Déploiement du frontend

Depuis la racine du projet (machine de dev) :

```bash
bash scripts/deploy-frontend-expatriate.sh
```

Ce script :
1. `npm run build` dans `client/`
2. Archive `client/dist/client/browser/` → `/tmp/expatriate-frontend.tar.gz`
3. `scp` vers le VPS
4. `rsync` vers `/var/www/expatriate365/frontend/`

---

## Étape 4 — Seed (base vide uniquement — EFFACE toutes les données)

```bash
ssh root@acm365hub.poweryoursaas.com \
  "cd /var/www/expatriate365/api && \
   set -a && source <(grep -v '^#' /etc/expatriate365/env | grep -v '^_DEPLOY' | sed 's/\r//') && set +a && \
   dotnet server.dll seed && \
   systemctl restart expatriate365-api"
```

---

## Vérifications post-déploiement

```bash
# Santé de l'API
curl https://acm365hub.poweryoursaas.com/api/v1/health

# Logs en direct
ssh root@acm365hub.poweryoursaas.com "journalctl -u expatriate365-api -f"

# État des services
ssh root@acm365hub.poweryoursaas.com "systemctl status expatriate365-api nginx mysql"
```

---

## Commandes utiles sur le VPS

```bash
# Redémarrer l'API
systemctl restart expatriate365-api

# Voir les logs applicatifs
journalctl -u expatriate365-api -f

# Voir les logs Nginx
tail -f /var/log/nginx/error.log

# Tester la config Nginx
nginx -t && systemctl reload nginx

# Vérifier les variables d'environnement chargées
cat /etc/expatriate365/env
```

---

## Cohabitation avec school365

Les deux projets sont **totalement isolés** sur le même VPS :

| | school365 | expatriate365 |
|---|---|---|
| Port interne | `5000` | `5001` |
| Service systemd | `school365-api` | `expatriate365-api` |
| Répertoire web | `/var/www/school365/` | `/var/www/expatriate365/` |
| Secrets | `/etc/school365/env` | `/etc/expatriate365/env` |
| Base MySQL | `school365_prod` | `expatriate365_prod` |
| Utilisateur système | `school365` | `expatriate365` |
| Vhost Nginx | `school365` | `expatriate365` |
| Script de déploiement | `/usr/local/bin/deploy-api.sh` | `/usr/local/bin/deploy-expatriate-api.sh` |
