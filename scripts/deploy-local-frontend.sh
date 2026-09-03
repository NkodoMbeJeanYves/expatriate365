#!/usr/bin/env bash
# =============================================================================
# Expatriate365 — Redéploiement frontend (local WSL2)
#
# Prérequis : setup-local-wsl.sh déjà exécuté
#
# Usage :
#   sed 's/\r//' /mnt/c/asp/expatriate365/scripts/deploy-local-frontend.sh > /tmp/deploy-local-frontend.sh
#   sudo bash /tmp/deploy-local-frontend.sh
#
# Valeur par défaut : ~/frontend.zip
# =============================================================================
set -euo pipefail

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
log()     { echo -e "${GREEN}[✓]${NC} $*"; }
info()    { echo -e "${CYAN}[→]${NC} $*"; }
warn()    { echo -e "${YELLOW}[!]${NC} $*"; }
error()   { echo -e "${RED}[✗]${NC} $*"; exit 1; }

[[ $EUID -ne 0 ]] && error "Exécuter en root : sudo bash $0"

APP_NAME="expatriate365"
DEPLOY_DIR="/var/www/${APP_NAME}/frontend"

# ── Chemin zip ────────────────────────────────────────────────────────────────
read -rp "Chemin vers frontend.zip [~/frontend.zip] : " FRONTEND_ZIP
FRONTEND_ZIP="${FRONTEND_ZIP:-$HOME/frontend.zip}"
FRONTEND_ZIP="${FRONTEND_ZIP/#\~/$HOME}"
[[ ! -f "$FRONTEND_ZIP" ]] && error "Fichier introuvable : $FRONTEND_ZIP"

echo ""
info "frontend.zip : $FRONTEND_ZIP"
read -rp "Confirmer ? (oui/non) : " CONFIRM
[[ "$CONFIRM" != "oui" ]] && { warn "Annulé."; exit 0; }

# ── Extraction + rsync ────────────────────────────────────────────────────────
EXTRACT="/tmp/${APP_NAME}-frontend-extract"
rm -rf "$EXTRACT" && mkdir -p "$EXTRACT"
info "Extraction..."
unzip -q "$FRONTEND_ZIP" -d "$EXTRACT"

info "Déploiement vers $DEPLOY_DIR..."
rsync -a --delete "$EXTRACT/" "$DEPLOY_DIR/"
chown -R www-data:www-data "$DEPLOY_DIR"
rm -rf "$EXTRACT"
log "Fichiers déployés."

# ── Rechargement nginx ────────────────────────────────────────────────────────
service nginx reload
log "Nginx rechargé."

# ── Résumé ────────────────────────────────────────────────────────────────────
echo ""
echo -e "${GREEN}✓ Frontend redéployé — http://localhost${NC}"
