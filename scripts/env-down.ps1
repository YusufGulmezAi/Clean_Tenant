<#
.SYNOPSIS
    Belirtilen ortamın CleanTenant Docker stack'ini durdurur.

.DESCRIPTION
    Container'ları durdurup kaldırır; volume'lara dokunmaz (veri korunur).
    Tekrar 'env-up.ps1' çalıştırıldığında veriler yerinde olur.

.PARAMETER Env
    Hedef ortam.

.EXAMPLE
    ./scripts/env-down.ps1 -Env Development

.NOTES
    Faz: v0.1.2
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
$composeBase = Join-Path $rootPath 'compose/docker-compose.yml'
$composeOverride = Join-Path $rootPath "compose/docker-compose.$envLower.yml"

if (-not (Test-Path $envFile)) {
    Write-Warning ".env.$envLower bulunamadı; yine de mevcut container'lar durdurulmaya çalışılacak."
}

Write-Host ""
Write-Host "CleanTenant '$Env' ortamı durduruluyor (volume'lar korunur)..." -ForegroundColor Yellow

docker compose --env-file $envFile -f $composeBase -f $composeOverride down
if ($LASTEXITCODE -ne 0) {
    Write-Error "docker compose down başarısız (exit $LASTEXITCODE)."
    exit $LASTEXITCODE
}

Write-Host "Tamam. Veriler korundu. Tekrar başlatmak için 'env-up.ps1 -Env $Env'." -ForegroundColor Green
