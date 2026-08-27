#!/usr/bin/env bash
# =============================================================================
# Expatriate365 — Script de configuration VPS
# Cible : Ubuntu 22.04 LTS
# Usage : sudo bash setup-vps-expatriate.sh
#
# Ce script peut tourner sur un VPS où un autre projet est déjà installé.
# Il détecte les composants partagés déjà présents (MySQL, .NET, Nginx, UFW)
# et ne les réinstalle pas.
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
skip()    { echo -e "${YELLOW}[~]${NC} $* — déjà installé, ignoré."; }
section() {
    _STEP_NAME="$*"
    (( _STEP_NUM++ )) || true
    local ts; ts=$(date '+%H:%M:%S')
    echo -e "\n${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${CYAN}  [${_STEP_NUM}/${_STEP_TOTAL}] $_STEP_NAME${NC}  ${YELLOW}[${ts}]${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
}

_STEP_NAME="initialisation"
_STEP_NUM=0
_STEP_TOTAL=16
trap 'echo -e "\n${RED}[✗] ÉCHEC — ${_STEP_NAME} (ligne ${LINENO})${NC}" >&2' ERR

[[ $EUID -ne 0 ]] && error "Ce script doit être exécuté en root : sudo bash $0"

next() {
    echo -e "${CYAN}  ↳ Prochain : $*${NC}"
}

# =============================================================================
# SECTION 0 — PARAMÈTRES DU PROJET (identité, modifiables à chaque exécution)
# =============================================================================
section "0. Paramètres du projet"
echo ""
warn "Ces valeurs définissent l'identité du déploiement."
warn "APP_NAME est utilisé pour nommer les répertoires, services, fichiers de config."
echo ""

# ── Nom applicatif ────────────────────────────────────────────────────────────
info "Nom applicatif court (lettres, chiffres, tirets — sans espaces)"
info "  Utilisé pour : /var/www/<APP_NAME>/, /etc/<APP_NAME>/env, service <APP_NAME>-api"
info "  ex: expatriate365, myapp, crm-prod"
read -rp "  APP_NAME [expatriate365] : " APP_NAME
APP_NAME="${APP_NAME:-expatriate365}"
[[ ! "$APP_NAME" =~ ^[a-zA-Z0-9_-]+$ ]] && error "APP_NAME ne doit contenir que des lettres, chiffres, tirets ou underscores."

# ── Lecture env existant (basé sur APP_NAME) ──────────────────────────────────
_env_get() {
    # Strip surrounding quotes from value so callers get the raw string
    [[ -f "/etc/${APP_NAME}/env" ]] && grep -E "^${1}=" "/etc/${APP_NAME}/env" 2>/dev/null | head -1 | cut -d= -f2- | sed 's/^["\x27]//;s/["\x27]$//' || true
}

# ── Domaine ──────────────────────────────────────────────────────────────────
_SAVED_DOMAIN=$(_env_get "FrontendBaseUrl" | sed 's|https://||')
_DEFAULT_DOMAIN="${_SAVED_DOMAIN:-acm365hub.poweryoursaas.com}"
info "Domaine public — sera utilisé pour le certificat SSL et la configuration Nginx."
info "  Le DNS doit déjà pointer vers l'IP de ce VPS (vérifiez avant de continuer)."
info "  ex: acm365hub.poweryoursaas.com"
read -rp "  Domaine [$_DEFAULT_DOMAIN] : " DOMAIN
DOMAIN="${DOMAIN:-$_DEFAULT_DOMAIN}"
[[ -z "$DOMAIN" ]] && error "Le domaine est obligatoire."

# ── Port interne API ──────────────────────────────────────────────────────────
_SAVED_PORT=$(_env_get "_DEPLOY_API_PORT")
_DEFAULT_PORT="${_SAVED_PORT:-5001}"
info "Port interne de l'API — utilisé par le service .NET et le reverse proxy Nginx."
info "  Ce port n'est pas exposé publiquement (Nginx fait le pont)."
info "  Chaque projet sur ce VPS doit avoir un port différent (school365 = 5000)."
info "  Vérifiez qu'il est libre : ss -tlnp | grep <port>"
read -rp "  Port interne [$_DEFAULT_PORT] : " API_PORT
API_PORT="${API_PORT:-$_DEFAULT_PORT}"
[[ ! "$API_PORT" =~ ^[0-9]+$ ]] && error "Le port doit être un nombre."

# ── Nom de la base de données ─────────────────────────────────────────────────
_SAVED_DB_NAME=$(_env_get "ConnectionStrings__MySql" | sed 's/.*Database=\([^;]*\).*/\1/')
_DEFAULT_DB_NAME="${_SAVED_DB_NAME:-${APP_NAME}_prod}"
info "Nom de la base de données MySQL qui sera créée pour ce projet."
info "  Chaque projet doit avoir sa propre base (school365 = school365_prod)."
info "  Convention recommandée : <APP_NAME>_prod"
read -rp "  Base de données [$_DEFAULT_DB_NAME] : " DB_NAME
DB_NAME="${DB_NAME:-$_DEFAULT_DB_NAME}"

# ── DLL principale ────────────────────────────────────────────────────────────
_SAVED_DLL=$(_env_get "_DEPLOY_APP_DLL")
_DEFAULT_DLL="${_SAVED_DLL:-server.dll}"
info "Nom du fichier DLL principal — point d'entrée de l'API ASP.NET Core."
info "  C'est le fichier que systemd lancera avec : dotnet <APP_DLL>"
info "  Trouvez-le dans votre .csproj : <AssemblyName> ou le nom du projet."
info "  Pour ce projet : server.dll (défaut)"
read -rp "  DLL principale [$_DEFAULT_DLL] : " APP_DLL
APP_DLL="${APP_DLL:-$_DEFAULT_DLL}"

# ── Version .NET ─────────────────────────────────────────────────────────────
_DEFAULT_DOTNET_CHANNEL="9.0"
info "Channel .NET requis par ce projet."
info "  Vérifiez <TargetFramework> dans votre .csproj :"
info "    net9.0  → channel 9.0"
info "    net10.0 → channel 10.0"
info "  Ce projet cible net9.0 — répondez 9.0 sauf si vous avez migré."
read -rp "  Channel .NET [$_DEFAULT_DOTNET_CHANNEL] : " DOTNET_CHANNEL
DOTNET_CHANNEL="${DOTNET_CHANNEL:-$_DEFAULT_DOTNET_CHANNEL}"

echo ""
info "Récapitulatif des paramètres projet :"
echo "  APP_NAME      : $APP_NAME"
echo "  Domaine       : $DOMAIN"
echo "  Port API      : $API_PORT"
echo "  Base MySQL    : $DB_NAME"
echo "  DLL           : $APP_DLL"
echo "  .NET channel  : $DOTNET_CHANNEL"
echo ""
info "Ces paramètres seront sauvegardés dans /etc/${APP_NAME}/env et utilisés par les scripts de déploiement."
info "Vérifiez chaque valeur ci-dessus avant de continuer."
info "Tapez 'oui' pour passer à la saisie des secrets, 'non' pour annuler et corriger."
read -rp "Confirmer ces paramètres ? (oui/non) : " CONFIRM_PARAMS
[[ "$CONFIRM_PARAMS" != "oui" ]] && { warn "Annulé. Relancez le script."; exit 0; }

echo ""
next "Section 1/2 de saisie — identifiants et secrets (DB, JWT, SMTP)"
info "Aucune modification système n'a encore eu lieu."
echo ""

# =============================================================================
# SECTION 1 — IDENTIFIANTS ET SECRETS
# =============================================================================
section "1. Identifiants et secrets"
echo ""

[[ -f "/etc/${APP_NAME}/env" ]] && info "Fichier /etc/${APP_NAME}/env détecté — valeurs actuelles proposées (Entrée = conserver)."
echo ""

# ── Utilisateur DB ────────────────────────────────────────────────────────────
_SAVED_DB_USER=$(_env_get "ConnectionStrings__MySql" | sed 's/.*User=\([^;]*\).*/\1/')
_DEFAULT_DB_USER="${_SAVED_DB_USER:-${APP_NAME}_user}"
info "Nom d'utilisateur MySQL dédié à ce projet (sera créé s'il n'existe pas)."
info "  Il n'aura accès qu'à la base $DB_NAME — jamais à root."
info "  Convention recommandée : <APP_NAME>_user"
read -rp "  Utilisateur DB [$_DEFAULT_DB_USER] : " DB_USER
DB_USER="${DB_USER:-$_DEFAULT_DB_USER}"
next "Mot de passe pour cet utilisateur DB"

# ── Mot de passe DB ───────────────────────────────────────────────────────────
echo ""
_SAVED_DB_PASS=$(_env_get "ConnectionStrings__MySql" | sed 's/.*Password=\([^;]*\).*/\1/')
info "Mot de passe de l'utilisateur MySQL '$DB_USER' — sera stocké dans /etc/${APP_NAME}/env."
info "  Minimum 12 caractères. La saisie est masquée (rien ne s'affiche)."
if [[ -n "$_SAVED_DB_PASS" ]]; then
    info "Mot de passe DB : déjà défini (masqué) — appuyez Entrée pour conserver."
    read -rp "  Mot de passe DB (≥12 car.) [Entrée = conserver] : " -s DB_PASSWORD; echo
    DB_PASSWORD="${DB_PASSWORD:-$_SAVED_DB_PASS}"
else
    read -rp "  Mot de passe DB (≥12 car.) : " -s DB_PASSWORD; echo
fi
[[ ${#DB_PASSWORD} -lt 12 ]] && error "Le mot de passe DB doit faire au moins 12 caractères."
next "Mot de passe root MySQL (pour créer la base — ne sera pas stocké)"

# ── Mot de passe root MySQL (jamais stocké) ───────────────────────────────────
echo ""
warn "Mot de passe root MySQL — toujours demandé, jamais stocké."
info "  Nécessaire uniquement pour créer la base '$DB_NAME' et l'utilisateur '$DB_USER'."
info "  C'est le mot de passe défini lors de l'installation initiale de MySQL sur ce VPS."
info "  La saisie est masquée (rien ne s'affiche)."
read -rp "  Mot de passe root MySQL : " -s MYSQL_ROOT_PASSWORD; echo
[[ ${#MYSQL_ROOT_PASSWORD} -lt 12 ]] && error "Le mot de passe root MySQL doit faire au moins 12 caractères."
next "Clé secrète JWT (signature des tokens d'authentification)"

# ── Clé JWT ───────────────────────────────────────────────────────────────────
echo ""
_SAVED_JWT=$(_env_get "Jwt__Key")
info "Clé secrète JWT — utilisée pour signer et vérifier les tokens d'authentification."
info "  Minimum 32 caractères. Appuyez Entrée pour laisser le script en générer une."
info "  IMPORTANT : notez-la après l'installation (affichée dans le résumé final)."
info "  Changer cette clé invalide tous les tokens actifs (déconnexion forcée de tous les utilisateurs)."
if [[ -n "$_SAVED_JWT" ]]; then
    info "Clé JWT : déjà définie (masquée) — appuyez Entrée pour conserver."
    read -rp "  Clé JWT (≥32 car.) [Entrée = conserver] : " -s JWT_KEY; echo
    JWT_KEY="${JWT_KEY:-$_SAVED_JWT}"
else
    read -rp "  Clé JWT (Entrée = auto-génération) : " -s JWT_KEY; echo
    if [[ -z "$JWT_KEY" ]]; then
        JWT_KEY=$(openssl rand -base64 64 | tr -d '\n')
        log "Clé JWT générée automatiquement."
    fi
fi
[[ ${#JWT_KEY} -lt 32 ]] && error "La clé JWT doit faire au moins 32 caractères."
next "Super admin — email et mot de passe du compte administrateur global"

# ── Super admin ───────────────────────────────────────────────────────────────
echo ""
info "─── Super Admin (1/2) — Email ────────────────────────────────────────────"
info "  Compte administrateur global (aucun tenant, accès complet)."
info "  Utilisé pour se connecter à l'application au premier démarrage."
_SAVED_SADMIN_EMAIL=$(_env_get "Seed__SuperAdminEmail")
[[ -n "$_SAVED_SADMIN_EMAIL" ]] && info "  Valeur actuelle : $_SAVED_SADMIN_EMAIL"
read -rp "  Email super admin [Entrée = conserver / défaut super_admin@${DOMAIN}] : " SEED_ADMIN_EMAIL
SEED_ADMIN_EMAIL="${SEED_ADMIN_EMAIL:-${_SAVED_SADMIN_EMAIL:-super_admin@${DOMAIN}}}"

echo ""
info "─── Super Admin (2/2) — Mot de passe ─────────────────────────────────────"
info "  Mot de passe initial du compte super_admin."
info "  Minimum 8 caractères. La saisie est masquée."
_SAVED_SADMIN_PASS=$(_env_get "Seed__SuperAdminPassword")
if [[ -n "$_SAVED_SADMIN_PASS" ]]; then
    info "  Mot de passe super admin : déjà défini (masqué) — Entrée pour conserver."
    read -rsp "  Mot de passe [Entrée = conserver] : " SEED_ADMIN_PASSWORD; echo
    SEED_ADMIN_PASSWORD="${SEED_ADMIN_PASSWORD:-$_SAVED_SADMIN_PASS}"
else
    read -rsp "  Mot de passe super admin : " SEED_ADMIN_PASSWORD; echo
fi
[[ ${#SEED_ADMIN_PASSWORD} -lt 8 ]] && error "Le mot de passe super admin doit faire au moins 8 caractères."
next "Configuration SMTP — 5 champs (email, hôte, port, identifiant, mot de passe)"

# ── SMTP ──────────────────────────────────────────────────────────────────────
echo ""
info "─── SMTP (1/5) — Adresse email expéditrice ───────────────────────────────"
info "  Email utilisé comme expéditeur dans les emails envoyés par l'application."
info "  Aussi utilisé comme contact pour le certificat SSL Let's Encrypt."
_SAVED_SMTP_FROM=$(_env_get "Email__FromAddress")
[[ -n "$_SAVED_SMTP_FROM" ]] && info "  Valeur actuelle : $_SAVED_SMTP_FROM"
read -rp "  Email SMTP (From) [Entrée = conserver] : " SMTP_FROM
SMTP_FROM="${SMTP_FROM:-$_SAVED_SMTP_FROM}"

echo ""
info "─── SMTP (2/5) — Serveur SMTP ────────────────────────────────────────────"
info "  Adresse du serveur qui enverra les emails."
_SAVED_SMTP_HOST=$(_env_get "Email__SmtpHost")
info "  Gmail: smtp.gmail.com | OVH/LWS: ssl0.ovh.net | Office365: smtp.office365.com"
[[ -n "$_SAVED_SMTP_HOST" ]] && info "  Valeur actuelle : $_SAVED_SMTP_HOST"
read -rp "  Hôte SMTP [Entrée = conserver] : " SMTP_HOST
SMTP_HOST="${SMTP_HOST:-$_SAVED_SMTP_HOST}"

echo ""
info "─── SMTP (3/5) — Port SMTP ───────────────────────────────────────────────"
info "  587 = STARTTLS (recommandé pour Gmail, OVH, Office365)."
info "  465 = SSL/TLS direct (moins courant)."
_SAVED_SMTP_PORT=$(_env_get "Email__SmtpPort")
[[ -n "$_SAVED_SMTP_PORT" ]] && info "  Valeur actuelle : $_SAVED_SMTP_PORT"
read -rp "  Port SMTP [Entrée = conserver / défaut 587] : " SMTP_PORT
SMTP_PORT="${SMTP_PORT:-${_SAVED_SMTP_PORT:-587}}"

echo ""
info "─── SMTP (4/5) — Identifiant de connexion ────────────────────────────────"
info "  Généralement votre adresse email complète (ex: monapp@gmail.com)."
_SAVED_SMTP_USER=$(_env_get "Email__Username")
[[ -n "$_SAVED_SMTP_USER" ]] && info "  Valeur actuelle : $_SAVED_SMTP_USER"
read -rp "  Identifiant SMTP [Entrée = conserver] : " SMTP_USER
SMTP_USER="${SMTP_USER:-$_SAVED_SMTP_USER}"

echo ""
info "─── SMTP (5/5) — Mot de passe SMTP ──────────────────────────────────────"
info "  Pour Gmail : NE PAS utiliser votre mot de passe principal."
info "  Créez un App Password sur https://myaccount.google.com/apppasswords"
info "  Pour OVH/LWS/Office365 : mot de passe de la boîte email."
info "  La saisie est masquée (rien ne s'affiche)."
_SAVED_SMTP_PASS=$(_env_get "Email__Password")
if [[ -n "$_SAVED_SMTP_PASS" ]]; then
    info "  Mot de passe SMTP : déjà défini (masqué) — appuyez Entrée pour conserver."
    read -rp "  Mot de passe SMTP [Entrée = conserver] : " -s SMTP_PASSWORD; echo
    SMTP_PASSWORD="${SMTP_PASSWORD:-$_SAVED_SMTP_PASS}"
else
    read -rp "  Mot de passe SMTP / App Password : " -s SMTP_PASSWORD; echo
fi
next "Récapitulatif complet — dernière confirmation avant lancement"

# ── Récapitulatif final ───────────────────────────────────────────────────────
echo ""
section "Récapitulatif complet"
echo "  APP_NAME      : $APP_NAME"
echo "  Domaine       : $DOMAIN"
echo "  Port API      : $API_PORT"
echo "  Base MySQL    : $DB_NAME  (user: $DB_USER)"
echo "  DLL           : $APP_DLL"
echo "  .NET channel  : $DOTNET_CHANNEL"
echo "  SMTP          : $SMTP_HOST:$SMTP_PORT  (from: $SMTP_FROM)"
echo ""
info "Tapez 'oui' pour démarrer l'installation (irréversible), 'non' pour annuler."
read -rp "Confirmer et lancer l'installation ? (oui/non) : " CONFIRM
[[ "$CONFIRM" != "oui" ]] && { warn "Installation annulée."; exit 0; }

echo ""
info "Lancement de l'installation — 16 étapes automatiques."
info "Les composants déjà présents (MySQL, .NET, UFW) seront détectés et ignorés."
next "Étape 2/16 — Mise à jour du système"
echo ""

# =============================================================================
# 2. MISE À JOUR SYSTÈME
# =============================================================================
section "2. Mise à jour du système"
apt update -y
DEBIAN_FRONTEND=noninteractive apt upgrade -y
log "Système mis à jour."
next "Étape 3/16 — Outils essentiels (curl, nginx, certbot, ufw…)"

# =============================================================================
# 3. OUTILS ESSENTIELS
# =============================================================================
section "3. Outils essentiels"
DEBIAN_FRONTEND=noninteractive apt install -y \
    curl wget git unzip nginx certbot python3-certbot-nginx ufw \
    software-properties-common apt-transport-https gnupg lsb-release
log "Outils installés / déjà présents."
next "Étape 4/16 — Dépendances Chromium (PuppeteerSharp / génération PDF)"

# =============================================================================
# 4. DÉPENDANCES CHROMIUM
# =============================================================================
section "4. Dépendances Chromium (PuppeteerSharp)"
DEBIAN_FRONTEND=noninteractive apt install -y \
    ca-certificates fonts-liberation libasound2 libatk-bridge2.0-0 libatk1.0-0 \
    libc6 libcairo2 libcups2 libdbus-1-3 libexpat1 libfontconfig1 libgbm1 \
    libgcc1 libglib2.0-0 libgtk-3-0 libnspr4 libnss3 libpango-1.0-0 \
    libpangocairo-1.0-0 libstdc++6 libx11-6 libx11-xcb1 libxcb1 \
    libxcomposite1 libxcursor1 libxdamage1 libxext6 libxfixes3 libxi6 \
    libxrandr2 libxrender1 libxss1 libxtst6 wget xdg-utils
log "Dépendances Chromium OK."
next "Étape 5/16 — Pare-feu UFW"

# =============================================================================
# 5. PARE-FEU UFW
# =============================================================================
section "5. Pare-feu UFW"
if ufw status | grep -q "Status: active"; then
    skip "UFW déjà actif"
    ufw allow OpenSSH      2>/dev/null || true
    ufw allow 'Nginx Full' 2>/dev/null || true
    log "Règles OpenSSH + Nginx Full vérifiées."
else
    ufw --force reset
    ufw default deny incoming
    ufw default allow outgoing
    ufw allow OpenSSH
    ufw allow 'Nginx Full'
    ufw --force enable
    log "Pare-feu configuré."
fi
next "Étape 6/16 — Installation .NET $DOTNET_CHANNEL (peut prendre 1-2 min)"

# =============================================================================
# 6. INSTALLATION .NET
# =============================================================================
section "6. .NET $DOTNET_CHANNEL"
DOTNET_INSTALL_DIR="/opt/dotnet"

_dotnet_ok=false
if command -v dotnet &>/dev/null; then
    _installed_major=$(dotnet --version 2>/dev/null | cut -d. -f1 || echo "0")
    _required_major=$(echo "$DOTNET_CHANNEL" | cut -d. -f1)
    if [[ "$_installed_major" -ge "$_required_major" ]]; then
        skip ".NET $( dotnet --version ) déjà installé (requis : channel $DOTNET_CHANNEL)"
        _dotnet_ok=true
    else
        warn ".NET $_installed_major présent mais channel $DOTNET_CHANNEL requis — installation en parallèle."
    fi
fi

if [[ "$_dotnet_ok" == false ]]; then
    mkdir -p "$DOTNET_INSTALL_DIR"
    wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_INSTALL_DIR"
    ln -sf "$DOTNET_INSTALL_DIR/dotnet" /usr/local/bin/dotnet
    grep -q "DOTNET_ROOT" /etc/environment 2>/dev/null || echo "DOTNET_ROOT=$DOTNET_INSTALL_DIR" >> /etc/environment
    dotnet --version
    log ".NET channel $DOTNET_CHANNEL installé."
fi

# =============================================================================
# 7. MYSQL — Base + utilisateur applicatif
# =============================================================================
section "7. MySQL — Base $DB_NAME"

_mysql_root() {
    # Passage du mot de passe via variable d'environnement — jamais en argument de commande
    if MYSQL_PWD="" mysql --user=root --execute="SELECT 1;" 2>/dev/null; then
        MYSQL_PWD="" mysql --user=root "$@"
    elif MYSQL_PWD="${MYSQL_ROOT_PASSWORD}" mysql --user=root --execute="SELECT 1;" 2>/dev/null; then
        MYSQL_PWD="${MYSQL_ROOT_PASSWORD}" mysql --user=root "$@"
    else
        error "Connexion MySQL root impossible. Vérifiez le mot de passe root MySQL saisi."
    fi
}

if systemctl is-active --quiet mysql; then
    skip "MySQL déjà actif — installation ignorée"
else
    DEBIAN_FRONTEND=noninteractive apt install -y mysql-server
    systemctl enable mysql
    systemctl start mysql

    _mysql_root <<SQL
ALTER USER 'root'@'localhost' IDENTIFIED WITH mysql_native_password BY '${MYSQL_ROOT_PASSWORD}';
DELETE FROM mysql.user WHERE User='';
DELETE FROM mysql.user WHERE User='root' AND Host NOT IN ('localhost', '127.0.0.1', '::1');
DROP DATABASE IF EXISTS test;
DELETE FROM mysql.db WHERE Db='test' OR Db='test\\_%';
FLUSH PRIVILEGES;
SQL
    log "MySQL installé et sécurisé."
fi

# ── Choix : base existante ou nouvelle ───────────────────────────────────────
DB_EXISTS=false
if MYSQL_PWD="${MYSQL_ROOT_PASSWORD}" mysql --user=root --execute "USE ${DB_NAME};" 2>/dev/null; then
    DB_EXISTS=true
fi

FRESH_DB=false
if [[ "$DB_EXISTS" == true ]]; then
    echo ""
    warn "La base de données '${DB_NAME}' existe déjà sur ce serveur."
    echo ""
    echo "  [1] Conserver la base existante (garder les données actuelles)"
    echo "  [2] Repartir sur une base vide   (DROP + CREATE — TOUTES LES DONNÉES SERONT PERDUES)"
    echo ""
    read -rp "  Votre choix [1] : " _DB_CHOICE
    _DB_CHOICE="${_DB_CHOICE:-1}"
    if [[ "$_DB_CHOICE" == "2" ]]; then
        echo ""
        warn "⚠  Vous allez SUPPRIMER définitivement la base '${DB_NAME}' et toutes ses données."
        read -rp "  Tapez le nom de la base pour confirmer : " _DB_CONFIRM
        if [[ "$_DB_CONFIRM" == "${DB_NAME}" ]]; then
            FRESH_DB=true
            log "Confirmation reçue — la base sera recréée."
        else
            warn "Nom incorrect — la base existante sera conservée."
        fi
    fi
else
    log "La base '${DB_NAME}' n'existe pas encore — elle sera créée."
fi

# ── Création / recréation de la base ─────────────────────────────────────────
if [[ "$FRESH_DB" == true ]]; then
    MYSQL_PWD="${MYSQL_ROOT_PASSWORD}" mysql --user=root <<SQL
DROP DATABASE IF EXISTS ${DB_NAME};
CREATE DATABASE ${DB_NAME}
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '${DB_USER}'@'localhost'
    IDENTIFIED BY '${DB_PASSWORD}';
GRANT ALL PRIVILEGES ON ${DB_NAME}.* TO '${DB_USER}'@'localhost';
FLUSH PRIVILEGES;
SQL
    log "Base ${DB_NAME} recréée (vide) + utilisateur ${DB_USER} configuré."
else
    MYSQL_PWD="${MYSQL_ROOT_PASSWORD}" mysql --user=root <<SQL
CREATE DATABASE IF NOT EXISTS ${DB_NAME}
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '${DB_USER}'@'localhost'
    IDENTIFIED BY '${DB_PASSWORD}';
GRANT ALL PRIVILEGES ON ${DB_NAME}.* TO '${DB_USER}'@'localhost';
FLUSH PRIVILEGES;
SQL
    log "Base ${DB_NAME} conservée + utilisateur ${DB_USER} configuré."
fi
next "Étape 8/16 — Création de l'utilisateur système $APP_NAME"

# =============================================================================
# 8. UTILISATEUR SYSTÈME
# =============================================================================
section "8. Utilisateur système $APP_NAME"
if id "$APP_NAME" &>/dev/null; then
    skip "Utilisateur $APP_NAME déjà existant"
else
    adduser --system --no-create-home --group "$APP_NAME"
    log "Utilisateur $APP_NAME créé."
fi

# =============================================================================
# 9. ARBORESCENCE
# =============================================================================
section "9. Arborescence /var/www/$APP_NAME"
mkdir -p "/var/www/${APP_NAME}/api/wwwroot/{uploads,logos,photos,documents}"
mkdir -p "/var/www/${APP_NAME}/api/logs"
mkdir -p "/var/www/${APP_NAME}/frontend"

chown -R "${APP_NAME}:${APP_NAME}" "/var/www/${APP_NAME}/api"
chown -R www-data:www-data          "/var/www/${APP_NAME}/frontend"
chmod -R 755 "/var/www/${APP_NAME}"
log "Répertoires créés."
next "Étape 10/16 — Écriture du fichier de secrets /etc/$APP_NAME/env"

# =============================================================================
# 10. FICHIER DE SECRETS (/etc/$APP_NAME/env)
# =============================================================================
section "10. Fichier de secrets /etc/$APP_NAME/env"
mkdir -p "/etc/${APP_NAME}"

cat > "/etc/${APP_NAME}/env" <<ENV
# ${APP_NAME} — Variables d'environnement de production
# Fichier protégé : chmod 600 — NE PAS COMMITER

ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:${API_PORT}

# Base de données
ConnectionStrings__MySql="Server=localhost;Port=3306;Database=${DB_NAME};User=${DB_USER};Password=${DB_PASSWORD};"

# JWT
Jwt__Key=${JWT_KEY}
Jwt__Secret=${JWT_KEY}

# CORS
Cors__AllowedOrigins=https://${DOMAIN}

# Frontend (liens dans les emails)
FrontendBaseUrl=https://${DOMAIN}

# Super Admin seed
Seed__SuperAdminEmail=${SEED_ADMIN_EMAIL}
Seed__SuperAdminPassword=${SEED_ADMIN_PASSWORD}

# Email SMTP
Email__SmtpHost=${SMTP_HOST}
Email__SmtpPort=${SMTP_PORT}
Email__EnableSsl=true
Email__Username=${SMTP_USER}
Email__Password=${SMTP_PASSWORD}
Email__FromAddress=${SMTP_FROM}
Email__PortalDomain=${DOMAIN}

# Stockage fichiers
FileStorage__BasePath=/var/www/${APP_NAME}/api/wwwroot
FileStorage__UrlPrefix=https://${DOMAIN}

# Mémo déploiement (utilisés par les scripts, pas par l'API)
_DEPLOY_APP_NAME=${APP_NAME}
_DEPLOY_APP_DLL=${APP_DLL}
_DEPLOY_API_PORT=${API_PORT}
ENV

chmod 600 "/etc/${APP_NAME}/env"
chown root:root "/etc/${APP_NAME}/env"
sed -i 's/\r//' "/etc/${APP_NAME}/env"
log "Fichier /etc/${APP_NAME}/env créé (chmod 600)."
next "Étape 11/16 — Configuration Nginx (vhost $DOMAIN)"

# =============================================================================
# 11. NGINX — Vhost $APP_NAME
# =============================================================================
section "11. Nginx — vhost $DOMAIN"

cat > "/etc/nginx/sites-available/${APP_NAME}" <<NGINX
# ─── HTTP → HTTPS ─────────────────────────────────────────────────────────────
server {
    listen 80;
    server_name ${DOMAIN} www.${DOMAIN};

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

    ssl_certificate     /etc/letsencrypt/live/${DOMAIN}/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/${DOMAIN}/privkey.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_ciphers         HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    ssl_session_cache   shared:SSL:10m;
    ssl_session_timeout 10m;

    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Content-Type-Options nosniff always;
    add_header X-Frame-Options SAMEORIGIN always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    root  /var/www/${APP_NAME}/frontend;
    index index.html;

    client_max_body_size 50M;

    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot|webp|avif)\$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    location ~* (manifest\.webmanifest|ngsw\.json|ngsw-worker\.js|safety-worker\.js)\$ {
        expires 0;
        add_header Cache-Control "no-store, no-cache";
    }

    location /api/ {
        proxy_pass         http://127.0.0.1:${API_PORT};
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

    location /hubs/ {
        proxy_pass         http://127.0.0.1:${API_PORT};
        proxy_http_version 1.1;
        proxy_set_header   Upgrade \$http_upgrade;
        proxy_set_header   Connection "upgrade";
        proxy_set_header   Host \$host;
        proxy_set_header   X-Real-IP \$remote_addr;
        proxy_read_timeout 86400s;
    }

    location ~ ^/(uploads|logos|photos|documents)/ {
        proxy_pass       http://127.0.0.1:${API_PORT};
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }

    location /scalar/ {
        proxy_pass       http://127.0.0.1:${API_PORT};
        proxy_set_header Host \$host;
    }

    location / {
        try_files \$uri \$uri/ /index.html;
    }
}
NGINX

mkdir -p /var/www/certbot

cat > "/etc/nginx/sites-available/${APP_NAME}-http" <<NGINX_HTTP
server {
    listen 80;
    server_name ${DOMAIN} www.${DOMAIN};
    location /.well-known/acme-challenge/ { root /var/www/certbot; }
    location / { return 301 https://\$host\$request_uri; }
}
NGINX_HTTP

ln -sf "/etc/nginx/sites-available/${APP_NAME}-http" "/etc/nginx/sites-enabled/${APP_NAME}"
nginx -t && systemctl reload nginx
log "Vhost Nginx $DOMAIN configuré (HTTP-only — SSL à l'étape 12)."

# =============================================================================
# 12. SSL / LET'S ENCRYPT
# =============================================================================
section "12. Certificat SSL Let's Encrypt"
warn "Assurez-vous que $DOMAIN pointe vers l'IP de ce VPS avant de continuer."
info "  Vérifiez la propagation DNS : dig $DOMAIN +short"
info "  La commande doit retourner l'IP de ce VPS."
info "  Si ce n'est pas encore le cas, répondez 'non' — vous pourrez relancer certbot manuellement."
echo ""
read -rp "Lancer certbot maintenant ? (oui/non) : " RUN_CERTBOT

if [[ "$RUN_CERTBOT" == "oui" ]]; then
    certbot --nginx \
        -d "${DOMAIN}" \
        -d "www.${DOMAIN}" \
        --non-interactive \
        --agree-tos \
        --email "${SMTP_FROM}" \
        --redirect

    ln -sf "/etc/nginx/sites-available/${APP_NAME}" "/etc/nginx/sites-enabled/${APP_NAME}"
    rm -f "/etc/nginx/sites-available/${APP_NAME}-http"
    nginx -t && systemctl reload nginx
    certbot renew --dry-run
    log "Certificat SSL obtenu et renouvellement automatique vérifié."
else
    warn "Certbot ignoré. Relancez manuellement :"
    warn "  certbot --nginx -d ${DOMAIN} -d www.${DOMAIN}"
    warn "Puis : systemctl reload nginx"
fi
next "Étape 13/16 — Création du service systemd ${APP_NAME}-api"

# =============================================================================
# 13. SERVICE SYSTEMD
# =============================================================================
section "13. Service systemd ${APP_NAME}-api"

cat > "/etc/systemd/system/${APP_NAME}-api.service" <<SYSTEMD
[Unit]
Description=${APP_NAME} ASP.NET Core API
After=network.target mysql.service
Requires=mysql.service

[Service]
Type=simple
User=${APP_NAME}
Group=${APP_NAME}
WorkingDirectory=/var/www/${APP_NAME}/api
ExecStart=/usr/local/bin/dotnet /var/www/${APP_NAME}/api/${APP_DLL}
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=${APP_NAME}-api

EnvironmentFile=/etc/${APP_NAME}/env

ProtectSystem=full
PrivateTmp=true
NoNewPrivileges=true

[Install]
WantedBy=multi-user.target
SYSTEMD

systemctl daemon-reload
systemctl enable "${APP_NAME}-api"
log "Service systemd ${APP_NAME}-api créé et activé."
warn "Le service ne peut pas démarrer tant que le binaire n'est pas déployé dans /var/www/${APP_NAME}/api/"
next "Étape 14/16 — Logrotate"

# =============================================================================
# 14. LOGROTATE
# =============================================================================
section "14. Logrotate"
cat > "/etc/logrotate.d/${APP_NAME}" <<LOGROTATE
/var/www/${APP_NAME}/api/logs/*.log {
    daily
    rotate 14
    compress
    delaycompress
    missingok
    notifempty
    create 0640 ${APP_NAME} ${APP_NAME}
    sharedscripts
    postrotate
        systemctl kill -s HUP ${APP_NAME}-api 2>/dev/null || true
    endscript
}
LOGROTATE
log "Logrotate configuré."
next "Étape 15/16 — Génération du script de déploiement sur le VPS"

# =============================================================================
# 15. SCRIPT DE DÉPLOIEMENT SUR LE VPS
# =============================================================================
section "15. Script /usr/local/bin/deploy-${APP_NAME}-api.sh"

cat > "/usr/local/bin/deploy-${APP_NAME}-api.sh" <<DEPLOY
#!/usr/bin/env bash
# =============================================================================
# Déploiement de l'API ${APP_NAME}
#
# Mode migrations EF Core (défaut) :
#   ssh root@VPS "bash /usr/local/bin/deploy-${APP_NAME}-api.sh"
#
# Mode schema SQL (alternative si EF Core indisponible) :
#   scp schema.sql root@VPS:/tmp/${APP_NAME}-schema.sql
#   ssh root@VPS "bash /usr/local/bin/deploy-${APP_NAME}-api.sh --schema-only"
# =============================================================================
set -euo pipefail

APP_NAME="${APP_NAME}"
APP_DLL="${APP_DLL}"
API_DIR="/var/www/\${APP_NAME}/api"
BACKUP_DIR="/var/backups/\${APP_NAME}/api"
ENV_FILE="/etc/\${APP_NAME}/env"
SCHEMA_FILE="/tmp/\${APP_NAME}-schema.sql"

# ── Parsing des arguments ─────────────────────────────────────────────────────
SCHEMA_MODE=false
_HAS_RESET=false
_HAS_SEED=false
for _arg in "\${@:-}"; do
    case "\${_arg}" in
        --schema-only) SCHEMA_MODE=true ;;
        --reset)       _HAS_RESET=true  ;;
        --seed)        _HAS_SEED=true   ;;
    esac
done

echo "[DEBUG] APP_NAME=\${APP_NAME}"
echo "[DEBUG] APP_DLL=\${APP_DLL}"
echo "[DEBUG] API_DIR=\${API_DIR}"
echo "[DEBUG] ENV_FILE=\${ENV_FILE}"
echo "[DEBUG] Arguments : schema=\${SCHEMA_MODE} reset=\${_HAS_RESET} seed=\${_HAS_SEED}"
echo "[DEBUG] ENV_FILE exists=\$([ -f \"\${ENV_FILE}\" ] && echo yes || echo NO)"

# ── Arrêt du service ──────────────────────────────────────────────────────────
echo "→ Arrêt du service \${APP_NAME}-api…"
systemctl stop "\${APP_NAME}-api" || true

# ── Sauvegarde ────────────────────────────────────────────────────────────────
echo "→ Sauvegarde…"
mkdir -p "\${BACKUP_DIR}"
if [ -d "\${API_DIR}" ] && [ "\$(ls -A \${API_DIR})" ]; then
    tar -czf "\${BACKUP_DIR}/api-\$(date +%Y%m%d-%H%M%S).tar.gz" -C "\${API_DIR}" . 2>/dev/null || true
fi

# ── Décompression ─────────────────────────────────────────────────────────────
echo "→ Décompression…"
mkdir -p "\${API_DIR}"
unzip -o "/tmp/\${APP_NAME}-api.zip" -d "/tmp/\${APP_NAME}-api-extract/"

rsync -av --exclude='wwwroot/uploads' --exclude='wwwroot/logos' \
          --exclude='wwwroot/photos'  --exclude='wwwroot/documents' \
          --exclude='logs' \
          "/tmp/\${APP_NAME}-api-extract/" "\${API_DIR}/"

mkdir -p "\${API_DIR}/wwwroot/{uploads,logos,photos,documents}"
mkdir -p "\${API_DIR}/logs"

# ── Permissions ───────────────────────────────────────────────────────────────
echo "→ Permissions…"
chown -R "\${APP_NAME}:\${APP_NAME}" "\${API_DIR}"
chmod -R 755 "\${API_DIR}"

# ── Chargement des variables d'environnement (sans les afficher) ──────────────
set -a
# shellcheck source=/dev/null
source <(grep -v '^#' "\${ENV_FILE}" | { grep -v '^_DEPLOY' || true; } | sed 's/\r//')
set +a
echo "[DEBUG] source ENV_FILE : OK"
echo "[DEBUG] ASPNETCORE_URLS=\${ASPNETCORE_URLS:-<non défini>}"
echo "[DEBUG] ConnectionStrings__MySql=\$(echo \"\${ConnectionStrings__MySql:-<non défini>}\" | sed 's/Password=[^;]*/Password=***/')"

if [[ "\${SCHEMA_MODE}" == true ]]; then
    # ── Mode SQL dump ─────────────────────────────────────────────────────────
    if [ ! -f "\${SCHEMA_FILE}" ]; then
        echo "[✗] Fichier \${SCHEMA_FILE} introuvable."
        echo "    Générez-le localement avec :"
        echo "      cd server && dotnet ef migrations script --idempotent -o schema.sql"
        echo "    Puis transférez-le :"
        echo "      scp schema.sql root@VPS:/tmp/\${APP_NAME}-schema.sql"
        exit 1
    fi
    echo "→ Application du schéma SQL (\${SCHEMA_FILE})…"
    DB_HOST=\$(echo "\${ConnectionStrings__MySql}" | grep -oP 'Server=\K[^;]+')
    DB_PORT=\$(echo "\${ConnectionStrings__MySql}" | grep -oP 'Port=\K[^;]+')
    DB_NAME_VAL=\$(echo "\${ConnectionStrings__MySql}" | grep -oP 'Database=\K[^;]+')
    DB_USER_VAL=\$(echo "\${ConnectionStrings__MySql}" | grep -oP 'User=\K[^;]+')
    DB_PASS_VAL=\$(echo "\${ConnectionStrings__MySql}" | grep -oP 'Password=\K[^;]+')
    MYSQL_PWD="\${DB_PASS_VAL}" mysql --host="\${DB_HOST}" --port="\${DB_PORT:-3306}" \
          --user="\${DB_USER_VAL}" "\${DB_NAME_VAL}" < "\${SCHEMA_FILE}" \
        && echo "[✓] Schéma appliqué avec succès." \
        || echo "[✗] Erreur lors de l'application du schéma."
    rm -f "\${SCHEMA_FILE}"
else
    # ── Mode migrations EF Core (défaut) ─────────────────────────────────────
    echo "→ Migrations EF Core…"
    echo "[DEBUG] dotnet \${API_DIR}/\${APP_DLL} (exists=\$([ -f \"\${API_DIR}/\${APP_DLL}\" ] && echo yes || echo NO))"
    cd "\${API_DIR}"
    dotnet "\${APP_DLL}" -- ef database update 2>/dev/null || \
        echo "[WARN] Migration EF non exécutée en mode publié — utilisez --schema-only si Pomelo indisponible"
fi

# ── Seed (reset + repopulation) — uniquement si demandé ──────────────────────
if [[ "\${_HAS_RESET}" == true ]] || [[ "\${_HAS_SEED}" == true ]]; then
    set -a
    # shellcheck source=/dev/null
    source <(grep -v '^#' "\${ENV_FILE}" | { grep -v '^_DEPLOY' || true; } | sed 's/\r//')
    set +a
    echo "[DEBUG] post-source ASPNETCORE_URLS=\${ASPNETCORE_URLS:-<non défini>}"
    if [[ "\$_HAS_RESET" == true ]]; then
        echo "→ --reset : suppression du schéma et recréation (rôles + super_admin seulement)…"
        echo "[DEBUG] dotnet \${API_DIR}/\${APP_DLL} --reset"
        dotnet "\${API_DIR}/\${APP_DLL}" --reset
        echo "[✓] Reset terminé."
    fi
    if [[ "\$_HAS_SEED" == true ]]; then
        echo "→ --seed : reset total + données de démo…"
        echo "[DEBUG] dotnet \${API_DIR}/\${APP_DLL} --seed"
        dotnet "\${API_DIR}/\${APP_DLL}" --seed
        echo "[✓] Seed terminé."
    fi
else
    echo "→ Base conservée. Options disponibles : --reset (schéma vide) ou --seed (schéma vide + démo)."
fi

# ── Démarrage ─────────────────────────────────────────────────────────────────
echo "→ Démarrage du service \${APP_NAME}-api…"
systemctl start "\${APP_NAME}-api"
systemctl status "\${APP_NAME}-api" --no-pager

echo "✓ Déploiement terminé."
rm -rf "/tmp/\${APP_NAME}-api.zip" "/tmp/\${APP_NAME}-api-extract"
DEPLOY

chmod +x "/usr/local/bin/deploy-${APP_NAME}-api.sh"
log "Script /usr/local/bin/deploy-${APP_NAME}-api.sh créé."
next "Étape 16/16 — Vérifications finales (dernière étape)"

# =============================================================================
# 16. VÉRIFICATIONS FINALES
# =============================================================================
section "16. Vérifications finales"

echo ""
info "État des services :"
systemctl is-active nginx          && log "nginx              : actif"  || warn "nginx              : inactif"
systemctl is-active mysql          && log "mysql              : actif"  || warn "mysql              : inactif"
systemctl is-enabled "${APP_NAME}-api" &>/dev/null \
    && log "${APP_NAME}-api       : activé au démarrage" \
    || warn "${APP_NAME}-api       : non activé"

echo ""
info "Connexion MySQL (test) :"
MYSQL_PWD="${DB_PASSWORD}" mysql --user="${DB_USER}" "${DB_NAME}" \
    -e "SELECT 'OK' AS connexion;" 2>/dev/null \
    && log "Connexion MySQL $DB_USER : OK" \
    || warn "Connexion MySQL $DB_USER : ÉCHEC"

echo ""
nginx -t && log "Configuration Nginx : syntaxe OK" || warn "Configuration Nginx : erreur de syntaxe"

# =============================================================================
# RÉSUMÉ FINAL
# =============================================================================
section "Installation terminée — $APP_NAME"

cat <<SUMMARY

  APP_NAME         : ${APP_NAME}
  Domaine          : https://${DOMAIN}
  API (local)      : http://127.0.0.1:${API_PORT}
  Frontend         : /var/www/${APP_NAME}/frontend/
  API              : /var/www/${APP_NAME}/api/
  Secrets          : /etc/${APP_NAME}/env  (chmod 600)
  Service          : systemctl [start|stop|status] ${APP_NAME}-api
  Logs API         : journalctl -u ${APP_NAME}-api -f
  Logs Nginx       : tail -f /var/log/nginx/error.log

  ──────────────────────────────────────────────────────
  ÉTAPES SUIVANTES (depuis votre machine de dev) :

  1. Déployer le backend :
       bash scripts/deploy-backend-expatriate.sh

  2. Déployer le frontend :
       bash scripts/deploy-frontend-expatriate.sh

  3. Le seed (reset total + super_admin + données de démo) est automatiquement
       exécuté à chaque déploiement backend via deploy-${APP_NAME}-api.sh.

  4. Vérifications :
       curl https://${DOMAIN}/api/v1/health
       journalctl -u ${APP_NAME}-api --since "5 min ago"

  ──────────────────────────────────────────────────────
  Clé JWT        : grep Jwt__Key /etc/${APP_NAME}/env
  Super admin    : ${SEED_ADMIN_EMAIL} (mot de passe masqué)
  ──────────────────────────────────────────────────────

SUMMARY
