#!/bin/bash
# CleanTenant — PostgreSQL init aşaması 1: Veritabanlarını oluştur.
# Bu script container'ın ilk açılışında otomatik çalışır; sonraki açılışlarda
# atlanır (volume zaten initialize edilmiş olur).
#
# Olusturulan veritabanlari:
#   - cleantenant_catalog : Tenant registry + global identity
#   - cleantenant_main    : Is verisi (entity'ler, aggregate'ler)
#   - cleantenant_log     : Serilog sink hedefi
#   - cleantenant_audit   : Append-only audit trail

set -e

echo "[CleanTenant] 4 veritabani olusturuluyor..."

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "postgres" <<-EOSQL
    CREATE DATABASE cleantenant_catalog
        WITH OWNER = $POSTGRES_USER
             ENCODING = 'UTF8'
             LC_COLLATE = 'C.UTF-8'
             LC_CTYPE   = 'C.UTF-8'
             TEMPLATE   = template0;

    CREATE DATABASE cleantenant_main
        WITH OWNER = $POSTGRES_USER
             ENCODING = 'UTF8'
             LC_COLLATE = 'C.UTF-8'
             LC_CTYPE   = 'C.UTF-8'
             TEMPLATE   = template0;

    CREATE DATABASE cleantenant_log
        WITH OWNER = $POSTGRES_USER
             ENCODING = 'UTF8'
             LC_COLLATE = 'C.UTF-8'
             LC_CTYPE   = 'C.UTF-8'
             TEMPLATE   = template0;

    CREATE DATABASE cleantenant_audit
        WITH OWNER = $POSTGRES_USER
             ENCODING = 'UTF8'
             LC_COLLATE = 'C.UTF-8'
             LC_CTYPE   = 'C.UTF-8'
             TEMPLATE   = template0;
EOSQL

echo "[CleanTenant] 4 veritabani basariyla olusturuldu."
