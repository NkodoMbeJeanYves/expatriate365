#!/usr/bin/env bash
# =============================================================================
# Expatriate365 — Déploiement du backend ASP.NET Core
#
# Mode migrations EF Core (défaut) :
#   bash scripts/deploy-backend-expatriate.sh
#
# Mode schema SQL (si Pomelo/EF Core indisponible, ex: migration .NET 9→10) :
#   bash scripts/deploy-backend-expatriate.sh --schema-only
#   bash scripts/deploy-backend-expatriate.sh            # données conservées
#   bash scripts/deploy-backend-expatriate.sh --reset    # schéma vide + bootstrap
#   bash scripts/deploy-backend-expatriate.sh --seed     # schéma vide + démo complète
#
# Prérequis :
#   - dotnet installé localement
#   - SSH configuré vers le VPS
#   - deploy-${APP_NAME}-api.sh généré sur le VPS par setup-vps-expatriate.sh
# =============================================================================
set -euo pipefail

# ─── Paramètres — modifiables ─────────────────────────────────────────────────
APP_NAME="expatriate365"                       # doit correspondre à APP_NAME saisi lors du setup
DOMAIN="acm365hub.poweryoursaas.com"           # domaine SSH du VPS
PROJECT="server/server.csproj"
PUBLISH_DIR="./publish/api"
ZIP_LOCAL="./publish/${APP_NAME}-api.zip"
ZIP_REMOTE="/tmp/${APP_NAME}-api.zip"
SCHEMA_LOCAL="./publish/schema.sql"
SCHEMA_REMOTE="/tmp/${APP_NAME}-schema.sql"

# ─── Modes ────────────────────────────────────────────────────────────────────
SCHEMA_MODE=false
SEED_MODE=false
RESET_MODE=false
for arg in "$@"; do
  [[ "$arg" == "--schema-only" ]] && SCHEMA_MODE=true
  [[ "$arg" == "--seed"        ]] && SEED_MODE=true
  [[ "$arg" == "--reset"       ]] && RESET_MODE=true
done

# ─── Vérification du répertoire ──────────────────────────────────────────────
if [ ! -f "$PROJECT" ]; then
  echo "[✗] Ce script doit être exécuté depuis la racine du projet ($PROJECT introuvable)"
  echo "    Répertoire actuel : $(pwd)"
  exit 1
fi

# ─── 1. Build ─────────────────────────────────────────────────────────────────
echo "→ Fichier existant Pas de Build du backend (.NET — $PROJECT)..."
# rm -rf "$PUBLISH_DIR"
# dotnet publish "$PROJECT" \
#   -c Release \
#   -o "$PUBLISH_DIR" \
#   --self-contained false \
#   -r linux-x64

# ─── 2. Vérification ─────────────────────────────────────────────────────────
if [ ! -f "$PUBLISH_DIR/server.dll" ]; then
  echo "[✗] server.dll absent dans $PUBLISH_DIR — le build a échoué"
  exit 1
fi

# ─── 3. Génération du schema SQL (mode --schema-only uniquement) ──────────────
if [[ "$SCHEMA_MODE" == true ]]; then
  echo "→ Génération du schéma SQL idempotent (dotnet ef migrations script)..."
  mkdir -p publish
  (cd server && dotnet ef migrations script --idempotent -o "../${SCHEMA_LOCAL}")
  if [ ! -f "$SCHEMA_LOCAL" ]; then
    echo "[✗] Génération du schéma SQL échouée — $SCHEMA_LOCAL introuvable"
    exit 1
  fi
  echo "[✓] Schéma généré : $SCHEMA_LOCAL"
fi

# ─── 4. Archive ──────────────────────────────────────────────────────────────
echo "→ Création de l'archive $ZIP_LOCAL..."
# rm -f "$ZIP_LOCAL"
# if command -v zip &>/dev/null; then
#   (cd publish && zip -r "${APP_NAME}-api.zip" api/)
# else
#   powershell.exe -Command "Compress-Archive -Path publish\\api\\* -DestinationPath publish\\${APP_NAME}-api.zip -Force"
# fi

echo "→ Contenu du zip (premières lignes) :"
unzip -l "$ZIP_LOCAL" | head -10

# ─── 5. Transfert ─────────────────────────────────────────────────────────────
echo "→ Transfert de l'archive vers $DOMAIN..."
scp "$ZIP_LOCAL" root@"$DOMAIN":"$ZIP_REMOTE"

if [[ "$SCHEMA_MODE" == true ]]; then
  echo "→ Transfert du schéma SQL vers $DOMAIN..."
  scp "$SCHEMA_LOCAL" root@"$DOMAIN":"$SCHEMA_REMOTE"
fi

# ─── 6. Déploiement ──────────────────────────────────────────────────────────
REMOTE_ARGS=""
[[ "$SCHEMA_MODE" == true ]] && REMOTE_ARGS="$REMOTE_ARGS --schema-only"
[[ "$RESET_MODE"  == true ]] && REMOTE_ARGS="$REMOTE_ARGS --reset"
[[ "$SEED_MODE"   == true ]] && REMOTE_ARGS="$REMOTE_ARGS --seed"

echo "→ Déploiement sur le VPS${REMOTE_ARGS:+ (args:$REMOTE_ARGS)}..."
# shellcheck disable=SC2086
ssh root@"$DOMAIN" "bash /usr/local/bin/deploy-${APP_NAME}-api.sh $REMOTE_ARGS"

echo ""
echo "✓ Backend déployé sur https://$DOMAIN/api/v1"
