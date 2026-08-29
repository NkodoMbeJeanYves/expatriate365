#!/usr/bin/env bash
# =============================================================================
# Expatriate365 — Déploiement du frontend Angular
#
# Depuis votre machine Windows (Git Bash / WSL), à la racine du projet :
#
#   1. Déploiement automatique (build + transfert + déploiement en une commande) :
#        bash scripts/deploy-frontend-expatriate.sh
#
# ── OU manuellement étape par étape ──────────────────────────────────────────
#
#   1. Builder :
#        cd client && npm run build && cd ..
#
#   2. Créer l'archive :
#        tar -czf /tmp/expatriate365-frontend.tar.gz -C client/dist/client/browser .
#
#   3. Transférer l'archive et le script :
#        scp /tmp/expatriate365-frontend.tar.gz root@acm365hub.poweryoursaas.com:/tmp/
#        scp scripts/deploy-frontend-expatriate.sh root@acm365hub.poweryoursaas.com:/tmp/
#
#   4. Se connecter et exécuter :
#        ssh root@acm365hub.poweryoursaas.com
#        sed -i 's/\r//' /tmp/deploy-frontend-expatriate.sh
#        bash /tmp/deploy-frontend-expatriate.sh --remote
# =============================================================================
set -euo pipefail

# ─── Paramètres — modifiables ─────────────────────────────────────────────────
APP_NAME="expatriate365"                       # doit correspondre à APP_NAME saisi lors du setup
DOMAIN="acm365hub.poweryoursaas.com"           # domaine SSH du VPS
BUILD_DIR="client/dist/client/browser"
ARCHIVE_LOCAL="/tmp/${APP_NAME}-frontend.tar.gz"
ARCHIVE_REMOTE="/tmp/${APP_NAME}-frontend.tar.gz"
REMOTE_DIR="/var/www/${APP_NAME}/frontend"

# ─── Détection du mode d'exécution ───────────────────────────────────────────
REMOTE_MODE=false
[[ "${1:-}" == "--remote" ]] && REMOTE_MODE=true

# =============================================================================
# MODE DISTANT — tourne sur le VPS
# =============================================================================
if [[ "$REMOTE_MODE" == true ]]; then

    if [ ! -d "$REMOTE_DIR" ]; then
        echo "[✗] Répertoire $REMOTE_DIR introuvable — setup-vps-expatriate.sh doit avoir été exécuté."
        exit 1
    fi

    echo "→ Vérification de l'archive $ARCHIVE_REMOTE..."
    if [ ! -f "$ARCHIVE_REMOTE" ]; then
        echo "[✗] Archive introuvable : $ARCHIVE_REMOTE"
        echo "    Depuis Windows : tar -czf $ARCHIVE_LOCAL -C $BUILD_DIR ."
        echo "    Puis : scp $ARCHIVE_LOCAL root@$DOMAIN:$ARCHIVE_REMOTE"
        exit 1
    fi

    echo "→ Extraction..."
    TMP_EXTRACT="/tmp/${APP_NAME}-frontend-extract"
    rm -rf "$TMP_EXTRACT"
    mkdir -p "$TMP_EXTRACT"
    tar -xzf "$ARCHIVE_REMOTE" -C "$TMP_EXTRACT"

    echo "→ Vérification du contenu (index.html requis)..."
    if [ ! -f "$TMP_EXTRACT/index.html" ]; then
        echo "[✗] index.html absent — vérifiez outputPath dans angular.json"
        exit 1
    fi

    echo "→ Déploiement vers $REMOTE_DIR..."
    rsync -a --delete "$TMP_EXTRACT/" "$REMOTE_DIR/"
    chown -R www-data:www-data "$REMOTE_DIR"

    echo "→ Nettoyage..."
    rm -rf "$TMP_EXTRACT" "$ARCHIVE_REMOTE"

    echo ""
    echo "✓ Frontend déployé — $(ls "$REMOTE_DIR" | wc -l) fichiers dans $REMOTE_DIR"
    exit 0
fi

# =============================================================================
# MODE LOCAL — tourne sur la machine de dev
# =============================================================================

if [ ! -f "client/angular.json" ] && [ ! -f "client/package.json" ]; then
    echo "[✗] Ce script doit être exécuté depuis la racine du projet."
    echo "    Répertoire actuel : $(pwd)"
    exit 1
fi

# ─── 1. Build Angular ─────────────────────────────────────────────────────────
echo "→ Build Angular (production)..."
(cd client && npm run build)

# ─── 2. Vérification ─────────────────────────────────────────────────────────
if [ ! -f "$BUILD_DIR/index.html" ]; then
    echo "[✗] index.html absent dans $BUILD_DIR — le build a échoué"
    exit 1
fi

# ─── 3. Archive ──────────────────────────────────────────────────────────────
echo "→ Création de l'archive $ARCHIVE_LOCAL..."
tar -czf "$ARCHIVE_LOCAL" -C "$BUILD_DIR" .

# ─── 4. Transfert ─────────────────────────────────────────────────────────────
echo "→ Transfert vers $DOMAIN ($ARCHIVE_REMOTE)..."
scp "$ARCHIVE_LOCAL" root@"$DOMAIN":"$ARCHIVE_REMOTE"

# ─── 5. Déploiement distant ───────────────────────────────────────────────────
echo "→ Déploiement sur le VPS..."
sed 's/\r//' "$0" | ssh root@"$DOMAIN" "bash -s -- --remote"

echo ""
echo "✓ Frontend déployé sur https://$DOMAIN"
