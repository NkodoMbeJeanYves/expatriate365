#!/usr/bin/env bash
# =============================================================================
# Déploiement de l'API — script autonome uploadé sur le VPS par deploy-backend-expatriate.sh
#
# NE PAS EXÉCUTER DIRECTEMENT — uploadé et instancié par deploy-backend-expatriate.sh
#
# Modes :
#   bash /usr/local/bin/deploy-<APP_NAME>-api.sh
#   bash /usr/local/bin/deploy-<APP_NAME>-api.sh --schema-only
#   bash /usr/local/bin/deploy-<APP_NAME>-api.sh --reset
#   bash /usr/local/bin/deploy-<APP_NAME>-api.sh --seed
# =============================================================================
set -euo pipefail

APP_NAME="__APP_NAME__"
APP_DLL="__APP_DLL__"
API_DIR="/var/www/${APP_NAME}/api"
BACKUP_DIR="/var/backups/${APP_NAME}/api"
ENV_FILE="/etc/${APP_NAME}/env"
SCHEMA_FILE="/tmp/${APP_NAME}-schema.sql"

# ── Parsing des arguments ─────────────────────────────────────────────────────
SCHEMA_MODE=false
_HAS_RESET=false
_HAS_SEED=false
for _arg in "${@:-}"; do
    case "${_arg}" in
        --schema-only) SCHEMA_MODE=true ;;
        --reset)       _HAS_RESET=true  ;;
        --seed)        _HAS_SEED=true   ;;
    esac
done

echo "[DEBUG] APP_NAME=${APP_NAME}"
echo "[DEBUG] APP_DLL=${APP_DLL}"
echo "[DEBUG] API_DIR=${API_DIR}"
echo "[DEBUG] ENV_FILE=${ENV_FILE}"
echo "[DEBUG] Arguments : schema=${SCHEMA_MODE} reset=${_HAS_RESET} seed=${_HAS_SEED}"
echo "[DEBUG] ENV_FILE exists=$([ -f "${ENV_FILE}" ] && echo yes || echo NO)"

# ── Arrêt du service ──────────────────────────────────────────────────────────
echo "→ Arrêt du service ${APP_NAME}-api…"
systemctl stop "${APP_NAME}-api" || true

# ── Sauvegarde ────────────────────────────────────────────────────────────────
echo "→ Sauvegarde…"
mkdir -p "${BACKUP_DIR}"
if [ -d "${API_DIR}" ] && [ "$(ls -A ${API_DIR})" ]; then
    tar -czf "${BACKUP_DIR}/api-$(date +%Y%m%d-%H%M%S).tar.gz" -C "${API_DIR}" . 2>/dev/null || true
fi

# ── Décompression ─────────────────────────────────────────────────────────────
echo "→ Décompression…"
mkdir -p "${API_DIR}"
unzip -o "/tmp/${APP_NAME}-api.zip" -d "/tmp/${APP_NAME}-api-extract/"

rsync -av --exclude='wwwroot/uploads' --exclude='wwwroot/logos' \
          --exclude='wwwroot/photos'  --exclude='wwwroot/documents' \
          --exclude='logs' \
          "/tmp/${APP_NAME}-api-extract/" "${API_DIR}/"

mkdir -p "${API_DIR}/wwwroot/{uploads,logos,photos,documents}"
mkdir -p "${API_DIR}/logs"

# ── Permissions ───────────────────────────────────────────────────────────────
echo "→ Permissions…"
chown -R "${APP_NAME}:${APP_NAME}" "${API_DIR}"
chmod -R 755 "${API_DIR}"

# ── Chargement des variables d'environnement (sans les afficher) ──────────────
set -a
# shellcheck source=/dev/null
source <(grep -v '^#' "${ENV_FILE}" | grep -v '^_DEPLOY' | sed 's/\r//')
set +a
echo "[DEBUG] source ENV_FILE : OK"
echo "[DEBUG] ASPNETCORE_URLS=${ASPNETCORE_URLS:-<non défini>}"
echo "[DEBUG] ConnectionStrings__MySql=$(echo "${ConnectionStrings__MySql:-<non défini>}" | sed 's/Password=[^;]*/Password=***/')"

if [[ "${SCHEMA_MODE}" == true ]]; then
    # ── Mode SQL dump ─────────────────────────────────────────────────────────
    if [ ! -f "${SCHEMA_FILE}" ]; then
        echo "[✗] Fichier ${SCHEMA_FILE} introuvable."
        echo "    Générez-le localement avec :"
        echo "      cd server && dotnet ef migrations script --idempotent -o schema.sql"
        echo "    Puis transférez-le :"
        echo "      scp schema.sql root@VPS:/tmp/${APP_NAME}-schema.sql"
        exit 1
    fi
    echo "→ Application du schéma SQL (${SCHEMA_FILE})…"
    DB_HOST=$(echo "${ConnectionStrings__MySql}" | grep -oP 'Server=\K[^;]+')
    DB_PORT=$(echo "${ConnectionStrings__MySql}" | grep -oP 'Port=\K[^;]+')
    DB_NAME_VAL=$(echo "${ConnectionStrings__MySql}" | grep -oP 'Database=\K[^;]+')
    DB_USER_VAL=$(echo "${ConnectionStrings__MySql}" | grep -oP 'User=\K[^;]+')
    DB_PASS_VAL=$(echo "${ConnectionStrings__MySql}" | grep -oP 'Password=\K[^;]+')
    MYSQL_PWD="${DB_PASS_VAL}" mysql --host="${DB_HOST}" --port="${DB_PORT:-3306}" \
          --user="${DB_USER_VAL}" "${DB_NAME_VAL}" < "${SCHEMA_FILE}" \
        && echo "[✓] Schéma appliqué avec succès." \
        || echo "[✗] Erreur lors de l'application du schéma."
    rm -f "${SCHEMA_FILE}"
else
    # ── Mode migrations EF Core (défaut) ─────────────────────────────────────
    echo "→ Migrations EF Core…"
    echo "[DEBUG] dotnet ${API_DIR}/${APP_DLL} (exists=$([ -f "${API_DIR}/${APP_DLL}" ] && echo yes || echo NO))"
    cd "${API_DIR}"
    dotnet "${APP_DLL}" -- ef database update 2>/dev/null || \
        echo "[WARN] Migration EF non exécutée en mode publié — utilisez --schema-only si Pomelo indisponible"
fi

# ── Seed / Reset — uniquement si demandé ─────────────────────────────────────
if [[ "${_HAS_RESET}" == true ]] || [[ "${_HAS_SEED}" == true ]]; then
    set -a
    # shellcheck source=/dev/null
    source <(grep -v '^#' "${ENV_FILE}" | grep -v '^_DEPLOY' | sed 's/\r//')
    set +a
    echo "[DEBUG] post-source ASPNETCORE_URLS=${ASPNETCORE_URLS:-<non défini>}"
    if [[ "${_HAS_RESET}" == true ]]; then
        echo "→ --reset : suppression du schéma et recréation (rôles + super_admin seulement)…"
        echo "[DEBUG] dotnet ${API_DIR}/${APP_DLL} --reset"
        dotnet "${API_DIR}/${APP_DLL}" --reset
        echo "[✓] Reset terminé."
    fi
    if [[ "${_HAS_SEED}" == true ]]; then
        echo "→ --seed : reset total + données de démo…"
        echo "[DEBUG] dotnet ${API_DIR}/${APP_DLL} --seed"
        dotnet "${API_DIR}/${APP_DLL}" --seed
        echo "[✓] Seed terminé."
    fi
else
    echo "→ Base conservée. Options disponibles : --reset (schéma vide) ou --seed (schéma vide + démo)."
fi

# ── Démarrage ─────────────────────────────────────────────────────────────────
echo "→ Démarrage du service ${APP_NAME}-api…"
systemctl start "${APP_NAME}-api"
systemctl status "${APP_NAME}-api" --no-pager

echo "✓ Déploiement terminé."
rm -rf "/tmp/${APP_NAME}-api.zip" "/tmp/${APP_NAME}-api-extract"
