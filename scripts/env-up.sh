#!/bin/bash
# CleanTenant — Ortam Docker Stack'ini Başlatır
# Kullanım: ./scripts/env-up.sh <Development|Test|Demo|Production>

set -e

if [ -z "$1" ]; then
    echo "Hata: Ortam parametresi gerekli." >&2
    echo "Kullanım: $0 <Development|Test|Demo|Production>" >&2
    exit 1
fi

ENV="$1"
case "$ENV" in
    Development|Test|Demo|Production) ;;
    *) echo "Hata: Geçersiz ortam '$ENV'. Geçerli: Development, Test, Demo, Production." >&2; exit 1 ;;
esac

ENV_LOWER=$(echo "$ENV" | tr '[:upper:]' '[:lower:]')
ROOT_PATH="$(cd "$(dirname "$0")/.." && pwd)"
ENV_FILE="$ROOT_PATH/.env.$ENV_LOWER"
COMPOSE_BASE="$ROOT_PATH/compose/docker-compose.yml"
COMPOSE_OVERRIDE="$ROOT_PATH/compose/docker-compose.$ENV_LOWER.yml"

if ! docker info > /dev/null 2>&1; then
    echo "Hata: Docker bulunamadı veya engine çalışmıyor." >&2
    exit 1
fi

if [ ! -f "$ENV_FILE" ]; then
    echo "Hata: $ENV_FILE bulunamadı. Önce '.env.$ENV_LOWER.example' dosyasını kopyalayıp düzenleyin." >&2
    exit 1
fi

if [ ! -f "$COMPOSE_OVERRIDE" ]; then
    echo "Hata: Compose override dosyası bulunamadı: $COMPOSE_OVERRIDE" >&2
    exit 1
fi

echo ""
echo "CleanTenant '$ENV' ortamı başlatılıyor..."
echo "  Env dosyası   : $ENV_FILE"
echo "  Compose base  : $COMPOSE_BASE"
echo "  Override      : $COMPOSE_OVERRIDE"
echo ""

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_BASE" -f "$COMPOSE_OVERRIDE" up -d

echo ""
echo "Servis durumu:"
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_BASE" -f "$COMPOSE_OVERRIDE" ps

echo ""
echo "Tamam. '$ENV' ortamı ayakta."
