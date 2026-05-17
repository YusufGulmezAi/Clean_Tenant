#!/bin/bash
# CleanTenant — PostgreSQL init aşaması 2: Extension'ları yükle.
# 01-init-databases.sh sonrası tüm 4 DB için extension'lar kurulur.
#
# Extension'lar:
#   - citext   : Case-insensitive metin tipi (e-posta, kullaniciAdi vb.)
#   - unaccent : Aksan/Türkçe karakter normalizasyonu (arama icin)
#   - pg_trgm  : Trigram benzerligi (fuzzy search + GIN index)
#   - pgcrypto : gen_random_uuid() ve şifreleme primitif'leri

set -e

DATABASES=(cleantenant_catalog cleantenant_main cleantenant_log cleantenant_audit)

for db in "${DATABASES[@]}"; do
    echo "[CleanTenant] $db: extension'lar yukleniyor..."
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$db" <<-EOSQL
        CREATE EXTENSION IF NOT EXISTS citext;
        CREATE EXTENSION IF NOT EXISTS unaccent;
        CREATE EXTENSION IF NOT EXISTS pg_trgm;
        CREATE EXTENSION IF NOT EXISTS pgcrypto;
EOSQL
done

echo "[CleanTenant] Tum extension'lar yuklendi."
