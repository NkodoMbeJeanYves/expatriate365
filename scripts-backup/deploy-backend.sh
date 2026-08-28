#!/usr/bin/env bash
# =============================================================================
# acm365hub — Déploiement du backend ASP.NET Core
#
# Usage : bash scripts/deploy-backend-expatriate.sh
# =============================================================================
set -euo pipefail

DOMAIN="acm365hub.poweryoursaas.com"
PROJECT="server/server.csproj"
PUBLISH_DIR="./publish/expatriate/api"
ZIP_PATH="./publish/api-expatriate.zip"

# Vérification
if [ ! -f "$PROJECT" ]; then
  echo "[✗] Ce script doit être exécuté depuis la racine du projet"
  exit 1
fi

# 1. Build
# echo "→ Build du backend acm365hub (.NET)..."
# rm -rf "$PUBLISH_DIR"
# dotnet publish "$PROJECT" \
#   -c Release \
#   -o "$PUBLISH_DIR" \
#   --self-contained false \
#   -r linux-x64

if [ ! -f "$PUBLISH_DIR/server.dll" ]; then
  echo "[✗] server.dll absent dans $PUBLISH_DIR"
  exit 1
fi

# 2. Création du zip
echo "→ Création de l'archive..."
# rm -f "$ZIP_PATH"
# if command -v zip &>/dev/null; then
#   (cd publish/expatriate && zip -r api-expatriate.zip api/)
# else
#   powershell.exe -Command "Compress-Archive -Path publish\\expatriate\\api -DestinationPath publish\\api-expatriate.zip -Force"
# fi

# 3. Transfert vers le VPS
echo "→ Transfert vers $DOMAIN..."
scp "$ZIP_PATH" root@"$DOMAIN":/tmp/api-expatriate.zip

# 4. Déploiement sur le VPS
echo "→ Déploiement sur le VPS..."
ssh root@"$DOMAIN" "bash /usr/local/bin/deploy-expatriate-api.sh"

echo ""
echo "✓ Backend acm365hub déployé sur https://$DOMAIN/api/v1"
