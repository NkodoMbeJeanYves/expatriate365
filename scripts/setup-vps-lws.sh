#!/usr/bin/env bash
# =============================================================================
# acm365hub — Script de configuration VPS LWS
# Cible : Ubuntu 22.04 LTS
# Usage : sudo bash setup-vps-expatriate.sh
# =============================================================================
set -euo pipefail

# ─── Couleurs ────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
log() { echo -e "${GREEN}[✓]${NC} $*"; }
info() { echo -e "${CYAN}[→]${NC} $*"; }
warn() { echo -e "${YELLOW}[!]${NC} $*"; }
error() { echo -e "${RED}[✗]${NC} $*"; exit 1; }
section() { _STEP_NAME="$*"; echo -e "\n${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}\n${CYAN}  $_STEP_NAME${NC}\n${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"; }
trap 'echo -e "\n${RED}[✗] ÉCHEC — ${_STEP_NAME} (ligne ${LINENO})${NC}" >&2' ERR
[[ $EUID -ne 0 ]] && error "Ce script doit être exécuté en root : sudo bash $0"

# =============================================================================
# PARAMÈTRES
# =============================================================================
section "Configuration acm365hub — Saisie des paramètres"

read -rp "Nom de domaine (ex: acm365hub.poweryoursaas.com) : " DOMAIN
[[ -z "$DOMAIN" ]] && error "Le domaine est obligatoire."

read -rp "Mot de passe MySQL acm365hub_user (≥12 caractères) : " -s DB_PASSWORD; echo
[[ ${#DB_PASSWORD} -lt 12 ]] && error "Mot de passe trop court."

read -rp "Clé JWT (≥32 caractères, Entrée = auto-génération) : " -s JWT_KEY; echo
[[ -z "$JWT_KEY" ]] && JWT_KEY=$(openssl rand -base64 64 | tr -d '\n')
[[ ${#JWT_KEY} -lt 32 ]] && error "Clé JWT trop courte."

read -rp "Adresse email SMTP (From) : " SMTP_FROM
read -rp "Hôte SMTP : " SMTP_HOST
read -rp "Port SMTP [587] : " SMTP_PORT; SMTP_PORT=${SMTP_PORT:-587}
read -rp "Identifiant SMTP : " SMTP_USER
read -rp "Mot de passe SMTP : " -s SMTP_PASSWORD; echo

# =============================================================================
# MYSQL — Création de la base et de l'utilisateur expatriate365
# =============================================================================
section "Création de la base MySQL expatriate365"

# Saisie du mot de passe root MySQL
read -rp "Mot de passe root MySQL : " -s MYSQL_ROOT_PASSWORD; echo
[[ ${#MYSQL_ROOT_PASSWORD} -lt 12 ]] && error "Le mot de passe root doit faire au moins 12 caractères."

# Création de la base et de l'utilisateur
mysql --user=root --password="${MYSQL_ROOT_PASSWORD}" <<SQL
CREATE DATABASE IF NOT EXISTS expatriate365_prod
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS 'expatriate365_user'@'localhost'
    IDENTIFIED BY '${DB_PASSWORD}';

GRANT ALL PRIVILEGES ON expatriate365_prod.* TO 'expatriate365_user'@'localhost';
FLUSH PRIVILEGES;
SQL

log "Base expatriate365_prod et utilisateur expatriate365_user créés."


# =============================================================================
# ARBORESCENCE
# =============================================================================
section "Création des répertoires /var/www/acm365hub"
mkdir -p /var/www/acm365hub/api/wwwroot/{uploads,logos,photos,documents}
mkdir -p /var/www/acm365hub/api/logs
mkdir -p /var/www/acm365hub/frontend
adduser --system --no-create-home --group acm365hub || true
chown -R acm365hub:acm365hub /var/www/acm365hub/api
chown -R www-data:www-data /var/www/acm365hub/frontend
chmod -R 755 /var/www/acm365hub
log "Répertoires créés."

# =============================================================================
# FICHIER DE SECRETS
# =============================================================================
section "Création du fichier de secrets /etc/acm365hub/env"
mkdir -p /etc/acm365hub
cat > /etc/acm365hub/env <<ENV
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5001

ConnectionStrings__MySql=Server=localhost;Port=3306;Database=acm365hub_prod;User=acm365hub_user;Password=${DB_PASSWORD};

Jwt__Key=${JWT_KEY}
Jwt__Secret=${JWT_KEY}

Cors__AllowedOrigins=https://${DOMAIN}
FrontendBaseUrl=https://${DOMAIN}

Email__SmtpHost=${SMTP_HOST}
Email__SmtpPort=${SMTP_PORT}
Email__EnableSsl=true
Email__Username=${SMTP_USER}
Email__Password=${SMTP_PASSWORD}
Email__FromAddress=${SMTP_FROM}
Email__PortalDomain=${DOMAIN}

FileStorage__BasePath=/var/www/acm365hub/api/wwwroot
FileStorage__UrlPrefix=https://${DOMAIN}
ENV
chmod 600 /etc/acm365hub/env
chown root:root /etc/acm365hub/env
log "Secrets créés."

# =============================================================================
# NGINX
# =============================================================================
section "Configuration Nginx pour acm365hub"
cat > /etc/nginx/sites-available/acm365hub <<NGINX
server {
    listen 80;
    server_name ${DOMAIN} www.${DOMAIN};
    location /.well-known/acme-challenge/ { root /var/www/certbot; }
    location / { return 301 https://\$host\$request_uri; }
}
server {
    listen 443 ssl http2;
    server_name ${DOMAIN} www.${DOMAIN};
    ssl_certificate     /etc/letsencrypt/live/${DOMAIN}/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/${DOMAIN}/privkey.pem;
    root /var/www/acm365hub/frontend;
    index index.html;
    client_max_body_size 50M;
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot|webp|avif)\$ { expires 1y; add_header Cache-Control "public, immutable"; }
    location /api/ { proxy_pass http://127.0.0.1:5001; proxy_http_version 1.1; proxy_set_header Upgrade \$http_upgrade; proxy_set_header Connection keep-alive; proxy_set_header Host \$host; }
    location / { try_files \$uri \$uri/ /index.html; }
}
NGINX
ln -sf /etc/nginx/sites-available/acm365hub /etc/nginx/sites-enabled/acm365hub
nginx -t && systemctl reload nginx
log "Nginx configuré."

# =============================================================================
# SSL
# =============================================================================
section "Certificat SSL Let's Encrypt"
certbot --nginx -d "${DOMAIN}" -d "www.${DOMAIN}" --non-interactive --agree-tos --email "${SMTP_FROM}" --redirect || warn "Certbot doit être relancé manuellement."

# =============================================================================
# SYSTEMD
# =============================================================================
section "Service systemd acm365hub-api"
cat > /etc/systemd/system/acm365hub-api.service <<SYSTEMD
[Unit]
Description=acm365hub ASP.NET Core API
After=network.target mysql.service
Requires=mysql.service

[Service]
Type=simple
User=acm365hub
Group=acm365hub
WorkingDirectory=/var/www/acm365hub/api
ExecStart=/usr/local/bin/dotnet /var/www/acm365hub/api/server.dll --urls "http://0.0.0.0:5001"
Restart=always
RestartSec=10
EnvironmentFile=/etc/acm365hub/env

[Install]
WantedBy=multi-user.target
SYSTEMD
systemctl daemon-reload
systemctl enable acm365hub-api
log "Service acm365hub-api créé."

# =============================================================================
# SCRIPT DEPLOY
# =============================================================================
section "Script de déploiement deploy-expatriate-api.sh"
cat > /usr/local/bin/deploy-expatriate-api.sh <<'DEPLOY'
#!/usr/bin/env bash
set -euo pipefail
API_DIR=/var/www/acm365hub/api
BACKUP_DIR=/var/backups/acm365hub/api
systemctl stop acm365hub-api || true
mkdir -p "$BACKUP_DIR"
if [ -d "$API_DIR" ] && [ "$(ls -A $API_DIR)" ]; then
    tar -czf "$BACKUP_DIR/api-$(date +%Y%m%d-%H%M%S).tar.gz" -C "$API_DIR" . || true
fi
mkdir -p "$API_DIR"
unzip -o /tmp/api-expatriate.zip -d /tmp/api-extract/
rsync -av --exclude='wwwroot/uploads' --exclude='wwwroot/logos' --exclude='wwwroot/photos' --exclude='wwwroot/documents' --exclude='logs' /tmp/api-extract/ "$API_DIR/"
mkdir -p "$API_DIR"/wwwroot/{uploads,logos,photos,documents} "$API_DIR"/logs
chown -R acm365hub:acm365hub "$API_DIR"
chmod -R 755 "$API_DIR"
cd "$API_DIR"
export $(grep -v '^#' /etc/acm365hub/env | sed 's/\r//' | xargs)
dotnet server.dll -- ef database update || echo "[WARN] Migration EF non exécutée"
systemctl start acm365hub-api
systemctl status acm365hub-api --no-pager
rm -rf /tmp/api-expatriate.zip /tmp/api-extract
DEPLOY
chmod +x /usr/local/bin/deploy-expatriate-api
log "Script /usr/local/bin/deploy-expatriate-api.sh créé."