#!/bin/bash
# CleanTenant — Seed Verisi Yükleme (iskelet)
# Faz v0.1.4 alt fazında doldurulacaktır.
# Kullanım: ./scripts/env-seed.sh <Development|Test|Demo|Production>

set -e

if [ -z "$1" ]; then
    echo "Hata: Ortam parametresi gerekli." >&2
    exit 1
fi

ENV="$1"
echo ""
echo "[ISKELET] '$ENV' ortamı için seed verisi yüklenecek."
echo "Bu script Faz v0.1.4 alt fazında dolacaktır."
echo ""
exit 0
