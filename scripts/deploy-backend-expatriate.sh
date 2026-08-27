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
#   - setup-vps-expatriate.sh exécuté une fois sur le VPS (nginx, systemd, user, env file)
#   - scripts/vps-deploy-api.sh présent localement (uploadé automatiquement à chaque déploiement)
# =============================================================================
set -euo pipefail

# ─── Paramètres — modifiables ─────────────────────────────────────────────────
APP_NAME="expatriate365"                       # doit correspondre à APP_NAME saisi lors du setup
APP_DLL="server.dll"                           # DLL principale du projet
DOMAIN="acm365hub.poweryoursaas.com"           # domaine SSH du VPS
PROJECT="server/server.csproj"
PUBLISH_DIR="./publish/api"
ZIP_LOCAL="./publish/${APP_NAME}-api.zip"
ZIP_REMOTE="/tmp/${APP_NAME}-api.zip"
SCHEMA_LOCAL="./publish/schema.sql"
SCHEMA_REMOTE="/tmp/${APP_NAME}-schema.sql"
DEPLOY_SCRIPT_LOCAL="scripts/vps-deploy-api.sh"
DEPLOY_SCRIPT_REMOTE="/usr/local/bin/deploy-${APP_NAME}-api.sh"

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
echo "→ Build du backend (.NET — $PROJECT)..."
rm -rf "$PUBLISH_DIR"
dotnet publish "$PROJECT" \
  -c Release \
  -o "$PUBLISH_DIR" \
  --self-contained false \
  -r linux-x64

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
rm -f "$ZIP_LOCAL"
if command -v zip &>/dev/null; then
  (cd publish && zip -r "${APP_NAME}-api.zip" api/)
else
  powershell.exe -Command "Compress-Archive -Path publish\\api -DestinationPath publish\\${APP_NAME}-api.zip -Force"
fi

echo "→ Contenu du zip (premières lignes) :"
unzip -l "$ZIP_LOCAL" | head -10

# ─── 5. Transfert ─────────────────────────────────────────────────────────────
echo "→ Transfert de l'archive vers $DOMAIN..."
scp "$ZIP_LOCAL" root@"$DOMAIN":"$ZIP_REMOTE"

if [[ "$SCHEMA_MODE" == true ]]; then
  echo "→ Transfert du schéma SQL vers $DOMAIN..."
  scp "$SCHEMA_LOCAL" root@"$DOMAIN":"$SCHEMA_REMOTE"
fi

# ─── Upload + instanciation du script de déploiement VPS ─────────────────────
echo "→ Mise à jour du script de déploiement sur le VPS..."
if [ ! -f "$DEPLOY_SCRIPT_LOCAL" ]; then
  echo "[✗] $DEPLOY_SCRIPT_LOCAL introuvable — exécutez ce script depuis la racine du projet"
  exit 1
fi
# Instancier les placeholders localement, puis uploader
_tmp_deploy=$(mktemp /tmp/vps-deploy-api-XXXX.sh)
sed -e "s|__APP_NAME__|${APP_NAME}|g" \
    -e "s|__APP_DLL__|${APP_DLL}|g" \
    "$DEPLOY_SCRIPT_LOCAL" > "$_tmp_deploy"
scp "$_tmp_deploy" root@"$DOMAIN":"$DEPLOY_SCRIPT_REMOTE"
ssh root@"$DOMAIN" "chmod +x ${DEPLOY_SCRIPT_REMOTE}"
rm -f "$_tmp_deploy"
echo "[✓] Script de déploiement mis à jour : ${DEPLOY_SCRIPT_REMOTE}"

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
