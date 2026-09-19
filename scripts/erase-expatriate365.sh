#!/usr/bin/env bash
# =============================================================================
# Expatriate365 — Suppression complète de l'application du VPS
#
# Ce script supprime :
#   - Le service systemd expatriate365-api
#   - Les fichiers applicatifs /var/www/expatriate365/
#   - Le fichier d'environnement /etc/expatriate365/
#   - Le script de déploiement /usr/local/bin/deploy-expatriate365-api.sh
#   - Le vhost nginx (sites-available + sites-enabled)
#   - La base de données MySQL et l'utilisateur (optionnel)
#   - L'utilisateur système expatriate365 (optionnel)
#
# Ce script ne supprime PAS :
#   - MySQL lui-même (partagé avec d'autres projets)
#   - .NET / nginx / certbot (partagés avec d'autres projets)
#   - Le certificat SSL Let's Encrypt (géré par certbot)
#
# Usage depuis le VPS (en root) :
#   sed 's/\r//' scripts/erase-expatriate365.sh | ssh root@acm365hub.poweryoursaas.com "bash -s"
#
# Ou après copie :
#   scp scripts/erase-expatriate365.sh root@acm365hub.poweryoursaas.com:/tmp/
#   ssh root@acm365hub.poweryoursaas.com "sed -i 's/\r//' /tmp/erase-expatriate365.sh && bash /tmp/erase-expatriate365.sh"
# =============================================================================
set -euo pipefail

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
log()     { echo -e "${GREEN}[✓]${NC} $*"; }
info()    { echo -e "${CYAN}[→]${NC} $*"; }
warn()    { echo -e "${YELLOW}[!]${NC} $*"; }
error()   { echo -e "${RED}[✗]${NC} $*"; exit 1; }
section() { echo -e "\n${CYAN}━━━ $* ━━━${NC}"; }

[[ $EUID -ne 0 ]] && error "Ce script doit être exécuté en root : sudo bash $0"

APP_NAME="expatriate365"
APP_DLL="server.dll"
NGINX_CONF="/etc/nginx/sites-available/${APP_NAME}"
ENV_FILE="/etc/${APP_NAME}/env"
DEPLOY_SCRIPT="/usr/local/bin/deploy-${APP_NAME}-api.sh"
WWW_DIR="/var/www/${APP_NAME}"

# =============================================================================
# AVERTISSEMENT
# =============================================================================
echo ""
echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${RED}  ⚠  SUPPRESSION COMPLÈTE DE L'APPLICATION ${APP_NAME}  ⚠${NC}"
echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
warn "Seront supprimés :"
echo "  • Service systemd  : ${APP_NAME}-api"
echo "  • Fichiers app     : ${WWW_DIR}/"
echo "  • Env secrets      : /etc/${APP_NAME}/"
echo "  • Script deploy    : ${DEPLOY_SCRIPT}"
echo "  • Vhost nginx      : ${NGINX_CONF}"
echo ""
warn "Optionnel (à confirmer) :"
echo "  • Base MySQL + utilisateur DB"
echo "  • Utilisateur système ${APP_NAME}"
echo ""
read -rp "Confirmer la suppression ? (oui/non) : " CONFIRM
[[ "$CONFIRM" != "oui" ]] && { warn "Annulé."; exit 0; }

# =============================================================================
# 1. ARRÊT ET SUPPRESSION DU SERVICE SYSTEMD
# =============================================================================
section "1. Service systemd ${APP_NAME}-api"

if systemctl is-active --quiet "${APP_NAME}-api" 2>/dev/null; then
    systemctl stop "${APP_NAME}-api"
    log "Service arrêté."
fi

if systemctl is-enabled --quiet "${APP_NAME}-api" 2>/dev/null; then
    systemctl disable "${APP_NAME}-api"
    log "Service désactivé."
fi

SERVICE_FILE="/etc/systemd/system/${APP_NAME}-api.service"
if [[ -f "$SERVICE_FILE" ]]; then
    rm -f "$SERVICE_FILE"
    systemctl daemon-reload
    log "Fichier service supprimé."
else
    info "Fichier service introuvable — ignoré."
fi

# =============================================================================
# 2. FICHIERS APPLICATIFS
# =============================================================================
section "2. Fichiers applicatifs ${WWW_DIR}"

if [[ -d "$WWW_DIR" ]]; then
    rm -rf "$WWW_DIR"
    log "${WWW_DIR} supprimé."
else
    info "${WWW_DIR} introuvable — ignoré."
fi

# =============================================================================
# 3. FICHIER D'ENVIRONNEMENT
# =============================================================================
section "3. Secrets /etc/${APP_NAME}"

if [[ -d "/etc/${APP_NAME}" ]]; then
    rm -rf "/etc/${APP_NAME}"
    log "/etc/${APP_NAME} supprimé."
else
    info "/etc/${APP_NAME} introuvable — ignoré."
fi

# =============================================================================
# 4. SCRIPT DE DÉPLOIEMENT
# =============================================================================
section "4. Script de déploiement"

if [[ -f "$DEPLOY_SCRIPT" ]]; then
    rm -f "$DEPLOY_SCRIPT"
    log "${DEPLOY_SCRIPT} supprimé."
else
    info "${DEPLOY_SCRIPT} introuvable — ignoré."
fi

# =============================================================================
# 5. VHOST NGINX
# =============================================================================
section "5. Vhost nginx"

if [[ -f "${NGINX_CONF}" ]]; then
    rm -f "${NGINX_CONF}"
    log "sites-available/${APP_NAME} supprimé."
fi

if [[ -L "/etc/nginx/sites-enabled/${APP_NAME}" ]]; then
    rm -f "/etc/nginx/sites-enabled/${APP_NAME}"
    log "sites-enabled/${APP_NAME} supprimé."
fi

# Réactiver le vhost default si aucun autre site n'est actif
if [[ -z "$(ls /etc/nginx/sites-enabled/ 2>/dev/null)" ]]; then
    if [[ -f /etc/nginx/sites-available/default ]]; then
        ln -sf /etc/nginx/sites-available/default /etc/nginx/sites-enabled/default
        warn "Aucun vhost actif — vhost default nginx réactivé."
    fi
fi

nginx -t && service nginx reload && log "Nginx rechargé."

# =============================================================================
# 6. BASE MYSQL (optionnel)
# =============================================================================
section "6. Base MySQL (optionnel)"

# Lire les credentials depuis l'env si encore disponible (supprimé à l'étape 3)
# Demander explicitement
echo ""
read -rp "Supprimer la base MySQL et l'utilisateur DB ? (oui/non) [non] : " _DROP_DB
_DROP_DB="${_DROP_DB:-non}"

if [[ "$_DROP_DB" == "oui" ]]; then
    read -rp "  Nom de la base MySQL [${APP_NAME}] : " DB_NAME
    DB_NAME="${DB_NAME:-${APP_NAME}}"
    read -rp "  Utilisateur MySQL [${APP_NAME}_user] : " DB_USER
    DB_USER="${DB_USER:-${APP_NAME}_user}"
    read -rsp "  Mot de passe root MySQL : " MYSQL_ROOT_PASSWORD; echo

    _mysql_root() {
        if MYSQL_PWD="" mysql --user=root --execute="SELECT 1;" 2>/dev/null; then
            MYSQL_PWD="" mysql --user=root "$@"
        elif MYSQL_PWD="${MYSQL_ROOT_PASSWORD}" mysql --user=root --execute="SELECT 1;" 2>/dev/null; then
            MYSQL_PWD="${MYSQL_ROOT_PASSWORD}" mysql --user=root "$@"
        else
            warn "Connexion MySQL root impossible — base non supprimée."
            return 1
        fi
    }

    _mysql_root <<SQL 2>/dev/null && log "Base ${DB_NAME} et utilisateur ${DB_USER} supprimés." || warn "Erreur lors de la suppression MySQL."
DROP DATABASE IF EXISTS ${DB_NAME};
DROP USER IF EXISTS '${DB_USER}'@'localhost';
FLUSH PRIVILEGES;
SQL
else
    info "Base MySQL conservée."
fi

# =============================================================================
# 7. UTILISATEUR SYSTÈME (optionnel)
# =============================================================================
section "7. Utilisateur système ${APP_NAME} (optionnel)"

echo ""
read -rp "Supprimer l'utilisateur système ${APP_NAME} ? (oui/non) [non] : " _DROP_USER
_DROP_USER="${_DROP_USER:-non}"

if [[ "$_DROP_USER" == "oui" ]]; then
    if id "${APP_NAME}" &>/dev/null; then
        userdel "${APP_NAME}" 2>/dev/null && log "Utilisateur ${APP_NAME} supprimé."
    else
        info "Utilisateur ${APP_NAME} introuvable — ignoré."
    fi
else
    info "Utilisateur système conservé."
fi

# =============================================================================
# RÉSUMÉ
# =============================================================================
echo ""
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}  ✓ Suppression de ${APP_NAME} terminée${NC}"
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo "  MySQL, .NET, nginx et les certificats SSL sont conservés."
echo "  Pour réinstaller : sudo bash /tmp/setup-vps-expatriate.sh"
echo ""
