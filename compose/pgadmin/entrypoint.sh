#!/bin/sh
# CleanTenant — pgAdmin custom entrypoint
#
# Görev: POSTGRES_USER ve POSTGRES_PASSWORD env değişkenlerini kullanarak
# /tmp/pgpass dosyasını runtime'da üretir (izinler 0600). servers.json bu
# dosyaya PassFile olarak işaret ettiği için kullanıcı login bypass moduyla
# açıldığında bağlantılar şifre sormadan kurulur.
#
# Şifre .env.development dosyasından değişirse pgadmin container restart
# edildiğinde dosya yeniden üretilir; manuel müdahale gerekmez.

set -eu

PGPASS_FILE="/tmp/pgpass"

if [ -z "${POSTGRES_USER:-}" ] || [ -z "${POSTGRES_PASSWORD:-}" ]; then
  echo "[pgadmin-entrypoint] HATA: POSTGRES_USER veya POSTGRES_PASSWORD env değişkeni boş." >&2
  exit 1
fi

# Wildcard host/db: tüm CleanTenant veritabanları için tek satır yeterli.
printf 'postgres:5432:*:%s:%s\n' "$POSTGRES_USER" "$POSTGRES_PASSWORD" > "$PGPASS_FILE"
chmod 0600 "$PGPASS_FILE"

# Standart pgAdmin entrypoint'ine devret.
exec /entrypoint.sh "$@"
