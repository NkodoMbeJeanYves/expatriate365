#!/bin/bash
# setup-downloads.sh
# Script pour créer les dossiers de téléchargement et configurer les droits

# Variables
BASE_DIR="/var/www/expatriate365/api"
DOWNLOAD_DIR="$BASE_DIR/downloads"

# Création des dossiers
echo "📂 Création des dossiers..."
sudo mkdir -p "$DOWNLOAD_DIR/attachments"
sudo mkdir -p "$DOWNLOAD_DIR/avatars"
sudo mkdir -p "$DOWNLOAD_DIR/branding"
sudo mkdir -p "$DOWNLOAD_DIR/docs"
# sudo mkdir -p "$DOWNLOAD_DIR/avatars"
# sudo mkdir -p "$DOWNLOAD_DIR/pictures"

# Attribution des droits à www-data
echo "🔒 Attribution des droits..."
sudo chown -R www-data:www-data "$DOWNLOAD_DIR"
sudo chmod -R 755 "$DOWNLOAD_DIR"

# Ajouter l'utilisateur API (exemple: school365) au groupe www-data
echo "👤 Ajout de l'utilisateur API au groupe www-data..."
sudo usermod -a -G www-data expatriate365

# Vérification de la config Nginx
echo "🧪 Test de la configuration Nginx..."
sudo nginx -t

# Rechargement de Nginx
echo "🔄 Rechargement de Nginx..."
sudo systemctl reload nginx

echo "✅ Configuration terminée !"
echo "Les fichiers peuvent être uploadés dans:"
echo " - $DOWNLOAD_DIR/docs"
echo " - $DOWNLOAD_DIR/avatars"
echo " - $DOWNLOAD_DIR/attachments"
echo " - $DOWNLOAD_DIR/branding"
