#!/usr/bin/env bash
# =============================================================================
# Expatriate365 — Setup local WSL2 (Ubuntu)
#
# Version simplifiée de setup-vps-expatriate.sh pour un environnement local :
#   - Pas de SSL / certbot
#   - MySQL (même config que la prod)
#   - Pas de SMTP
#   - Nginx en HTTP sur localhost:80
#   - Fichiers sources dans ~/expatriate365 (système de fichiers Linux natif)
#
# Usage depuis WSL2 :
#   cd ~ && git clone <ton-repo> expatriate365
#   sed 's/\r//' expatriate365/scripts/setup-local-wsl.sh | sudo bash -s
#
# Ou :
#   cp /mnt/c/dev/expatriate365/scripts/setup-local-wsl.sh /tmp/
#   sudo bash /tmp/setup-local-wsl.sh
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
DOTNET_CHANNEL="9.0"

# Répertoire du projet (système de fichiers Linux natif — meilleur que /mnt/c/)
read -rp "  Chemin du projet [~/expatriate365] : " PROJECT_DIR
PROJECT_DIR="${PROJECT_DIR:-$HOME/expatriate365}"
PROJECT_DIR="${PROJECT_DIR/#\~/$HOME}"
[[ ! -d "$PROJECT_DIR" ]] && error "Répertoire introuvable : $PROJECT_DIR"

API_DIR="$PROJECT_DIR/server"
FRONTEND_DIR="$PROJECT_DIR/client"

# Clé JWT
JWT_KEY=$(openssl rand -base64 64 | tr -d '\n')
log "Clé JWT générée automatiquement."

# Base de données MySQL
read -rp "  Nom de la base MySQL [${APP_NAME}_local] : " DB_NAME
DB_NAME="${DB_NAME:-${APP_NAME}_local}"
read -rp "  Utilisateur MySQL [${APP_NAME}_user] : " DB_USER
DB_USER="${DB_USER:-${APP_NAME}_user}"
read -rsp "  Mot de passe utilisateur MySQL (≥12 car.) : " DB_PASSWORD; echo
[[ ${#DB_PASSWORD} -lt 12 ]] && error "Mot de passe DB trop court (12 car. min)."
read -rsp "  Mot de passe root MySQL : " MYSQL_ROOT_PASSWORD; echo
[[ ${#MYSQL_ROOT_PASSWORD} -lt 4 ]] && error "Mot de passe root MySQL trop court."

# Super admin
read -rp "  Email super admin [super_admin@localhost] : " SEED_ADMIN_EMAIL
SEED_ADMIN_EMAIL="${SEED_ADMIN_EMAIL:-super_admin@localhost}"
read -rsp "  Mot de passe super admin (≥8 car.) : " SEED_ADMIN_PASSWORD; echo
[[ ${#SEED_ADMIN_PASSWORD} -lt 8 ]] && error "Mot de passe trop court."

echo ""
info "Récapitulatif :"
echo "  APP_NAME    : $APP_NAME"
echo "  Projet      : $PROJECT_DIR"
echo "  API port    : $API_PORT"
echo "  Base MySQL  : $DB_NAME (user: $DB_USER)"
echo "  Super admin : $SEED_ADMIN_EMAIL"
echo ""
read -rp "Confirmer ? (oui/non) : " CONFIRM
[[ "$CONFIRM" != "oui" ]] && { warn "Annulé."; exit 0; }

# =============================================================================
# 1. MISE À JOUR + OUTILS
# =============================================================================
section "1. Outils système"
apt update -y
DEBIAN_FRONTEND=noninteractive apt install -y \
    curl wget git unzip nginx \
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
# 3. INSTALLATION .NET
# =============================================================================
section "2. .NET $DOTNET_CHANNEL"
DOTNET_INSTALL_DIR="/opt/dotnet"

if command -v dotnet &>/dev/null; then
    _installed_major=$(dotnet --version 2>/dev/null | cut -d. -f1 || echo "0")
    _required_major=$(echo "$DOTNET_CHANNEL" | cut -d. -f1)
    if [[ "$_installed_major" -ge "$_required_major" ]]; then
        log ".NET $(dotnet --version) déjà installé — ignoré."
    else
        warn ".NET $_installed_major présent mais channel $DOTNET_CHANNEL requis — mise à jour."
        goto_install=true
    fi
else
    goto_install=true
fi

if [[ "${goto_install:-false}" == true ]]; then
    mkdir -p "$DOTNET_INSTALL_DIR"
    wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_INSTALL_DIR"
    ln -sf "$DOTNET_INSTALL_DIR/dotnet" /usr/local/bin/dotnet
    grep -q "DOTNET_ROOT" /etc/environment 2>/dev/null || echo "DOTNET_ROOT=$DOTNET_INSTALL_DIR" >> /etc/environment
    log ".NET channel $DOTNET_CHANNEL installé."
fi
dotnet --version

# =============================================================================
# 3. UTILISATEUR SYSTÈME
# =============================================================================
section "4. Utilisateur système $APP_NAME"
if id "$APP_NAME" &>/dev/null; then
    log "Utilisateur $APP_NAME déjà existant — ignoré."
else
    adduser --system --no-create-home --group "$APP_NAME"
    log "Utilisateur $APP_NAME créé."
fi

# =============================================================================
# 4. ARBORESCENCE
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
# 5. FICHIER DE SECRETS
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
# 6. SERVICE SYSTEMD
# =============================================================================
section "7. Service systemd ${APP_NAME}-api"

# WSL2 : systemd peut ne pas être disponible — détection automatique
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
    warn "systemd non disponible dans ce WSL2 — le service sera lancé manuellement."
    warn "Pour démarrer l'API : sudo -u ${APP_NAME} dotnet /var/www/${APP_NAME}/api/${APP_DLL}"
fi

# =============================================================================
# 7. NGINX — HTTP local (pas de SSL)
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

    # ── Frontend Angular (ng serve --host 0.0.0.0) ───────────────────────────
    location / {
        proxy_pass         http://127.0.0.1:4200;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade \$http_upgrade;
        proxy_set_header   Connection "upgrade";
        proxy_set_header   Host \$host;
    }
}
NGINX

ln -sf "/etc/nginx/sites-available/${APP_NAME}" "/etc/nginx/sites-enabled/${APP_NAME}"
rm -f /etc/nginx/sites-enabled/default
nginx -t
service nginx restart
log "Nginx configuré sur http://localhost"

# =============================================================================
# 8. BUILD API + DÉPLOIEMENT LOCAL
# =============================================================================
section "9. Build et déploiement de l'API"

info "Build de l'API depuis $API_DIR..."
cd "$API_DIR"
dotnet publish -c Release -o "/var/www/${APP_NAME}/api" --nologo -q
chown -R "${APP_NAME}:${APP_NAME}" "/var/www/${APP_NAME}/api"
chown -R "${APP_NAME}:www-data" "/var/www/${APP_NAME}/api/downloads"
chmod -R 750 "/var/www/${APP_NAME}/api/downloads"
find "/var/www/${APP_NAME}/api/downloads" -type d -exec chmod g+s {} \;
log "API déployée dans /var/www/${APP_NAME}/api"

# ── Migrations ────────────────────────────────────────────────────────────────
info "Application des migrations..."
set -a; source <(grep -v '^#' "/etc/${APP_NAME}/env" | sed 's/\r//'); set +a
cd "/var/www/${APP_NAME}/api"
dotnet "${APP_DLL}" -- ef database update 2>/dev/null \
    || warn "Migration EF ignorée — utilisez 'dotnet ef database update' manuellement si nécessaire."

# ── Démarrage ─────────────────────────────────────────────────────────────────
if [[ -d /run/systemd/system ]]; then
    service "${APP_NAME}-api" start || systemctl start "${APP_NAME}-api" || true
    log "Service ${APP_NAME}-api démarré."
else
    warn "Lance l'API manuellement dans un terminal :"
    warn "  sudo -u ${APP_NAME} ASPNETCORE_URLS=http://0.0.0.0:${API_PORT} dotnet /var/www/${APP_NAME}/api/${APP_DLL}"
fi

# =============================================================================
# 9. BUILD FRONTEND
# =============================================================================
section "10. Build frontend Angular"

if ! command -v ng &>/dev/null && ! command -v npx &>/dev/null; then
    warn "Angular CLI introuvable — installe Node.js puis relance :"
    warn "  curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -"
    warn "  sudo apt install -y nodejs"
    warn "  npm install -g @angular/cli"
else
    info "Build Angular depuis $FRONTEND_DIR..."
    cd "$FRONTEND_DIR"
    npm ci --silent
    npx ng build --configuration production --output-path "/var/www/${APP_NAME}/frontend" 2>/dev/null \
        || warn "Build Angular échoué — lance 'ng serve --host 0.0.0.0' à la place pour le dev."
    chown -R www-data:www-data "/var/www/${APP_NAME}/frontend"
    log "Frontend déployé dans /var/www/${APP_NAME}/frontend"
fi

# =============================================================================
# RÉSUMÉ
# =============================================================================
echo ""
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}  ✓ Setup local terminé${NC}"
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo "  Frontend     : http://localhost"
echo "  API          : http://localhost/api/v1"
echo "  Scalar       : http://localhost/scalar"
echo "  Super admin  : $SEED_ADMIN_EMAIL"
echo ""
echo "  Pour redéployer l'API après un changement :"
echo "    cd $API_DIR && dotnet publish -c Release -o /var/www/${APP_NAME}/api"
echo "    sudo service ${APP_NAME}-api restart"
echo ""
echo "  Pour le dev frontend (hot reload) :"
echo "    cd $FRONTEND_DIR && ng serve --host 0.0.0.0"
echo "    → accède via http://localhost (nginx proxifie le port 4200)"
echo ""
