#!/bin/bash
# setup-download.sh
# Crée les dossiers d'upload et configure les droits pour l'API et nginx.
#
# Droits :
#   - Propriétaire : expatriate365 (service app) → peut écrire les fichiers uploadés
#   - Groupe       : www-data (nginx)            → peut lire via les alias nginx
#   - Permissions  : 750 (rwxr-x---) sur les dossiers
#   - setgid       : les nouveaux fichiers héritent automatiquement du groupe www-data
#
# Usage : sudo bash setup-download.sh
set -euo pipefail

APP_NAME="expatriate365"
BASE_DIR="/var/www/${APP_NAME}/api"
DOWNLOAD_DIR="${BASE_DIR}/downloads"

# ── Création des dossiers ──────────────────────────────────────────────────────
echo "→ Création des dossiers..."
mkdir -p "${DOWNLOAD_DIR}/attachments"
mkdir -p "${DOWNLOAD_DIR}/avatars"
mkdir -p "${DOWNLOAD_DIR}/branding"
mkdir -p "${DOWNLOAD_DIR}/docs"

# ── Propriétaire : service app ; groupe : www-data ────────────────────────────
echo "→ Attribution des droits..."
chown -R "${APP_NAME}:www-data" "${DOWNLOAD_DIR}"

# ── 750 : service app lit/écrit, nginx (www-data) lit, autres : rien ──────────
chmod -R 750 "${DOWNLOAD_DIR}"

# ── setgid sur les dossiers : nouveaux fichiers héritent du groupe www-data ───
find "${DOWNLOAD_DIR}" -type d -exec chmod g+s {} \;

# ── Vérification ──────────────────────────────────────────────────────────────
echo ""
echo "→ Résultat :"
ls -la "${DOWNLOAD_DIR}"

# ── Nginx ─────────────────────────────────────────────────────────────────────
echo ""
echo "→ Test de la configuration Nginx..."
nginx -t

echo "→ Rechargement de Nginx..."
systemctl reload nginx

echo ""
echo "✓ Configuration terminée."
echo "  Dossiers d'upload disponibles :"
echo "    ${DOWNLOAD_DIR}/attachments"
echo "    ${DOWNLOAD_DIR}/avatars"
echo "    ${DOWNLOAD_DIR}/branding"
echo "    ${DOWNLOAD_DIR}/docs"
