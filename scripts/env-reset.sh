#!/bin/bash
# CleanTenant — Ortam Sıfırlama (TÜM VOLUME'LAR SİLİNİR)
# Kullanım: ./scripts/env-reset.sh <Development|Test|Demo|Production> [--force]

set -e

if [ -z "$1" ]; then
    echo "Hata: Ortam parametresi gerekli." >&2
    echo "Kullanım: $0 <Development|Test|Demo|Production> [--force]" >&2
    exit 1
fi

ENV="$1"
case "$ENV" in
    Development|Test|Demo|Production) ;;
    *) echo "Hata: Geçersiz ortam '$ENV'." >&2; exit 1 ;;
esac

FORCE=false
if [ "$2" = "--force" ]; then
    FORCE=true
fi

ENV_LOWER=$(echo "$ENV" | tr '[:upper:]' '[:lower:]')
ROOT_PATH="$(cd "$(dirname "$0")/.." && pwd)"
ENV_FILE="$ROOT_PATH/.env.$ENV_LOWER"
COMPOSE_BASE="$ROOT_PATH/compose/docker-compose.yml"
COMPOSE_OVERRIDE="$ROOT_PATH/compose/docker-compose.$ENV_LOWER.yml"

if [ "$FORCE" = false ]; then
    echo ""
    echo "============================================================"
    echo "  UYARI: '$ENV' ortamındaki TÜM VERİLER silinecek."
    echo "  Postgres + Redis + Seq volume'ları kaldırılacak."
    echo "  Bu işlemin geri dönüşü yoktur."
    echo "============================================================"

    if [ "$ENV" = "Production" ]; then
        echo ""
        echo "PRODUCTION ortamını sıfırlıyorsunuz. Bir kez daha düşünün."
    fi

    echo ""
    read -p "Devam etmek için ortam adını yazın ('$ENV'): " confirm
    if [ "$confirm" != "$ENV" ]; then
        echo "İptal edildi."
        exit 0
    fi
fi

echo ""
echo "'$ENV' ortamı sıfırlanıyor..."
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_BASE" -f "$COMPOSE_OVERRIDE" down --volumes --remove-orphans

echo "Tamam. '$ENV' ortamı temiz."
