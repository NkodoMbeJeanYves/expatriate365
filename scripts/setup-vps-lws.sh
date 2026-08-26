#!/usr/bin/env bash
# =============================================================================
# School365 — Script de configuration VPS LWS
# Cible : Ubuntu 22.04 LTS
# Usage : sudo bash setup-vps-lws.sh
# =============================================================================
set -euo pipefail

# ─── Couleurs ─────────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

log()     { echo -e "${GREEN}[✓]${NC} $*"; }
info()    { echo -e "${CYAN}[→]${NC} $*"; }
warn()    { echo -e "${YELLOW}[!]${NC} $*"; }
error()   { echo -e "${RED}[✗]${NC} $*"; exit 1; }
section() {
    _STEP_NAME="$*"
    local ts; ts=$(date '+%H:%M:%S')
    echo -e "\n${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${CYAN}  $_STEP_NAME${NC}  ${YELLOW}[${ts}]${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
}

# ─── Suivi des étapes — affiche l'étape courante en cas d'erreur ──────────────
_STEP_NAME="initialisation"
trap 'echo -e "\n${RED}[✗] ÉCHEC — ${_STEP_NAME} (ligne ${LINENO})${NC}" >&2' ERR

# ─── Vérification root ────────────────────────────────────────────────────────
[[ $EUID -ne 0 ]] && error "Ce script doit être exécuté en root : sudo bash $0"

# =============================================================================
# COLLECTE DES PARAMÈTRES
# =============================================================================
section "Configuration School365 — Saisie des paramètres"
echo ""

# Lit une valeur depuis /etc/school365/env si le fichier existe
_env_get() {
    [[ -f /etc/school365/env ]] && grep -E "^${1}=" /etc/school365/env 2>/dev/null | head -1 | cut -d= -f2- || true
}

if [[ -f /etc/school365/env ]]; then
    info "Fichier /etc/school365/env détecté — valeurs actuelles proposées par défaut (Entrée = conserver)."
    echo ""
fi

# ── DOMAINE ──────────────────────────────────────────────────────────────────
info "Votre nom de domaine public (DNS déjà configuré vers ce VPS)"
info "  ex: school365.monecole.fr  ou  app.lycee-victor-hugo.fr"
_SAVED_DOMAIN=$(_env_get "FrontendBaseUrl" | sed 's|https://||')
if [[ -n "$_SAVED_DOMAIN" ]]; then
    info "Valeur actuelle : $_SAVED_DOMAIN"
    read -rp "Nom de domaine [Entrée = conserver] : " DOMAIN
    DOMAIN=${DOMAIN:-$_SAVED_DOMAIN}
else
    read -rp "Nom de domaine : " DOMAIN
fi
[[ -z "$DOMAIN" ]] && error "Le domaine est obligatoire."

# ── MOT DE PASSE DB (utilisateur applicatif) ─────────────────────────────────
echo ""
info "Mot de passe pour l'utilisateur MySQL 'school365_user' (≥ 12 caractères)"
_SAVED_DB_PASS=$(_env_get "ConnectionStrings__MySql" | sed 's/.*Password=\([^;]*\).*/\1/')
if [[ -n "$_SAVED_DB_PASS" ]]; then
    info "Valeur actuelle : (déjà définie — masquée)"
    read -rp "Mot de passe MySQL school365_user [Entrée = conserver] : " -s DB_PASSWORD; echo
    DB_PASSWORD=${DB_PASSWORD:-$_SAVED_DB_PASS}
else
    read -rp "Mot de passe MySQL school365_user : " -s DB_PASSWORD; echo
fi
[[ ${#DB_PASSWORD} -lt 12 ]] && error "Le mot de passe MySQL doit faire au moins 12 caractères."

# ── MOT DE PASSE DB (root) ────────────────────────────────────────────────────
echo ""
info "Mot de passe pour le compte root MySQL (≥ 12 caractères)"
# Non stocké dans l'env — l'utilisateur doit saisir le même mot de passe qu'à la première installation
if [[ -f /etc/school365/env ]]; then
    warn "Non stocké dans l'env — saisir le même mot de passe que lors de la première installation."
fi
read -rp "Mot de passe root MySQL : " -s MYSQL_ROOT_PASSWORD; echo
[[ ${#MYSQL_ROOT_PASSWORD} -lt 12 ]] && error "Le mot de passe root MySQL doit faire au moins 12 caractères."

# ── CLÉ JWT ───────────────────────────────────────────────────────────────────
echo ""
info "Clé secrète JWT (≥ 32 caractères) — Entrée pour auto-génération"
_SAVED_JWT=$(_env_get "Jwt__Key")
if [[ -n "$_SAVED_JWT" ]]; then
    info "Valeur actuelle : (déjà définie — masquée)"
    read -rp "Clé JWT [Entrée = conserver] : " -s JWT_KEY; echo
    JWT_KEY=${JWT_KEY:-$_SAVED_JWT}
else
    read -rp "Clé JWT (Entrée = auto-génération) : " -s JWT_KEY; echo
    if [[ -z "$JWT_KEY" ]]; then
        JWT_KEY=$(openssl rand -base64 64 | tr -d '\n')
        log "Clé JWT générée automatiquement."
    fi
fi
[[ ${#JWT_KEY} -lt 32 ]] && error "La clé JWT doit faire au moins 32 caractères."

# ── SMTP ──────────────────────────────────────────────────────────────────────
echo ""
info "Adresse email expéditrice (aussi utilisée pour Let's Encrypt)"
_SAVED_SMTP_FROM=$(_env_get "Email__FromAddress")
if [[ -n "$_SAVED_SMTP_FROM" ]]; then
    info "Valeur actuelle : $_SAVED_SMTP_FROM"
    read -rp "Adresse email SMTP (From) [Entrée = conserver] : " SMTP_FROM
    SMTP_FROM=${SMTP_FROM:-$_SAVED_SMTP_FROM}
else
    read -rp "Adresse email SMTP (From) : " SMTP_FROM
fi

echo ""
info "Serveur SMTP  →  Gmail: smtp.gmail.com | OVH/LWS: ssl0.ovh.net | Office365: smtp.office365.com"
_SAVED_SMTP_HOST=$(_env_get "Email__SmtpHost")
if [[ -n "$_SAVED_SMTP_HOST" ]]; then
    info "Valeur actuelle : $_SAVED_SMTP_HOST"
    read -rp "Hôte SMTP [Entrée = conserver] : " SMTP_HOST
    SMTP_HOST=${SMTP_HOST:-$_SAVED_SMTP_HOST}
else
    read -rp "Hôte SMTP : " SMTP_HOST
fi

echo ""
info "Port SMTP  →  587 = STARTTLS (recommandé) | 465 = SSL/TLS"
_SAVED_SMTP_PORT=$(_env_get "Email__SmtpPort")
if [[ -n "$_SAVED_SMTP_PORT" ]]; then
    info "Valeur actuelle : $_SAVED_SMTP_PORT"
    read -rp "Port SMTP [Entrée = conserver] : " SMTP_PORT
    SMTP_PORT=${SMTP_PORT:-$_SAVED_SMTP_PORT}
else
    read -rp "Port SMTP [587] : " SMTP_PORT
    SMTP_PORT=${SMTP_PORT:-587}
fi

echo ""
info "Identifiant de connexion SMTP (généralement votre adresse email complète)"
_SAVED_SMTP_USER=$(_env_get "Email__Username")
if [[ -n "$_SAVED_SMTP_USER" ]]; then
    info "Valeur actuelle : $_SAVED_SMTP_USER"
    read -rp "Identifiant SMTP [Entrée = conserver] : " SMTP_USER
    SMTP_USER=${SMTP_USER:-$_SAVED_SMTP_USER}
else
    read -rp "Identifiant SMTP : " SMTP_USER
fi

echo ""
info "Mot de passe SMTP / App Password"
info "  Gmail → créez un App Password sur https://myaccount.google.com/apppasswords"
_SAVED_SMTP_PASS=$(_env_get "Email__Password")
if [[ -n "$_SAVED_SMTP_PASS" ]]; then
    info "Valeur actuelle : (déjà définie — masquée)"
    read -rp "Mot de passe SMTP [Entrée = conserver] : " -s SMTP_PASSWORD; echo
    SMTP_PASSWORD=${SMTP_PASSWORD:-$_SAVED_SMTP_PASS}
else
    read -rp "Mot de passe SMTP / App Password : " -s SMTP_PASSWORD; echo
fi

# Résumé
echo ""
section "Récapitulatif"
echo "  Domaine      : $DOMAIN"
echo "  DB User      : school365_user"
echo "  DB Name      : school365_prod"
echo "  SMTP Host    : $SMTP_HOST:$SMTP_PORT"
echo "  SMTP From    : $SMTP_FROM"
echo ""
read -rp "Confirmer et lancer l'installation ? (oui/non) : " CONFIRM
[[ "$CONFIRM" != "oui" ]] && { warn "Installation annulée."; exit 0; }

# =============================================================================
# 1. MISE À JOUR SYSTÈME
# =============================================================================
section "1. Mise à jour du système"
apt update -y
DEBIAN_FRONTEND=noninteractive apt upgrade -y
log "Système mis à jour."

# =============================================================================
# 2. OUTILS ESSENTIELS
# =============================================================================
section "2. Installation des outils essentiels"
DEBIAN_FRONTEND=noninteractive apt install -y \
    curl wget git unzip nginx certbot python3-certbot-nginx ufw \
    software-properties-common apt-transport-https gnupg lsb-release
log "Outils installés."

# =============================================================================
# 3. DÉPENDANCES PUPPETEERCHARP / CHROMIUM
# =============================================================================
section "3. Dépendances Chromium (PuppeteerSharp)"
DEBIAN_FRONTEND=noninteractive apt install -y \
    ca-certificates fonts-liberation libasound2 libatk-bridge2.0-0 libatk1.0-0 \
    libc6 libcairo2 libcups2 libdbus-1-3 libexpat1 libfontconfig1 libgbm1 \
    libgcc1 libglib2.0-0 libgtk-3-0 libnspr4 libnss3 libpango-1.0-0 \
    libpangocairo-1.0-0 libstdc++6 libx11-6 libx11-xcb1 libxcb1 \
    libxcomposite1 libxcursor1 libxdamage1 libxext6 libxfixes3 libxi6 \
    libxrandr2 libxrender1 libxss1 libxtst6 wget xdg-utils
log "Dépendances Chromium installées."

# =============================================================================
# 4. PARE-FEU UFW
# =============================================================================
section "4. Configuration UFW (pare-feu)"
ufw --force reset
ufw default deny incoming
ufw default allow outgoing
ufw allow OpenSSH
ufw allow 'Nginx Full'
ufw --force enable
log "Pare-feu configuré (SSH + HTTP + HTTPS autorisés)."

# =============================================================================
# 5. INSTALLATION .NET 10
# =============================================================================
section "5. Installation .NET 10"
DOTNET_INSTALL_DIR="/opt/dotnet"
mkdir -p "$DOTNET_INSTALL_DIR"

wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --version latest --channel 10.0 --install-dir "$DOTNET_INSTALL_DIR"

# Rendre dotnet disponible system-wide
ln -sf "$DOTNET_INSTALL_DIR/dotnet" /usr/local/bin/dotnet

# Variables pour l'utilisateur courant
if ! grep -q "DOTNET_ROOT" /etc/environment 2>/dev/null; then
    echo "DOTNET_ROOT=$DOTNET_INSTALL_DIR" >> /etc/environment
fi

dotnet --version
log ".NET 10 installé dans $DOTNET_INSTALL_DIR."

# =============================================================================
# 6. MYSQL 8
# =============================================================================
section "6. Installation et configuration MySQL 8"
DEBIAN_FRONTEND=noninteractive apt install -y mysql-server

# Activer et démarrer MySQL
systemctl enable mysql
systemctl start mysql

# Configuration sécurisée non-interactive
# Sur Ubuntu 22.04, root utilise auth_socket ; fallback si déjà sécurisé (ré-exécution)
_mysql_root() {
    if mysql --user=root --execute="SELECT 1;" 2>/dev/null; then
        mysql --user=root "$@"
    elif mysql --user=root --password="${MYSQL_ROOT_PASSWORD}" --execute="SELECT 1;" 2>/dev/null; then
        warn "MySQL root déjà sécurisé — utilisation du mot de passe fourni."
        mysql --user=root --password="${MYSQL_ROOT_PASSWORD}" "$@"
    else
        error "Connexion MySQL root impossible. Vérifiez le mot de passe root MySQL saisi."
    fi
}

_mysql_root <<SQL
ALTER USER 'root'@'localhost' IDENTIFIED WITH mysql_native_password BY '${MYSQL_ROOT_PASSWORD}';
DELETE FROM mysql.user WHERE User='';
DELETE FROM mysql.user WHERE User='root' AND Host NOT IN ('localhost', '127.0.0.1', '::1');
DROP DATABASE IF EXISTS test;
DELETE FROM mysql.db WHERE Db='test' OR Db='test\\_%';
FLUSH PRIVILEGES;
SQL

# Créer la base et l'utilisateur applicatif
mysql --user=root --password="${MYSQL_ROOT_PASSWORD}" <<SQL
CREATE DATABASE IF NOT EXISTS school365_prod
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'school365_user'@'localhost'
    IDENTIFIED BY '${DB_PASSWORD}';
GRANT ALL PRIVILEGES ON school365_prod.* TO 'school365_user'@'localhost';
FLUSH PRIVILEGES;
SQL

log "MySQL 8 configuré — base school365_prod créée."

# =============================================================================
# 7. UTILISATEUR APPLICATIF
# =============================================================================
section "7. Création de l'utilisateur système school365"
if ! id "school365" &>/dev/null; then
    adduser --system --no-create-home --group school365
    log "Utilisateur school365 créé."
else
    warn "Utilisateur school365 déjà existant — ignoré."
fi

# =============================================================================
# 8. ARBORESCENCE DES RÉPERTOIRES
# =============================================================================
section "8. Création de l'arborescence /var/www/school365"
mkdir -p /var/www/school365/api/wwwroot/{uploads,logos,photos,documents}
mkdir -p /var/www/school365/api/logs
mkdir -p /var/www/school365/frontend

chown -R school365:school365 /var/www/school365/api
chown -R www-data:www-data    /var/www/school365/frontend
chmod -R 755 /var/www/school365

log "Répertoires créés."

# =============================================================================
# 9. FICHIER DE SECRETS (/etc/school365/env)
# =============================================================================
section "9. Création du fichier de secrets sécurisé"
mkdir -p /etc/school365

cat > /etc/school365/env <<ENV
# School365 — Variables d'environnement de production
# Fichier protégé : chmod 600 — NE PAS COMMITER

ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5000

# Base de données
ConnectionStrings__MySql=Server=localhost;Port=3306;Database=school365_prod;User=school365_user;Password=${DB_PASSWORD};

# JWT
Jwt__Key=${JWT_KEY}
Jwt__Secret=${JWT_KEY}

# CORS
Cors__AllowedOrigins=https://${DOMAIN}

# Frontend (liens dans les emails)
FrontendBaseUrl=https://${DOMAIN}

# Email SMTP
Email__SmtpHost=${SMTP_HOST}
Email__SmtpPort=${SMTP_PORT}
Email__EnableSsl=true
Email__Username=${SMTP_USER}
Email__Password=${SMTP_PASSWORD}
Email__FromAddress=${SMTP_FROM}
Email__PortalDomain=${DOMAIN}

# Stockage fichiers
FileStorage__BasePath=/var/www/school365/api/wwwroot
FileStorage__UrlPrefix=https://${DOMAIN}
ENV

chmod 600 /etc/school365/env
chown root:root /etc/school365/env
# Supprimer les \r éventuels (fins de ligne Windows) pour éviter les erreurs de source
sed -i 's/\r//' /etc/school365/env
log "Fichier /etc/school365/env créé et sécurisé (chmod 600)."

# =============================================================================
# 10. NGINX — Configuration du site
# =============================================================================
section "10. Configuration Nginx"

# Supprimer le site par défaut
rm -f /etc/nginx/sites-enabled/default

cat > /etc/nginx/sites-available/school365 <<NGINX
# ─── HTTP → HTTPS ─────────────────────────────────────────────────────────────
server {
    listen 80;
    server_name ${DOMAIN} www.${DOMAIN};

    # Nécessaire pour certbot (challenge ACME)
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    location / {
        return 301 https://\$host\$request_uri;
    }
}

# ─── HTTPS principal ──────────────────────────────────────────────────────────
server {
    listen 443 ssl http2;
    server_name ${DOMAIN} www.${DOMAIN};

    # Certificats Let's Encrypt (générés par certbot juste après)
    ssl_certificate     /etc/letsencrypt/live/${DOMAIN}/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/${DOMAIN}/privkey.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_ciphers         HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    ssl_session_cache   shared:SSL:10m;
    ssl_session_timeout 10m;

    # Sécurité HTTP headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Content-Type-Options nosniff always;
    add_header X-Frame-Options SAMEORIGIN always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    root  /var/www/school365/frontend;
    index index.html;

    # Limite taille upload (documents, photos…)
    client_max_body_size 50M;

    # ── Fichiers statiques Angular (cache long)
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot|webp|avif)\$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # ── PWA — manifest et service worker (pas de cache)
    location ~* (manifest\.webmanifest|ngsw\.json|ngsw-worker\.js|safety-worker\.js)\$ {
        expires 0;
        add_header Cache-Control "no-store, no-cache";
    }

    # ── API backend (reverse proxy)
    location /api/ {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade \$http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host \$host;
        proxy_set_header   X-Real-IP \$remote_addr;
        proxy_set_header   X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        proxy_read_timeout 300s;
    }

    # ── SignalR WebSocket
    location /hubs/ {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade \$http_upgrade;
        proxy_set_header   Connection "upgrade";
        proxy_set_header   Host \$host;
        proxy_set_header   X-Real-IP \$remote_addr;
        proxy_read_timeout 86400s;
    }

    # ── Fichiers uploadés (servis par ASP.NET Core)
    location ~ ^/(uploads|logos|photos|documents)/ {
        proxy_pass       http://127.0.0.1:5000;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }

    # ── Scalar / OpenAPI docs
    location /scalar/ {
        proxy_pass       http://127.0.0.1:5000;
        proxy_set_header Host \$host;
    }

    # ── SPA fallback (toutes les routes Angular)
    location / {
        try_files \$uri \$uri/ /index.html;
    }
}
NGINX

# Dossier pour le challenge ACME (HTTP)
mkdir -p /var/www/certbot

# Démarrer nginx avec un vhost HTTP-only — certbot génèrera les certs à l'étape 11
cat > /etc/nginx/sites-available/school365-http <<NGINX_HTTP
server {
    listen 80;
    server_name ${DOMAIN} www.${DOMAIN};
    location /.well-known/acme-challenge/ { root /var/www/certbot; }
    location / { return 301 https://\$host\$request_uri; }
}
NGINX_HTTP

ln -sf /etc/nginx/sites-available/school365-http /etc/nginx/sites-enabled/school365
systemctl enable nginx
nginx -t && systemctl restart nginx
log "Nginx configuré pour $DOMAIN (HTTP-only, SSL sera activé à l'étape 11)."

# =============================================================================
# 11. SSL / LET'S ENCRYPT
# =============================================================================
section "11. Certificat SSL Let's Encrypt"
warn "Assurez-vous que $DOMAIN pointe vers l'IP de ce VPS avant de continuer."
echo ""
read -rp "Lancer certbot maintenant ? (oui/non) : " RUN_CERTBOT

if [[ "$RUN_CERTBOT" == "oui" ]]; then
    # school365-http est déjà actif (étape 10) — certbot valide via HTTP et génère les certs
    certbot --nginx \
        -d "${DOMAIN}" \
        -d "www.${DOMAIN}" \
        --non-interactive \
        --agree-tos \
        --email "${SMTP_FROM}" \
        --redirect

    # Activer le vhost complet (SSL) — les certs existent maintenant
    ln -sf /etc/nginx/sites-available/school365 /etc/nginx/sites-enabled/school365
    rm -f /etc/nginx/sites-available/school365-http
    nginx -t && systemctl reload nginx

    # Vérifier le renouvellement automatique
    certbot renew --dry-run
    log "Certificat SSL obtenu et renouvellement automatique vérifié."
else
    warn "Certbot ignoré. Relancez manuellement :"
    warn "  certbot --nginx -d ${DOMAIN} -d www.${DOMAIN}"
    warn "Puis : systemctl reload nginx"
fi

# =============================================================================
# 12. SERVICE SYSTEMD POUR L'API
# =============================================================================
section "12. Service systemd school365-api"

cat > /etc/systemd/system/school365-api.service <<SYSTEMD
[Unit]
Description=School365 ASP.NET Core API
After=network.target mysql.service
Requires=mysql.service

[Service]
Type=simple
User=school365
Group=school365
WorkingDirectory=/var/www/school365/api
ExecStart=/usr/local/bin/dotnet /var/www/school365/api/server.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=school365-api

# Chargement des secrets
EnvironmentFile=/etc/school365/env

# Hardening systemd
ProtectSystem=full
PrivateTmp=true
NoNewPrivileges=true

[Install]
WantedBy=multi-user.target
SYSTEMD

systemctl daemon-reload
systemctl enable school365-api
log "Service systemd school365-api créé et activé."
warn "Le service ne peut pas démarrer tant que le binaire n'est pas déployé dans /var/www/school365/api/"

# =============================================================================
# 13. LOGROTATE POUR LES LOGS APPLICATIFS
# =============================================================================
section "13. Configuration logrotate"
cat > /etc/logrotate.d/school365 <<LOGROTATE
/var/www/school365/api/logs/*.log {
    daily
    rotate 14
    compress
    delaycompress
    missingok
    notifempty
    create 0640 school365 school365
    sharedscripts
    postrotate
        systemctl kill -s HUP school365-api 2>/dev/null || true
    endscript
}
LOGROTATE
log "Logrotate configuré."

# =============================================================================
# 14. SCRIPT DE DÉPLOIEMENT DE L'API
# =============================================================================
section "14. Génération du script de déploiement deploy-api.sh"
cat > /usr/local/bin/deploy-api.sh <<'DEPLOY'
#!/usr/bin/env bash
# Usage (depuis votre machine de dev) :
#   scp publish/api.zip root@VOTRE_VPS:/tmp/api.zip
#   ssh root@VOTRE_VPS "bash /usr/local/bin/deploy-api.sh"
set -euo pipefail

API_DIR=/var/www/school365/api
BACKUP_DIR=/var/backups/school365/api

echo "→ Arrêt du service…"
systemctl stop school365-api || true

echo "→ Sauvegarde de l'ancienne version…"
mkdir -p "$BACKUP_DIR"
if [ -d "$API_DIR" ] && [ "$(ls -A $API_DIR)" ]; then
    tar -czf "$BACKUP_DIR/api-$(date +%Y%m%d-%H%M%S).tar.gz" -C "$API_DIR" . 2>/dev/null || true
fi

echo "→ Décompression du nouveau build…"
mkdir -p "$API_DIR"
unzip -o /tmp/api.zip -d /tmp/api-extract/

# Copier en préservant wwwroot/{uploads,logos,photos,documents} et logs
rsync -av --exclude='wwwroot/uploads' --exclude='wwwroot/logos' \
          --exclude='wwwroot/photos' --exclude='wwwroot/documents' \
          --exclude='logs' \
          /tmp/api-extract/api/ "$API_DIR/"

echo "→ Recréation des dossiers requis…"
mkdir -p "$API_DIR"/wwwroot/{uploads,logos,photos,documents}
mkdir -p "$API_DIR"/logs

echo "→ Permissions…"
chown -R school365:school365 "$API_DIR"
chmod -R 755 "$API_DIR"

echo "→ Migrations EF Core…"
cd "$API_DIR"
# export robuste : gère les \r et les caractères spéciaux dans les mots de passe
export $(grep -v '^#' /etc/school365/env | sed 's/\r//' | xargs)
dotnet server.dll -- ef database update 2>/dev/null || \
    echo "[WARN] Migration EF non exécutée (non disponible en mode publié — utilisez un outil dédié)"

echo "→ Démarrage du service…"
systemctl start school365-api
systemctl status school365-api --no-pager

echo "✓ Déploiement terminé."
rm -rf /tmp/api.zip /tmp/api-extract
DEPLOY

chmod +x /usr/local/bin/deploy-api.sh
log "Script /usr/local/bin/deploy-api.sh créé."

# =============================================================================
# 15. VÉRIFICATIONS FINALES
# =============================================================================
section "15. Vérifications finales"

echo ""
info "État des services :"
systemctl is-active nginx     && log "nginx        : actif" || warn "nginx        : inactif"
systemctl is-active mysql     && log "mysql        : actif" || warn "mysql        : inactif"
systemctl is-enabled school365-api &>/dev/null && log "school365-api: activé au démarrage" || warn "school365-api: non activé"

echo ""
info "Connexion MySQL (test) :"
mysql --user=school365_user --password="${DB_PASSWORD}" school365_prod -e "SELECT 'OK' AS connexion;" 2>/dev/null \
    && log "Connexion MySQL school365_user : OK" \
    || warn "Connexion MySQL school365_user : ÉCHEC"

echo ""
info "Test Nginx :"
nginx -t && log "Configuration Nginx : syntaxe OK" || warn "Configuration Nginx : erreur de syntaxe"

# =============================================================================
# RÉSUMÉ FINAL
# =============================================================================
section "Installation terminée"

cat <<SUMMARY

  Domaine         : https://${DOMAIN}
  API (local)     : http://127.0.0.1:5000
  Frontend        : /var/www/school365/frontend/
  API             : /var/www/school365/api/
  Secrets         : /etc/school365/env  (chmod 600)
  Service         : systemctl [start|stop|status|logs] school365-api
  Logs API        : journalctl -u school365-api -f
  Logs Nginx      : tail -f /var/log/nginx/error.log

  ──────────────────────────────────────────────────────
  ÉTAPES SUIVANTES (depuis votre machine de dev) :

  1. Corriger le code (voir DEPLOYMENT.md §2)

  2. Builder le backend :
       dotnet publish server/server.csproj -c Release -o ./publish/api --self-contained false -r linux-x64
       cd publish && zip -r api.zip api/
       # Structure attendue dans api.zip :
       #   api/server.dll
       #   api/appsettings.json
       #   api/*.dll
       #   api/wwwroot/  (optionnel)

  3. Transférer et déployer le backend :
       scp publish/api.zip root@${DOMAIN}:/tmp/api.zip
       ssh root@${DOMAIN} "bash /usr/local/bin/deploy-api.sh"
       # Si le fichier .service a été modifié manuellement, recharger systemd :
       #   ssh root@${DOMAIN} "systemctl daemon-reload && systemctl restart school365-api"

  4. Builder le frontend :
       cd client && npm run build

  5. Transférer le frontend :
       ssh root@${DOMAIN} "rm -rf /tmp/frontend && mkdir -p /tmp/frontend"
       scp -r client/dist/client/browser/. root@${DOMAIN}:/tmp/frontend
       ssh root@${DOMAIN} "rsync -a --delete /tmp/frontend/ /var/www/school365/frontend/ && chown -R www-data:www-data /var/www/school365/frontend && rm -rf /tmp/frontend"

  6. Créer le super-admin (base vide uniquement — EFFACE toutes les données) :
       ssh root@${DOMAIN} "cd /var/www/school365/api && export \$(grep -v '^#' /etc/school365/env | sed 's/\\r//' | xargs) && dotnet server.dll seed"
       ssh root@${DOMAIN} "systemctl restart school365-api"

  7. Vérifications post-déploiement :
       curl https://${DOMAIN}/api/v1/health
       curl -I https://${DOMAIN}
       journalctl -u school365-api --since "5 min ago"

  ──────────────────────────────────────────────────────
  Clé JWT (à conserver en lieu sûr) :
  ${JWT_KEY}
  ──────────────────────────────────────────────────────

SUMMARY
