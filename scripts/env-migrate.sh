#!/bin/bash
# CleanTenant — EF Core Migration Uygulama (iskelet)
# Faz v0.1.4 alt fazında doldurulacaktır.
# Kullanım: ./scripts/env-migrate.sh <Development|Test|Demo|Production>

set -e

if [ -z "$1" ]; then
    echo "Hata: Ortam parametresi gerekli." >&2
    exit 1
fi

ENV="$1"
echo ""
echo "[ISKELET] '$ENV' ortamı için EF Core migration uygulaması."
echo "Bu script Faz v0.1.4 alt fazında dolacaktır:"
echo "  - dotnet ef database update --context CatalogDbContext"
echo "  - dotnet ef database update --context MainDbContext"
echo "  - dotnet ef database update --context LogDbContext"
echo "  - dotnet ef database update --context AuditDbContext"
echo ""
exit 0
