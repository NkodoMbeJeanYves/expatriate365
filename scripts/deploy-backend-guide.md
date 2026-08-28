# Expatriate365 — Guide de déploiement du backend

## Vue d'ensemble

Le déploiement du backend se fait en **deux phases** distinctes :

1. **`setup-vps-expatriate.sh`** — à exécuter **une seule fois** (ou lors d'un changement de configuration) pour préparer l'infrastructure du VPS.
2. **`deploy-backend-expatriate.sh`** — à exécuter **à chaque mise à jour** du code.

---

## Phase 1 — Préparation du VPS (`setup-vps-expatriate.sh`)

> À exécuter depuis votre machine Windows (Git Bash / WSL).

### Prérequis
- Accès SSH root au VPS (`acm365hub.poweryoursaas.com`)
- DNS du domaine pointant vers l'IP du VPS (vérifiez avant de lancer)

### Étape 1 — Transfert du script

```bash
scp scripts/setup-vps-expatriate.sh root@acm365hub.poweryoursaas.com:/tmp/
```

### Étape 2 — Connexion et exécution

```bash
ssh root@acm365hub.poweryoursaas.com
sed -i 's/\r//' /tmp/setup-vps-expatriate.sh
sudo bash /tmp/setup-vps-expatriate.sh
```

### Ce que fait le script (16 étapes automatiques)

Le script est **idempotent** : il détecte les composants déjà installés (MySQL, .NET, Nginx, UFW) et les ignore.

| Étape | Action |
|-------|--------|
| **0. Paramètres** | Saisie interactive : `APP_NAME`, domaine, port interne, nom BDD, DLL, version .NET |
| **1. Secrets** | Saisie masquée : mot de passe DB, root MySQL, clé JWT, super_admin, SMTP |
| **2. Système** | `apt update && apt upgrade` |
| **3. Outils** | `curl`, `wget`, `nginx`, `certbot`, `ufw`, `unzip` |
| **4. Chromium** | Dépendances pour PuppeteerSharp (génération PDF) |
| **5. Pare-feu** | UFW : `deny incoming`, `allow OpenSSH`, `allow Nginx Full` |
| **6. .NET** | Installation du runtime .NET (channel 9.0 ou 10.0 selon réponse) |
| **7. MySQL** | Création de la base `expatriate365_prod` + utilisateur dédié. Propose de conserver ou recréer si la base existe déjà |
| **8. Utilisateur système** | Création de l'utilisateur `expatriate365` (sans shell, sans home) |
| **9. Arborescence** | `/var/www/expatriate365/api/` + `frontend/` + sous-dossiers `wwwroot` |
| **10. Fichier de secrets** | `/etc/expatriate365/env` (chmod 600) — toutes les variables d'environnement de l'API |
| **11. Nginx** | Vhost HTTP→HTTPS + reverse proxy vers `http://127.0.0.1:5001` |
| **12. SSL** | Certificat Let's Encrypt via Certbot |
| **13. Service systemd** | `expatriate365-api.service` — démarre `dotnet server.dll`, charge `/etc/expatriate365/env` |
| **14. Logrotate** | Rotation des logs de l'API |
| **15. Script de déploiement** | Génère `/usr/local/bin/deploy-expatriate365-api.sh` (remplacé automatiquement par `deploy-backend-expatriate.sh`) |
| **16. Résumé** | Affiche les chemins clés, commandes utiles, et la clé JWT générée |

### Variables d'environnement écrites dans `/etc/expatriate365/env`

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://0.0.0.0:5001` |
| `ConnectionStrings__MySql` | Chaîne de connexion MySQL complète (single-quotée) |
| `Jwt__Key` / `Jwt__Secret` | Clé de signature JWT (single-quotée — peut contenir des `$`) |
| `Cors__AllowedOrigins` | `https://acm365hub.poweryoursaas.com` |
| `FrontendBaseUrl` | URL publique du frontend |
| `Seed__SuperAdminEmail` | Email du compte super_admin |
| `Seed__SuperAdminPassword` | Mot de passe du super_admin (single-quoté) |
| `Email__*` | Configuration SMTP |
| `FileStorage__*` | Chemins de stockage fichiers |
| `_DEPLOY_*` | Mémo interne pour les scripts (ignoré par l'API) |

> **Important :** Les valeurs pouvant contenir des `$` (JWT, mots de passe) sont entourées de single-quotes pour éviter l'expansion bash lors du `source` du fichier.

---

## Phase 2 — Déploiement du code (`deploy-backend-expatriate.sh`)

> À exécuter depuis la **racine du projet** (`C:\dev\expatriate365`) avec Git Bash / WSL.

### Modes disponibles

| Commande | Comportement |
|----------|-------------|
| `bash scripts/deploy-backend-expatriate.sh` | Migration EF (idempotente) + bootstrap (défaut) |
| `bash scripts/deploy-backend-expatriate.sh --reset` | Drop BDD + migration + bootstrap (rôles + super_admin) |
| `bash scripts/deploy-backend-expatriate.sh --seed` | Drop BDD + migration + données de démo ACM complètes |
| `bash scripts/deploy-backend-expatriate.sh --schema-only` | Application d'un dump SQL (sans EF Core) |

### Étapes exécutées

```
[Local]
  1. dotnet publish -c Release -r linux-x64
  2. Vérification : server.dll présent
  3. (--schema-only) dotnet ef migrations script --idempotent
  4. zip publish/api/ → publish/expatriate365-api.zip
  5. scp expatriate365-api.zip → VPS:/tmp/
  6. sed substitution __APP_NAME__ / __APP_DLL__ dans vps-deploy-api.sh
     scp résultat → VPS:/usr/local/bin/deploy-expatriate365-api.sh
  7. ssh "bash /usr/local/bin/deploy-expatriate365-api.sh [--reset|--seed|--schema-only]"

[Sur le VPS — /usr/local/bin/deploy-expatriate365-api.sh]
  1. systemctl stop expatriate365-api
  2. Sauvegarde tar.gz de /var/www/expatriate365/api/
  3. unzip + rsync (préserve uploads, logos, photos, documents, logs)
  4. chown + chmod
  5. source /etc/expatriate365/env (sans afficher les secrets)
  6. Selon le mode :
       --reset   → dotnet server.dll --reset
                   (EnsureDeletedAsync + MigrateAsync + BootstrapAsync)
       --seed    → dotnet server.dll --seed
                   (EnsureDeletedAsync + MigrateAsync + ResetAndSeedAsync)
       défaut    → (le service démarre, MigrateAsync au démarrage)
  7. systemctl start expatriate365-api
```

### Comportement de la base de données au démarrage de l'API

| Mode | BDD | Résultat |
|------|-----|---------|
| Démarrage normal | Conservée | `MigrateAsync()` — applique les migrations en attente, ne fait rien si à jour |
| `--reset` | **Drop + recréation** | Schéma vide + rôles + super_admin uniquement |
| `--seed` | **Drop + recréation** | Schéma vide + rôles + super_admin + données de démo ACM |

> **Déploiement initial recommandé :**
> ```bash
> bash scripts/deploy-backend-expatriate.sh --seed
> ```

---

## Commandes utiles sur le VPS

```bash
# Statut du service
systemctl status expatriate365-api

# Logs en temps réel
journalctl -u expatriate365-api -f

# Logs des 100 dernières lignes
journalctl -u expatriate365-api -n 100 --no-pager

# Variables d'environnement chargées par systemd
systemctl show expatriate365-api --property=Environment

# Port d'écoute
ss -tlnp | grep dotnet

# Tester l'API directement (sans Nginx)
curl http://localhost:5001/health
```

---

## Chemins importants sur le VPS

| Chemin | Contenu |
|--------|---------|
| `/var/www/expatriate365/api/` | Binaires de l'API |
| `/var/www/expatriate365/frontend/` | Fichiers Angular buildés |
| `/etc/expatriate365/env` | Variables d'environnement (chmod 600) |
| `/etc/systemd/system/expatriate365-api.service` | Définition du service systemd |
| `/etc/nginx/sites-available/expatriate365` | Configuration Nginx |
| `/usr/local/bin/deploy-expatriate365-api.sh` | Script de déploiement VPS (mis à jour automatiquement) |
| `/var/backups/expatriate365/api/` | Sauvegardes tar.gz avant chaque déploiement |
| `/var/log/nginx/` | Logs Nginx (access + error) |
