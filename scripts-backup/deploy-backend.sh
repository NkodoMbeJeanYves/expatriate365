#!/usr/bin/env bash
# =============================================================================
# School365 — Déploiement du backend ASP.NET Core
#
# Usage (depuis c:\dev\school365, avec Git Bash / WSL) :
#   bash scripts/deploy-backend.sh
#
# Prérequis :
#   - dotnet installé localement
#   - SSH configuré vers le VPS (clé ou mot de passe)
#   - deploy-api.sh déjà présent sur le VPS (/usr/local/bin/deploy-api.sh)
# =============================================================================
set -euo pipefail

DOMAIN="school365hub.poweryoursaas.com"
PROJECT="server/server.csproj"
PUBLISH_DIR="./publish/api"
ZIP_PATH="./publish/api.zip"

# ─── Vérification du répertoire ──────────────────────────────────────────────
if [ ! -f "$PROJECT" ]; then
  echo "[✗] Ce script doit être exécuté depuis la racine du projet (là où se trouve server/server.csproj)"
  echo "    Répertoire actuel : $(pwd)"
  echo "    Exemple : cd c:/dev/school365 && bash scripts/deploy-backend.sh"
  exit 1
fi

# ─── 1. Build ─────────────────────────────────────────────────────────────────
echo "→ Build du backend (.NET)..."
rm -rf "$PUBLISH_DIR"
dotnet publish "$PROJECT" \
  -c Release \
  -o "$PUBLISH_DIR" \
  --self-contained false \
  -r linux-x64

# ─── 2. Vérification de la structure attendue ────────────────────────────────
if [ ! -f "$PUBLISH_DIR/server.dll" ]; then
  echo "[✗] server.dll absent dans $PUBLISH_DIR — le build a échoué"
  exit 1
fi

# ─── 3. Création du zip ──────────────────────────────────────────────────────
echo "→ Création de l'archive..."
rm -f "$ZIP_PATH"
# La structure dans le zip doit être : api/server.dll, api/*.dll, etc.
if command -v zip &>/dev/null; then
  (cd publish && zip -r api.zip api/)
else
  # Fallback PowerShell (Windows sans zip natif) — pointe le dossier pour conserver api/ dans le zip
  powershell.exe -Command "Compress-Archive -Path publish\\api -DestinationPath publish\\api.zip -Force"
fi

echo "→ Contenu du zip (premières lignes) :"
unzip -l "$ZIP_PATH" | head -10

# ─── 4. Transfert vers le VPS ────────────────────────────────────────────────
echo "→ Transfert vers $DOMAIN..."
scp "$ZIP_PATH" root@"$DOMAIN":/tmp/api.zip

# ─── 5. Déploiement sur le VPS ───────────────────────────────────────────────
echo "→ Déploiement sur le VPS..."
ssh root@"$DOMAIN" "bash /usr/local/bin/deploy-api.sh"

echo ""
echo "✓ Backend déployé sur https://$DOMAIN/api/v1"
