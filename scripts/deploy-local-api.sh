#!/usr/bin/env bash
# =============================================================================
# Expatriate365 — Redéploiement backend (local WSL2)
#
# Prérequis : setup-local-wsl.sh déjà exécuté
#
# Usage :
#   sed 's/\r//' /mnt/c/asp/expatriate365/scripts/deploy-local-api.sh > /tmp/deploy-local-api.sh
#   sudo bash /tmp/deploy-local-api.sh
#
# Valeur par défaut : ~/api.zip
# =============================================================================
set -euo pipefail

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
log()     { echo -e "${GREEN}[✓]${NC} $*"; }
info()    { echo -e "${CYAN}[→]${NC} $*"; }
warn()    { echo -e "${YELLOW}[!]${NC} $*"; }
error()   { echo -e "${RED}[✗]${NC} $*"; exit 1; }

[[ $EUID -ne 0 ]] && error "Exécuter en root : sudo bash $0"

APP_NAME="expatriate365"
APP_DLL="server.dll"
DEPLOY_DIR="/var/www/${APP_NAME}/api"

# ── Chemin zip ────────────────────────────────────────────────────────────────
read -rp "Chemin vers api.zip [~/api.zip] : " API_ZIP
API_ZIP="${API_ZIP:-$HOME/api.zip}"
API_ZIP="${API_ZIP/#\~/$HOME}"
[[ ! -f "$API_ZIP" ]] && error "Fichier introuvable : $API_ZIP"

# ── DB action ─────────────────────────────────────────────────────────────────
echo ""
echo "  [1] Conserver la base (défaut)"
echo "  [2] --reset"
echo "  [3] --seed"
read -rp "DB action [1] : " _CHOICE
_CHOICE="${_CHOICE:-1}"
_FLAG=""
[[ "$_CHOICE" == "2" ]] && _FLAG="--reset"
[[ "$_CHOICE" == "3" ]] && _FLAG="--seed"

echo ""
info "api.zip : $API_ZIP  |  DB : ${_FLAG:-conserver}"
read -rp "Confirmer ? (oui/non) : " CONFIRM
[[ "$CONFIRM" != "oui" ]] && { warn "Annulé."; exit 0; }

# ── Arrêt ─────────────────────────────────────────────────────────────────────
if systemctl is-active --quiet "${APP_NAME}-api" 2>/dev/null; then
    systemctl stop "${APP_NAME}-api"
    log "Service arrêté."
fi

# ── Extraction + rsync ────────────────────────────────────────────────────────
EXTRACT="/tmp/${APP_NAME}-api-extract"
rm -rf "$EXTRACT" && mkdir -p "$EXTRACT"
info "Extraction..."
unzip -q "$API_ZIP" -d "$EXTRACT"

info "Déploiement vers $DEPLOY_DIR..."
rsync -a \
    --exclude='downloads/' \
    --exclude='logs/' \
    "$EXTRACT/" "$DEPLOY_DIR/"

mkdir -p "${DEPLOY_DIR}/downloads/{attachments,branding,avatars,docs}"
mkdir -p "${DEPLOY_DIR}/logs"
chown -R "${APP_NAME}:${APP_NAME}" "$DEPLOY_DIR"
chown -R "${APP_NAME}:www-data" "${DEPLOY_DIR}/downloads"
chmod -R 750 "${DEPLOY_DIR}/downloads"
find "${DEPLOY_DIR}/downloads" -type d -exec chmod g+s {} \;
rm -rf "$EXTRACT"
log "Fichiers déployés."

# ── DB action (--reset / --seed) — s'exécute et se termine sans démarrer le serveur
if [[ -n "$_FLAG" ]]; then
    info "Application de $_FLAG..."
    set -a
    # shellcheck source=/dev/null
    source <(grep -v '^#' "/etc/${APP_NAME}/env" | { grep -v '^_DEPLOY' || true; } | sed 's/\r//')
    set +a
    dotnet "${DEPLOY_DIR}/${APP_DLL}" "$_FLAG"
    log "DB action $_FLAG terminée."
fi

# ── Redémarrage ───────────────────────────────────────────────────────────────
systemctl start "${APP_NAME}-api"
sleep 2
systemctl is-active --quiet "${APP_NAME}-api" \
    && log "Service démarré." \
    || warn "Service en erreur — vérifier : journalctl -u ${APP_NAME}-api -n 50"

# ── Résumé ────────────────────────────────────────────────────────────────────
echo ""
echo -e "${GREEN}✓ API redéployée — http://localhost/api/v1  |  http://localhost/scalar${NC}"
echo ""
echo "  Logs : sudo journalctl -u ${APP_NAME}-api -f"
