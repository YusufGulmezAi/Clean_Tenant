#!/bin/sh
# CleanTenant — pgAdmin custom entrypoint
#
# Görev: POSTGRES_USER değerini kullanarak /pgadmin4/servers.json üretir.
# 4 CleanTenant veritabanı (Catalog/Main/Log/Audit) için server tanımları
# önceden eklenmiş olur; kullanıcı pgAdmin UI'sini açtığında 4 server hazır
# bulur.
#
# ŞİFRE STRATEJİSİ:
#   pgAdmin 4 son sürümlerinde servers.json import'unda "Password" alanı
#   security policy gereği yok sayılıyor. Bu yüzden burada YAZMIYORUZ.
#   Kullanıcı her DB'ye İLK kez bağlanırken UI'de şifresini bir kez girer
#   ve "Save Password" kutusunu işaretler. pgAdmin şifreyi kendi encrypted
#   DB'sine yazar; pgadmin-data volume persist olduğu için sonraki açılışlar
#   tamamen otomatik olur.

set -eu

if [ -z "${POSTGRES_USER:-}" ]; then
  echo "[pgadmin-entrypoint] HATA: POSTGRES_USER env değişkeni boş." >&2
  exit 1
fi

python3 - <<'PYEOF' > /pgadmin4/servers.json
import json, os

user = os.environ["POSTGRES_USER"]

def srv(idx, name, db):
    return str(idx), {
        "Name": f"CleanTenant — {name}",
        "Group": "CleanTenant",
        "Host": "postgres",
        "Port": 5432,
        "MaintenanceDB": db,
        "Username": user,
        "SSLMode": "prefer",
    }

print(json.dumps({
    "Servers": dict([
        srv(1, "Catalog", "cleantenant_catalog"),
        srv(2, "Main",    "cleantenant_main"),
        srv(3, "Log",     "cleantenant_log"),
        srv(4, "Audit",   "cleantenant_audit"),
    ])
}, indent=2, ensure_ascii=False))
PYEOF

chown 5050:0 /pgadmin4/servers.json
chmod 0640 /pgadmin4/servers.json

# Standart pgAdmin entrypoint'ine devret (servers.json import ve web sunucusu).
exec /entrypoint.sh "$@"
