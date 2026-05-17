<#
.SYNOPSIS
    Belirtilen ortam için EF Core migration'larını uygular.

.DESCRIPTION
    .env.<env> dosyasından connection string'leri okur, dotnet ef ile
    migration'ları DB'ye uygular. Şu an yalnız Catalog DbContext aktif;
    Main, Log, Audit DbContext'leri ilerleyen alt fazlarda eklendikçe
    bu script güncellenir.

.PARAMETER Env
    Hedef ortam: Development | Test | Demo | Production.

.EXAMPLE
    ./scripts/env-migrate.ps1 -Env Development

.NOTES
    Faz: v0.1.4.a (Catalog migration aktif).
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
$persistenceProject = Join-Path $rootPath 'src/Infrastructure/CleanTenant.Infrastructure.Persistence/CleanTenant.Infrastructure.Persistence.csproj'

if (-not (Test-Path $envFile)) {
    Write-Error ".env.$envLower bulunamadı. Önce '.env.$envLower.example' kopyalayıp düzenleyin."
    exit 1
}

# dotnet-ef kurulu mu?
$efVersion = dotnet ef --version 2>&1 | Select-String -Pattern '^\d+\.\d+\.\d+'
if (-not $efVersion) {
    Write-Error "dotnet-ef bulunamadı. Kurulum: dotnet tool install --global dotnet-ef --version 10.0.7"
    exit 1
}

# .env dosyasından connection string'leri yükle
Get-Content $envFile | ForEach-Object {
    if ($_ -match '^\s*([^#=]+?)\s*=\s*(.+?)\s*$') {
        $name = $matches[1].Trim()
        $value = $matches[2].Trim()
        [Environment]::SetEnvironmentVariable($name, $value, 'Process')
    }
}

Write-Host ""
Write-Host "CleanTenant '$Env' ortamı için migration uygulanıyor..." -ForegroundColor Cyan
Write-Host "  Persistence projesi : $persistenceProject"
Write-Host "  Catalog connection  : $env:ConnectionStrings__Catalog"
Write-Host ""

# Catalog DB
Write-Host "[1/1] Catalog DB migration uygulanıyor..." -ForegroundColor Cyan
dotnet ef database update `
    --project $persistenceProject `
    --context CatalogDbContext `
    --no-build

if ($LASTEXITCODE -ne 0) {
    Write-Error "Catalog migration başarısız (exit $LASTEXITCODE)."
    exit $LASTEXITCODE
}

# İlerideki DbContext'ler (Main, Log, Audit) buraya eklenecek:
# Write-Host "[2/4] Main DB migration uygulanıyor..." -ForegroundColor Cyan
# dotnet ef database update --project $persistenceProject --context MainDbContext --no-build
# ...

Write-Host ""
Write-Host "Tamam. Migration'lar uygulandı." -ForegroundColor Green
