<#
.SYNOPSIS
    Belirtilen ortam için Catalog seed verisini yükler.

.DESCRIPTION
    .env.<env> dosyasından konfigürasyonu okur ve MigrationRunner'ın
    `seed` alt komutunu çağırır. Ortam başına davranış:
      - Development : Permission + built-in roller + Yusuf Developer admin + demo tenant
      - Test        : Yalnız Permission + built-in roller
      - Demo        : Permission + built-in roller + admin + demo tenant (Faz 1'de zenginleşecek)
      - Production  : Yalnız Permission + built-in roller (kullanıcı için 'init-system-admin')

.PARAMETER Env
    Hedef ortam.

.EXAMPLE
    ./scripts/env-seed.ps1 -Env Development

.NOTES
    Faz: v0.1.4.b
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Development', 'Test', 'Demo', 'Production')]
    [string]$Env
)

$ErrorActionPreference = 'Stop'

$envLower = $Env.ToLower()
$rootPath = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $rootPath ".env.$envLower"
$runnerProject = Join-Path $rootPath 'tools/CleanTenant.MigrationRunner/CleanTenant.MigrationRunner.csproj'

if (-not (Test-Path $envFile)) {
    Write-Error ".env.$envLower bulunamadı. Önce '.env.$envLower.example' kopyalayıp düzenleyin."
    exit 1
}

# .env dosyasından env değişkenlerini yükle
Get-Content $envFile | ForEach-Object {
    if ($_ -match '^\s*([^#=]+?)\s*=\s*(.+?)\s*$') {
        $name = $matches[1].Trim()
        $value = $matches[2].Trim()
        [Environment]::SetEnvironmentVariable($name, $value, 'Process')
    }
}

Write-Host ""
Write-Host "CleanTenant '$Env' ortamı için seed çalıştırılıyor..." -ForegroundColor Cyan

dotnet run --project $runnerProject -- seed --env $Env

if ($LASTEXITCODE -ne 0) {
    Write-Error "Seed başarısız (exit $LASTEXITCODE)."
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Tamam. Seed tamamlandı." -ForegroundColor Green
