<#
.SYNOPSIS
    Belirtilen ortam için .env.<env> değişkenlerini yükleyip bir .NET projesini çalıştırır.

.DESCRIPTION
    `dotnet run` doğrudan PowerShell'den çağrıldığında .env dosyasını otomatik
    yüklemez. Bu helper script, env-migrate.ps1 / env-seed.ps1'in yaptığı gibi
    .env.<env> dosyasını okuyup current process'e env-var olarak set eder ve
    ardından `dotnet run` çağırır.

.PARAMETER Env
    Hedef ortam: Development | Test | Demo | Production.

.PARAMETER Project
    Çalıştırılacak proje yolu. Varsayılan: ManagementApp.

.PARAMETER LaunchProfile
    `dotnet run --launch-profile` değeri. Varsayılan: https.

.EXAMPLE
    ./scripts/env-run.ps1 -Env Development
    ./scripts/env-run.ps1 -Env Development -Project src/Presentation/CleanTenant.WebApi
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Development', 'Test', 'Demo', 'Production')]
    [string]$Env,

    [string]$Project = 'src/Presentation/CleanTenant.ManagementApp',

    [string]$LaunchProfile = 'https'
)

$ErrorActionPreference = 'Stop'

$envLower = $Env.ToLower()
$rootPath = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $rootPath ".env.$envLower"

if (-not (Test-Path $envFile)) {
    Write-Error ".env.$envLower bulunamadı. Önce '.env.$envLower.example' kopyalayıp düzenleyin."
    exit 1
}

# .env dosyasından env değişkenlerini current process'e yükle
Get-Content $envFile | ForEach-Object {
    if ($_ -match '^\s*([^#=]+?)\s*=\s*(.+?)\s*$') {
        [Environment]::SetEnvironmentVariable($matches[1].Trim(), $matches[2].Trim(), 'Process')
    }
}

Write-Host ""
Write-Host "CleanTenant '$Env' ortamı için '$Project' çalıştırılıyor..." -ForegroundColor Cyan
Write-Host "  Launch profile : $LaunchProfile"
Write-Host ""

dotnet run --project $Project --launch-profile $LaunchProfile
