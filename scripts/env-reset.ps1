<#
.SYNOPSIS
    Belirtilen ortamı sıfırlar — container'lar durdurulur ve TÜM volume'lar silinir.

.DESCRIPTION
    YIKICI işlem. PostgreSQL veri klasörü, Redis dump dosyaları ve Seq verileri
    silinir. Bir sonraki 'env-up' çağrısında init script'leri yeniden çalışır
    ve veritabanları boş başlar.

.PARAMETER Env
    Hedef ortam.

.PARAMETER Force
    Onay sormadan sıfırlar. Otomasyon (CI) için.

.EXAMPLE
    ./scripts/env-reset.ps1 -Env Development
    # Onay istenir; ortam adı yazılarak doğrulanır.

.EXAMPLE
    ./scripts/env-reset.ps1 -Env Test -Force
    # Otomatik. Test ortamında yaygın kullanım.

.NOTES
    Faz: v0.1.2
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Development', 'Test', 'Demo', 'Production')]
    [string]$Env,

    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$envLower = $Env.ToLower()
$rootPath = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $rootPath ".env.$envLower"
$composeBase = Join-Path $rootPath 'compose/docker-compose.yml'
$composeOverride = Join-Path $rootPath "compose/docker-compose.$envLower.yml"

if (-not $Force) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Red
    Write-Host "  UYARI: '$Env' ortamındaki TÜM VERİLER silinecek." -ForegroundColor Red
    Write-Host "  Postgres + Redis + Seq volume'ları kaldırılacak." -ForegroundColor Red
    Write-Host "  Bu işlemin geri dönüşü yoktur." -ForegroundColor Red
    Write-Host "============================================================" -ForegroundColor Red
    Write-Host ""

    if ($Env -eq 'Production') {
        Write-Host "PRODUCTION ortamını sıfırlıyorsunuz. Bir kez daha düşünün." -ForegroundColor Red
        Write-Host ""
    }

    $confirm = Read-Host "Devam etmek için ortam adını yazın ('$Env')"
    if ($confirm -ne $Env) {
        Write-Host "İptal edildi." -ForegroundColor Yellow
        exit 0
    }
}

Write-Host ""
Write-Host "'$Env' ortamı sıfırlanıyor (container + volume)..." -ForegroundColor Red

docker compose --env-file $envFile -f $composeBase -f $composeOverride down --volumes --remove-orphans
if ($LASTEXITCODE -ne 0) {
    Write-Error "docker compose down başarısız (exit $LASTEXITCODE)."
    exit $LASTEXITCODE
}

Write-Host "Tamam. '$Env' ortamı temiz. 'env-up.ps1 -Env $Env' ile yeniden başlatabilirsiniz." -ForegroundColor Green
