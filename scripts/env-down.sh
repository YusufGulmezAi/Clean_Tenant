#!/bin/bash
# CleanTenant — Ortam Docker Stack'ini Durdurur (volume'lar korunur)
# Kullanım: ./scripts/env-down.sh <Development|Test|Demo|Production>

set -e

if [ -z "$1" ]; then
    echo "Hata: Ortam parametresi gerekli." >&2
    echo "Kullanım: $0 <Development|Test|Demo|Production>" >&2
    exit 1
fi

ENV="$1"
case "$ENV" in
    Development|Test|Demo|Production) ;;
    *) echo "Hata: Geçersiz ortam '$ENV'." >&2; exit 1 ;;
esac

ENV_LOWER=$(echo "$ENV" | tr '[:upper:]' '[:lower:]')
ROOT_PATH="$(cd "$(dirname "$0")/.." && pwd)"
ENV_FILE="$ROOT_PATH/.env.$ENV_LOWER"
COMPOSE_BASE="$ROOT_PATH/compose/docker-compose.yml"
COMPOSE_OVERRIDE="$ROOT_PATH/compose/docker-compose.$ENV_LOWER.yml"

echo ""
echo "CleanTenant '$ENV' ortamı durduruluyor (volume'lar korunur)..."
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_BASE" -f "$COMPOSE_OVERRIDE" down

echo "Tamam. Veriler korundu."
