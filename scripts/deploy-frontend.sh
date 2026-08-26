#!/usr/bin/env bash
# =============================================================================
# School365 — Déploiement du frontend Angular
#
# Depuis votre machine Windows (Git Bash / WSL), à la racine du projet :
#
#   1. Builder :
#        cd client && npm run build && cd ..
#
#   2. Créer l'archive :
#        tar -czf /tmp/frontend.tar.gz -C client/dist/client/browser .
#
#   3. Transférer le script et l'archive :
#        scp /tmp/frontend.tar.gz root@school365hub.poweryoursaas.com:/tmp/
#        scp scripts/deploy-frontend.sh root@school365hub.poweryoursaas.com:/tmp/
#
#   4. Se connecter et exécuter :
#        ssh root@school365hub.poweryoursaas.com
#        sed -i 's/\r//' /tmp/deploy-frontend.sh
#        bash /tmp/deploy-frontend.sh
# =============================================================================
set -euo pipefail

REMOTE_DIR="/var/www/school365/frontend"
ARCHIVE="/tmp/frontend.tar.gz"
TMP_EXTRACT="/tmp/frontend-extract"

# ─── Vérification de l'environnement VPS ─────────────────────────────────────
if [ ! -d "/var/www/school365" ]; then
  echo "[✗] Répertoire /var/www/school365 introuvable — ce script doit tourner sur le VPS après setup"
  exit 1
fi

echo "→ Vérification de l'archive..."
if [ ! -f "$ARCHIVE" ]; then
  echo "[✗] Archive introuvable : $ARCHIVE"
  echo "    Depuis Windows : tar -czf /tmp/frontend.tar.gz -C client/dist/client/browser ."
  echo "    Puis : scp /tmp/frontend.tar.gz root@VOTRE_VPS:/tmp/"
  exit 1
fi

echo "→ Extraction..."
rm -rf "$TMP_EXTRACT"
mkdir -p "$TMP_EXTRACT"
tar -xzf "$ARCHIVE" -C "$TMP_EXTRACT"

echo "→ Vérification du contenu (index.html requis)..."
if [ ! -f "$TMP_EXTRACT/index.html" ]; then
  echo "[✗] index.html absent dans l'archive — vérifiez le chemin du build"
  exit 1
fi

echo "→ Déploiement vers $REMOTE_DIR..."
rsync -a --delete "$TMP_EXTRACT/" "$REMOTE_DIR/"
chown -R www-data:www-data "$REMOTE_DIR"

echo "→ Nettoyage..."
rm -rf "$TMP_EXTRACT" "$ARCHIVE"

echo ""
echo "✓ Frontend déployé — $(ls $REMOTE_DIR | wc -l) fichiers dans $REMOTE_DIR"
