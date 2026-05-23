<#
.SYNOPSIS
    Geliştirme veritabanını sıfırlar — yalnız belirtilen System admin kullanıcısı
    ve referans (seed) verisi korunur.

.DESCRIPTION
    İş verisini temizler (test sonrası temiz başlangıç için):
      - Main DB   : TÜM tablolar TRUNCATE (companies, building schema, accounting,
                    budgets, accruals — hepsi iş verisi).
      - Catalog DB: tenant'lar + korunan admin DIŞINDAKİ tüm kullanıcılar + rol
                    atamaları + oturum/token kayıtları silinir.
                    KORUNUR: permissions, roller, role_permissions, lookup'lar
                    (il/ilçe/mahalle/bina/konut tipi/banka), localized_resources,
                    chart_of_accounts_templates, budget_type_metadata,
                    inflation_indexes ve korunan admin.
      - Audit DB  : tüm audit kayıtları TRUNCATE.
      - Log DB    : tüm log kayıtları TRUNCATE (best-effort).

    Migration history (__EFMigrationsHistory) korunur — şema migrate edilmiş kalır.

.PARAMETER AdminEmail
    Korunacak System admin kullanıcısının e-postası. Varsayılan: bootstrap admin.

.PARAMETER Container
    PostgreSQL docker container adı. Varsayılan: development container.

.PARAMETER Force
    Onay sormadan çalıştırır.

.EXAMPLE
    ./scripts/db-reset-keep-admin.ps1
    ./scripts/db-reset-keep-admin.ps1 -AdminEmail admin@firma.com -Force
#>
[CmdletBinding()]
param(
    [string]$AdminEmail = 'yusuf.gulmez.ai@gmail.com',
    [string]$Container = 'cleantenant-development-postgres',
    [string]$DbUser = 'cleantenant',
    [string]$CatalogDb = 'cleantenant_catalog',
    [string]$MainDb = 'cleantenant_main',
    [string]$AuditDb = 'cleantenant_audit',
    [string]$LogDb = 'cleantenant_log',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# E-posta içinde tek tırnak SQL injection koruması (basit kaçış)
$emailEscaped = $AdminEmail.Replace("'", "''")

function Invoke-Psql {
    param([string]$Database, [string]$Sql, [switch]$BestEffort)
    $Sql | docker exec -i $Container psql -U $DbUser -d $Database -v ON_ERROR_STOP=1 -q 2>&1 | Out-String | Write-Host
    if ($LASTEXITCODE -ne 0 -and -not $BestEffort) {
        throw "psql hatası (db=$Database, exit=$LASTEXITCODE)."
    }
}

# Container ayakta mı?
$running = docker ps --filter "name=$Container" --format "{{.Names}}" 2>&1
if ($running -notmatch [regex]::Escape($Container)) {
    Write-Error "Container bulunamadı/çalışmıyor: $Container"
    exit 1
}

# Korunacak admin gerçekten var mı?
$adminCheck = (docker exec $Container psql -U $DbUser -d $CatalogDb -tA -c "SELECT count(*) FROM ""AspNetUsers"" WHERE email = '$emailEscaped';" 2>&1).Trim()
if ($adminCheck -ne '1') {
    Write-Error "Korunacak admin bulunamadı veya birden fazla: email=$AdminEmail (count=$adminCheck). İşlem iptal."
    exit 1
}

Write-Host ""
Write-Host "VERİTABANI SIFIRLAMA" -ForegroundColor Yellow
Write-Host "  Container       : $Container"
Write-Host "  Korunacak admin : $AdminEmail"
Write-Host "  Main DB         : TÜM iş verisi silinecek (TRUNCATE)"
Write-Host "  Catalog DB      : tenant'lar + diğer kullanıcılar silinecek; referans + admin korunacak"
Write-Host "  Audit/Log DB    : temizlenecek"
Write-Host ""

if (-not $Force) {
    $confirm = Read-Host "Devam edilsin mi? (yes/no)"
    if ($confirm -ne 'yes') { Write-Host "İptal edildi." -ForegroundColor Cyan; exit 0 }
}

# ── 1. Catalog DB — seçici temizlik ──────────────────────────────────────────
$catalogSql = @"
BEGIN;
DELETE FROM user_role_assignments WHERE user_id NOT IN (SELECT id FROM "AspNetUsers" WHERE email = '$emailEscaped');
DELETE FROM refresh_tokens;
DELETE FROM support_sessions;
DELETE FROM tenant_connections;
DELETE FROM tenants;
DELETE FROM "AspNetUserRoles"   WHERE user_id NOT IN (SELECT id FROM "AspNetUsers" WHERE email = '$emailEscaped');
DELETE FROM "AspNetUserClaims"  WHERE user_id NOT IN (SELECT id FROM "AspNetUsers" WHERE email = '$emailEscaped');
DELETE FROM "AspNetUserLogins"  WHERE user_id NOT IN (SELECT id FROM "AspNetUsers" WHERE email = '$emailEscaped');
DELETE FROM "AspNetUserTokens"  WHERE user_id NOT IN (SELECT id FROM "AspNetUsers" WHERE email = '$emailEscaped');
DELETE FROM "AspNetUsers"       WHERE email <> '$emailEscaped';
COMMIT;
"@

# ── 2. Main / Audit / Log — tüm tabloları TRUNCATE (migration history hariç) ──
$truncateAllSql = @"
DO `$`$
DECLARE r RECORD;
BEGIN
  FOR r IN
    SELECT tablename FROM pg_tables
    WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory'
  LOOP
    EXECUTE 'TRUNCATE TABLE public.' || quote_ident(r.tablename) || ' RESTART IDENTITY CASCADE';
  END LOOP;
END `$`$;
"@

Write-Host "[1/4] Catalog DB temizleniyor (admin + referans korunuyor)..." -ForegroundColor Cyan
Invoke-Psql -Database $CatalogDb -Sql $catalogSql

Write-Host "[2/4] Main DB tüm iş verisi siliniyor..." -ForegroundColor Cyan
Invoke-Psql -Database $MainDb -Sql $truncateAllSql

Write-Host "[3/4] Audit DB temizleniyor..." -ForegroundColor Cyan
Invoke-Psql -Database $AuditDb -Sql $truncateAllSql -BestEffort

Write-Host "[4/4] Log DB temizleniyor..." -ForegroundColor Cyan
Invoke-Psql -Database $LogDb -Sql $truncateAllSql -BestEffort

Write-Host ""
Write-Host "Tamam. Veritabanı sıfırlandı; '$AdminEmail' ve referans veri korundu." -ForegroundColor Green
Write-Host "Not: Eksik referans (permission/rol vb.) varsa app startup seeder'ı tamamlar." -ForegroundColor DarkGray
