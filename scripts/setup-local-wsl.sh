#!/usr/bin/env bash
# =============================================================================
# Expatriate365 — Setup local WSL2 (Ubuntu)
#
# Version simplifiée de setup-vps-expatriate.sh pour un environnement local :
#   - Pas de SSL / certbot
#   - MySQL (même config que la prod)
#   - Pas de SMTP
#   - Nginx en HTTP sur localhost:80
#   - Déploiement API    via api.zip      → /var/www/expatriate365/api/
#   - Déploiement Front  via frontend.zip → /var/www/expatriate365/frontend/
#   - Options --reset / --seed en fin de déploiement
#
# Prérequis :
#   - api.zip      : dotnet publish -c Release → zip du dossier publish
#   - frontend.zip : ng build --configuration production → zip du dossier dist
#   - Placer les zips dans ~/ (ou indiquer les chemins au démarrage)
#
# Usage depuis WSL2 :
#   cp /mnt/c/dev/expatriate365/scripts/setup-local-wsl.sh /tmp/
#   sudo bash /tmp/setup-local-wsl.sh
#
# Valeurs par défaut (Entrée = accepter) :
#   Chemin api.zip        ~/api.zip
#   Chemin frontend.zip   ~/frontend.zip
#   Channel .NET          9.0
#   Nom de la base        expatriate365_local
#   Utilisateur MySQL     expatriate365_user
#   Email super admin     super_admin@localhost
#
# Champs obligatoires (pas de valeur par défaut) :
#   Mot de passe utilisateur MySQL  (≥ 12 caractères)
#   Mot de passe root MySQL
#   Mot de passe super admin        (≥ 8 caractères)
# =============================================================================
set -euo pipefail

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
log()     { echo -e "${GREEN}[✓]${NC} $*"; }
info()    { echo -e "${CYAN}[→]${NC} $*"; }
warn()    { echo -e "${YELLOW}[!]${NC} $*"; }
error()   { echo -e "${RED}[✗]${NC} $*"; exit 1; }
section() { echo -e "\n${CYAN}━━━ $* ━━━${NC}"; }

[[ $EUID -ne 0 ]] && error "Ce script doit être exécuté en root : sudo bash $0"

# =============================================================================
# PARAMÈTRES
# =============================================================================
section "Paramètres"

APP_NAME="expatriate365"
APP_DLL="server.dll"
API_PORT="5001"
DOMAIN="localhost"

# ── Chemin api.zip ────────────────────────────────────────────────────────────
echo ""
read -rp "  Chemin vers api.zip [~/api.zip] : " API_ZIP
API_ZIP="${API_ZIP:-$HOME/api.zip}"
API_ZIP="${API_ZIP/#\~/$HOME}"
[[ ! -f "$API_ZIP" ]] && error "Fichier introuvable : $API_ZIP"
log "api.zip trouvé : $API_ZIP"

# ── Chemin frontend.zip ───────────────────────────────────────────────────────
read -rp "  Chemin vers frontend.zip [~/frontend.zip] : " FRONTEND_ZIP
FRONTEND_ZIP="${FRONTEND_ZIP:-$HOME/frontend.zip}"
FRONTEND_ZIP="${FRONTEND_ZIP/#\~/$HOME}"
[[ ! -f "$FRONTEND_ZIP" ]] && error "Fichier introuvable : $FRONTEND_ZIP"
log "frontend.zip trouvé : $FRONTEND_ZIP"

# ── Version .NET ──────────────────────────────────────────────────────────────
echo ""
info "Version .NET à utiliser pour ce projet :"
echo "  [1] .NET 9  (net9.0  — défaut de ce projet)"
echo "  [2] .NET 10 (net10.0 — si vous avez migré le .csproj)"
read -rp "  Votre choix [1] : " _DOTNET_CHOICE
_DOTNET_CHOICE="${_DOTNET_CHOICE:-1}"
[[ "$_DOTNET_CHOICE" == "2" ]] && DOTNET_CHANNEL="10.0" || DOTNET_CHANNEL="9.0"
info "Channel sélectionné : .NET $DOTNET_CHANNEL"

# ── JWT ───────────────────────────────────────────────────────────────────────
JWT_KEY=$(openssl rand -base64 64 | tr -d '\n')
log "Clé JWT générée automatiquement."

# ── MySQL ─────────────────────────────────────────────────────────────────────
echo ""
read -rp "  Nom de la base MySQL [${APP_NAME}_local] : " DB_NAME
DB_NAME="${DB_NAME:-${APP_NAME}_local}"
read -rp "  Utilisateur MySQL [${APP_NAME}_user] : " DB_USER
DB_USER="${DB_USER:-${APP_NAME}_user}"
read -rsp "  Mot de passe utilisateur MySQL (≥12 car.) : " DB_PASSWORD; echo
[[ ${#DB_PASSWORD} -lt 12 ]] && error "Mot de passe DB trop court (12 car. min)."
read -rsp "  Mot de passe root MySQL : " MYSQL_ROOT_PASSWORD; echo
[[ ${#MYSQL_ROOT_PASSWORD} -lt 4 ]] && error "Mot de passe root MySQL trop court."

# ── Super admin ───────────────────────────────────────────────────────────────
echo ""
read -rp "  Email super admin [super_admin@localhost] : " SEED_ADMIN_EMAIL
SEED_ADMIN_EMAIL="${SEED_ADMIN_EMAIL:-super_admin@localhost}"
read -rsp "  Mot de passe super admin (≥8 car.) : " SEED_ADMIN_PASSWORD; echo
[[ ${#SEED_ADMIN_PASSWORD} -lt 8 ]] && error "Mot de passe trop court."

# ── Seed / Reset ──────────────────────────────────────────────────────────────
echo ""
info "Gestion de la base de données après déploiement :"
echo "  [1] Conserver la base existante (défaut)"
echo "  [2] --reset : recréer le schéma (rôles + super_admin seulement)"
echo "  [3] --seed  : recréer le schéma + injecter les données de démo"
read -rp "  Votre choix [1] : " _DB_ACTION
_DB_ACTION="${_DB_ACTION:-1}"
_HAS_RESET=false
_HAS_SEED=false
[[ "$_DB_ACTION" == "2" ]] && _HAS_RESET=true
[[ "$_DB_ACTION" == "3" ]] && _HAS_SEED=true

# ── Récapitulatif ─────────────────────────────────────────────────────────────
echo ""
info "Récapitulatif :"
echo "  api.zip      : $API_ZIP"
echo "  frontend.zip : $FRONTEND_ZIP"
echo "  .NET         : channel $DOTNET_CHANNEL"
echo "  API port     : $API_PORT"
echo "  Base MySQL   : $DB_NAME (user: $DB_USER)"
echo "  Super admin  : $SEED_ADMIN_EMAIL"
echo "  DB action    : $( [[ "$_HAS_RESET" == true ]] && echo "--reset" || ( [[ "$_HAS_SEED" == true ]] && echo "--seed" || echo "conserver" ) )"
echo ""
read -rp "Confirmer ? (oui/non) : " CONFIRM
[[ "$CONFIRM" != "oui" ]] && { warn "Annulé."; exit 0; }

# =============================================================================
# 1. MISE À JOUR + OUTILS
# =============================================================================
section "1. Outils système"
apt update -y
DEBIAN_FRONTEND=noninteractive apt install -y \
    curl wget git unzip rsync nginx \
    software-properties-common apt-transport-https gnupg
log "Outils installés."

# =============================================================================
# 2. MYSQL
# =============================================================================
section "2. MySQL — Base $DB_NAME"

if systemctl is-active --quiet mysql 2>/dev/null || service mysql status &>/dev/null; then
    log "MySQL déjà actif — installation ignorée."
else
    DEBIAN_FRONTEND=noninteractive apt install -y mysql-server
    service mysql start
    log "MySQL installé."
fi

_mysql_root() {
    if MYSQL_PWD="" mysql --user=root --execute="SELECT 1;" 2>/dev/null; then
        MYSQL_PWD="" mysql --user=root "$@"
    elif MYSQL_PWD="${MYSQL_ROOT_PASSWORD}" mysql --user=root --execute="SELECT 1;" 2>/dev/null; then
        MYSQL_PWD="${MYSQL_ROOT_PASSWORD}" mysql --user=root "$@"
    else
        error "Connexion MySQL root impossible. Vérifiez le mot de passe root MySQL."
    fi
}

_mysql_root <<SQL
CREATE DATABASE IF NOT EXISTS ${DB_NAME}
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '${DB_USER}'@'localhost'
    IDENTIFIED BY '${DB_PASSWORD}';
GRANT ALL PRIVILEGES ON ${DB_NAME}.* TO '${DB_USER}'@'localhost';
FLUSH PRIVILEGES;
SQL
log "Base ${DB_NAME} + utilisateur ${DB_USER} configurés."

# =============================================================================
# 3. INSTALLATION .NET (9 + 10)
# =============================================================================
section "3. .NET 9 + .NET 10"
DOTNET_INSTALL_DIR="/opt/dotnet"
mkdir -p "$DOTNET_INSTALL_DIR"

wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh

if dotnet --list-sdks 2>/dev/null | grep -q "^9\."; then
    log ".NET 9 déjà installé — ignoré."
else
    info "Installation .NET 9..."
    /tmp/dotnet-install.sh --channel 9.0 --install-dir "$DOTNET_INSTALL_DIR"
    log ".NET 9 installé."
fi

if dotnet --list-sdks 2>/dev/null | grep -q "^10\."; then
    log ".NET 10 déjà installé — ignoré."
else
    info "Installation .NET 10..."
    /tmp/dotnet-install.sh --channel 10.0 --install-dir "$DOTNET_INSTALL_DIR"
    log ".NET 10 installé."
fi

ln -sf "$DOTNET_INSTALL_DIR/dotnet" /usr/local/bin/dotnet
grep -q "DOTNET_ROOT" /etc/environment 2>/dev/null || echo "DOTNET_ROOT=$DOTNET_INSTALL_DIR" >> /etc/environment

info "SDKs disponibles :"
dotnet --list-sdks
info "Channel actif pour ce projet : .NET $DOTNET_CHANNEL"

# =============================================================================
# 4. UTILISATEUR SYSTÈME
# =============================================================================
section "4. Utilisateur système $APP_NAME"
if id "$APP_NAME" &>/dev/null; then
    log "Utilisateur $APP_NAME déjà existant — ignoré."
else
    adduser --system --no-create-home --group "$APP_NAME"
    log "Utilisateur $APP_NAME créé."
fi

# =============================================================================
# 5. ARBORESCENCE
# =============================================================================
section "5. Arborescence /var/www/$APP_NAME"
mkdir -p "/var/www/${APP_NAME}/api/downloads/{attachments,branding,avatars,docs}"
mkdir -p "/var/www/${APP_NAME}/api/logs"
mkdir -p "/var/www/${APP_NAME}/frontend"

chown -R "${APP_NAME}:${APP_NAME}" "/var/www/${APP_NAME}/api"
chown -R www-data:www-data          "/var/www/${APP_NAME}/frontend"
chmod -R 755 "/var/www/${APP_NAME}"
chown -R "${APP_NAME}:www-data" "/var/www/${APP_NAME}/api/downloads"
chmod -R 750 "/var/www/${APP_NAME}/api/downloads"
find "/var/www/${APP_NAME}/api/downloads" -type d -exec chmod g+s {} \;
log "Répertoires créés."

# =============================================================================
# 6. FICHIER DE SECRETS
# =============================================================================
section "6. Fichier de secrets /etc/$APP_NAME/env"
mkdir -p "/etc/${APP_NAME}"

cat > "/etc/${APP_NAME}/env" <<ENV
# ${APP_NAME} — Variables d'environnement local (WSL2)
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://0.0.0.0:${API_PORT}

# MySQL
ConnectionStrings__MySql='Server=localhost;Port=3306;Database=${DB_NAME};User=${DB_USER};Password=${DB_PASSWORD};'

# JWT
Jwt__SecretKey='${JWT_KEY}'
Jwt__Issuer=http://${DOMAIN}
Jwt__Audience=http://${DOMAIN}

# CORS
Cors__AllowedOrigins=http://${DOMAIN}

# Frontend
FrontendBaseUrl=http://${DOMAIN}

# Super Admin
Seed__SuperAdminEmail='${SEED_ADMIN_EMAIL}'
Seed__SuperAdminPassword='${SEED_ADMIN_PASSWORD}'

# Storage
FileStorage__BasePath=/var/www/${APP_NAME}/api/downloads

# Email (désactivé en local)
Email__FromAddress=noreply@localhost
Email__SmtpHost=localhost
Email__SmtpPort=1025
Email__Username=
Email__Password=

# Déploiement (interne)
_DEPLOY_APP_DLL=${APP_DLL}
_DEPLOY_API_PORT=${API_PORT}
ENV

chmod 600 "/etc/${APP_NAME}/env"
log "Fichier /etc/${APP_NAME}/env créé (chmod 600)."

# =============================================================================
# 7. SERVICE SYSTEMD
# =============================================================================
section "7. Service systemd ${APP_NAME}-api"

if [[ -d /run/systemd/system ]]; then
    cat > "/etc/systemd/system/${APP_NAME}-api.service" <<SERVICE
[Unit]
Description=${APP_NAME} API
After=network.target

[Service]
Type=simple
User=${APP_NAME}
WorkingDirectory=/var/www/${APP_NAME}/api
EnvironmentFile=/etc/${APP_NAME}/env
ExecStart=/usr/local/bin/dotnet /var/www/${APP_NAME}/api/${APP_DLL}
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
SERVICE
    systemctl daemon-reload
    systemctl enable "${APP_NAME}-api"
    log "Service systemd ${APP_NAME}-api configuré."
else
    warn "systemd non disponible dans ce WSL2 — service à lancer manuellement."
    warn "  sudo -u ${APP_NAME} dotnet /var/www/${APP_NAME}/api/${APP_DLL}"
fi

# =============================================================================
# 8. NGINX — HTTP local (pas de SSL)
# =============================================================================
section "8. Nginx — vhost localhost"

cat > "/etc/nginx/sites-available/${APP_NAME}" <<NGINX
server {
    listen 80;
    server_name localhost;

    client_max_body_size 20M;

    # ── API ASP.NET Core ──────────────────────────────────────────────────────
    location /api/ {
        proxy_pass         http://127.0.0.1:${API_PORT};
        proxy_http_version 1.1;
        proxy_set_header   Host \$host;
        proxy_set_header   X-Real-IP \$remote_addr;
        proxy_set_header   X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto \$scheme;
        proxy_read_timeout 300s;
    }

    # ── SignalR / WebSockets ──────────────────────────────────────────────────
    location /hubs/ {
        proxy_pass         http://127.0.0.1:${API_PORT};
        proxy_http_version 1.1;
        proxy_set_header   Upgrade \$http_upgrade;
        proxy_set_header   Connection "upgrade";
        proxy_set_header   Host \$host;
        proxy_read_timeout 86400s;
    }

    # ── Fichiers uploadés ─────────────────────────────────────────────────────
    location /downloads/avatars/ {
        alias /var/www/${APP_NAME}/api/downloads/avatars/;
    }
    location /downloads/branding/ {
        alias /var/www/${APP_NAME}/api/downloads/branding/;
    }
    location /downloads/attachments/ {
        alias /var/www/${APP_NAME}/api/downloads/attachments/;
    }
    location /downloads/docs/ {
        alias /var/www/${APP_NAME}/api/downloads/docs/;
        add_header Content-Disposition "attachment";
    }

    # ── Scalar ────────────────────────────────────────────────────────────────
    location /scalar/ {
        proxy_pass       http://127.0.0.1:${API_PORT};
        proxy_set_header Host \$host;
    }

    # ── Frontend Angular ──────────────────────────────────────────────────────
    root  /var/www/${APP_NAME}/frontend;
    index index.html;

    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot|webp|avif)\$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    location ~* (manifest\.webmanifest|ngsw\.json|ngsw-worker\.js|safety-worker\.js)\$ {
        expires 0;
        add_header Cache-Control "no-store, no-cache";
    }

    location / {
        try_files \$uri \$uri/ /index.html;
    }
}
NGINX

ln -sf "/etc/nginx/sites-available/${APP_NAME}" "/etc/nginx/sites-enabled/${APP_NAME}"
rm -f /etc/nginx/sites-enabled/default
nginx -t
service nginx restart
log "Nginx configuré sur http://localhost"

# =============================================================================
# 9. DÉPLOIEMENT API depuis api.zip
# =============================================================================
section "9. Déploiement API"

API_DEPLOY_DIR="/var/www/${APP_NAME}/api"
EXTRACT_DIR="/tmp/${APP_NAME}-api-extract"

info "Extraction de $API_ZIP..."
rm -rf "$EXTRACT_DIR"
mkdir -p "$EXTRACT_DIR"
unzip -o "$API_ZIP" -d "$EXTRACT_DIR"

info "Déploiement vers $API_DEPLOY_DIR..."
rsync -av \
    --exclude='downloads/attachments' \
    --exclude='downloads/branding' \
    --exclude='downloads/avatars' \
    --exclude='downloads/docs' \
    --exclude='logs' \
    "$EXTRACT_DIR/" "$API_DEPLOY_DIR/"

mkdir -p "${API_DEPLOY_DIR}/downloads/{attachments,branding,avatars,docs}"
mkdir -p "${API_DEPLOY_DIR}/logs"

chown -R "${APP_NAME}:${APP_NAME}" "$API_DEPLOY_DIR"
chown -R "${APP_NAME}:www-data" "${API_DEPLOY_DIR}/downloads"
chmod -R 750 "${API_DEPLOY_DIR}/downloads"
find "${API_DEPLOY_DIR}/downloads" -type d -exec chmod g+s {} \;
rm -rf "$EXTRACT_DIR"
log "API déployée dans $API_DEPLOY_DIR"

# ── Mémoriser les options choisies pour le résumé ────────────────────────────
_DB_ACTION_LABEL="conserver"
[[ "$_HAS_RESET" == true ]] && _DB_ACTION_LABEL="--reset"
[[ "$_HAS_SEED"  == true ]] && _DB_ACTION_LABEL="--seed"
log "API prête — démarrage à effectuer manuellement (voir résumé)."

# =============================================================================
# 10. DÉPLOIEMENT FRONTEND depuis frontend.zip
# =============================================================================
section "10. Déploiement Frontend"

FRONTEND_DEPLOY_DIR="/var/www/${APP_NAME}/frontend"
FRONTEND_EXTRACT="/tmp/${APP_NAME}-frontend-extract"

info "Extraction de $FRONTEND_ZIP..."
rm -rf "$FRONTEND_EXTRACT"
mkdir -p "$FRONTEND_EXTRACT"
unzip -o "$FRONTEND_ZIP" -d "$FRONTEND_EXTRACT"

info "Déploiement vers $FRONTEND_DEPLOY_DIR..."
rsync -av --delete "$FRONTEND_EXTRACT/" "$FRONTEND_DEPLOY_DIR/"
chown -R www-data:www-data "$FRONTEND_DEPLOY_DIR"
rm -rf "$FRONTEND_EXTRACT"
log "Frontend déployé dans $FRONTEND_DEPLOY_DIR"

service nginx reload

# =============================================================================
# RÉSUMÉ
# =============================================================================
echo ""
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}  ✓ Setup local terminé — démarrage manuel requis${NC}"
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo "  Application  : http://localhost"
echo "  API          : http://localhost/api/v1"
echo "  Scalar       : http://localhost/scalar"
echo "  Super admin  : $SEED_ADMIN_EMAIL"
echo "  DB action    : $_DB_ACTION_LABEL (à passer au démarrage de l'API)"
echo ""
echo -e "${CYAN}  ── Démarrer le backend ──────────────────────────────────────────${NC}"
if [[ -d /run/systemd/system ]]; then
echo "  sudo systemctl start ${APP_NAME}-api"
echo "  sudo systemctl status ${APP_NAME}-api"
else
echo "  # Terminal dédié (WSL2 sans systemd) :"
echo "  sudo -u ${APP_NAME} dotnet /var/www/${APP_NAME}/api/${APP_DLL}"
fi
if [[ "$_HAS_RESET" == true ]]; then
echo ""
echo "  # Puis appliquer le reset (dans un autre terminal après démarrage) :"
echo "  sudo -u ${APP_NAME} dotnet /var/www/${APP_NAME}/api/${APP_DLL} --reset"
fi
if [[ "$_HAS_SEED" == true ]]; then
echo ""
echo "  # Puis appliquer le seed (dans un autre terminal après démarrage) :"
echo "  sudo -u ${APP_NAME} dotnet /var/www/${APP_NAME}/api/${APP_DLL} --seed"
fi
echo ""
echo -e "${CYAN}  ── Frontend ─────────────────────────────────────────────────────${NC}"
echo "  Le frontend est servi par nginx (fichiers statiques)."
echo "  → Accède directement via http://localhost"
echo ""
echo -e "${CYAN}  ── Redéployer (nouveaux zips) ───────────────────────────────────${NC}"
echo "    cp /mnt/c/.../api.zip ~/api.zip"
echo "    cp /mnt/c/.../frontend.zip ~/frontend.zip"
echo "    sudo bash /tmp/setup-local-wsl.sh"
echo ""
