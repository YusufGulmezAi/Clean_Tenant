<#
.SYNOPSIS
    Belirtilen ortam için CleanTenant Docker stack'ini ayağa kaldırır.

.DESCRIPTION
    Compose base + ortam override + uygun .env dosyasıyla Docker servislerini
    başlatır. Health-check'lerin yeşil olmasını bekler ve servis durumunu listeler.

.PARAMETER Env
    Hedef ortam. Geçerli değerler: Development, Test, Demo, Production.

.EXAMPLE
    ./scripts/env-up.ps1 -Env Development

.NOTES
    Faz: v0.1.2 (Docker Compose + 4 Ortam Yapısı)
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = "Hedef ortam adı.")]
    [ValidateSet('Development', 'Test', 'Demo', 'Production')]
    [string]$Env
)

$ErrorActionPreference = 'Stop'

$envLower = $Env.ToLower()
$rootPath = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $rootPath ".env.$envLower"
$composeBase = Join-Path $rootPath 'compose/docker-compose.yml'
$composeOverride = Join-Path $rootPath "compose/docker-compose.$envLower.yml"

# 1) Docker var mı ve çalışıyor mu?
try {
    $null = docker info --format '{{.ServerVersion}}' 2>$null
    if ($LASTEXITCODE -ne 0) { throw }
} catch {
    Write-Error "Docker bulunamadı veya engine çalışmıyor. Docker Desktop'ı başlatın."
    exit 1
}

# 2) .env ve override dosyaları var mı?
if (-not (Test-Path $envFile)) {
    Write-Error ".env.$envLower bulunamadı. Önce '.env.$envLower.example' dosyasını kopyalayıp düzenleyin."
    exit 1
}
if (-not (Test-Path $composeBase)) {
    Write-Error "Compose base dosyası bulunamadı: $composeBase"
    exit 1
}
if (-not (Test-Path $composeOverride)) {
    Write-Error "Compose override dosyası bulunamadı: $composeOverride"
    exit 1
}

Write-Host ""
Write-Host "CleanTenant '$Env' ortamı başlatılıyor..." -ForegroundColor Cyan
Write-Host "  Env dosyası   : $envFile"
Write-Host "  Compose base  : $composeBase"
Write-Host "  Override      : $composeOverride"
Write-Host ""

# 3) Servisleri başlat
docker compose --env-file $envFile -f $composeBase -f $composeOverride up -d
if ($LASTEXITCODE -ne 0) {
    Write-Error "docker compose up başarısız (exit $LASTEXITCODE)."
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Servis durumu:" -ForegroundColor Green
docker compose --env-file $envFile -f $composeBase -f $composeOverride ps

Write-Host ""
Write-Host "Tamam. '$Env' ortamı ayakta. İndirme için 'env-down.ps1 -Env $Env'." -ForegroundColor Green
