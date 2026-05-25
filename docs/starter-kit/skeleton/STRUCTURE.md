# D) Proje İskeleti — Çözüm Ağacı & Temel Dosyalar

Boş bir .NET 10 / Blazor / PostgreSQL / Clean Architecture / çok-kiracılı SaaS
çözümünün hedef yapısı. `Acme.Saas` → kendi adınla değiştir.

## Çözüm ağacı

```
Acme.Saas/
├── Acme.Saas.slnx                      # yeni solution formatı
├── global.json                         # SDK pin
├── Directory.Build.props               # ortak derleme ayarları (Nullable, ImplicitUsings, analizörler)
├── Directory.Packages.props            # Central Package Management (tek yerde sürümler)
├── .editorconfig                       # stil sözleşmesi (zorunlu)
├── .gitignore / .gitattributes
├── README.md
│
├── compose/
│   ├── docker-compose.yml              # base: postgres(17) + redis + minio
│   ├── docker-compose.development.yml
│   ├── docker-compose.test.yml
│   ├── docker-compose.demo.yml
│   ├── docker-compose.production.yml
│   └── postgres-init/                  # 4 DB + extension (citext, pg_trgm, unaccent, pgcrypto)
│
├── scripts/                            # .ps1 + .sh ikizleri (Windows + Linux)
│   ├── env-up.ps1 / env-down.ps1       # compose aç/kapat
│   ├── env-migrate.ps1                 # 4 DB'ye migration uygula
│   ├── env-seed.ps1                    # seed data
│   ├── env-run.ps1                     # app başlat
│   ├── env-reset.ps1                   # sıfırla (dikkatli)
│   └── kill-app.ps1                    # port/DLL kilidi temizle
│
├── src/
│   ├── Core/
│   │   ├── Acme.Saas.SharedKernel/     # BaseEntity, Result/Error, IDomainEvent,
│   │   │                               #   markerlar, IClock, UrlCode üretici, Context arayüzleri
│   │   ├── Acme.Saas.Domain/           # aggregate'ler, value object'ler, Events/
│   │   └── Acme.Saas.Application/       # Features/<Area>/<UseCase>, Common/Pipeline,
│   │                                   #   Common/Persistence (DbContext arayüzleri)
│   ├── Infrastructure/
│   │   ├── Acme.Saas.Persistence/      # 4 DbContext, Configurations/, Interceptors/,
│   │   │                               #   Migrations/, Outbox/, Seeding/
│   │   ├── Acme.Saas.Identity/         # ASP.NET Identity, 2FA, lockout
│   │   ├── Acme.Saas.Caching/          # Redis/HybridCache, session store
│   │   ├── Acme.Saas.Logging/          # Serilog → Log DB, OpenTelemetry
│   │   ├── Acme.Saas.Storage/          # MinIO/S3, görsel işleme
│   │   ├── Acme.Saas.BackgroundJobs/   # Hangfire (ayrı jobs DB + sentetik tenant oturumu)
│   │   └── Acme.Saas.Export/           # PDF/Excel
│   └── Presentation/
│       ├── Acme.Saas.WebApi/           # REST + ProblemDetails + health + rate limit
│       ├── Acme.Saas.ManagementApp/    # Blazor (MudBlazor) yönetim arayüzü
│       └── Acme.Saas.PortalApp/        # (opsiyonel) son kullanıcı portalı
│
├── tests/
│   ├── Acme.Saas.Domain.UnitTests/         # en ucuz kat — invariant'lar
│   ├── Acme.Saas.Application.UnitTests/     # handler + validator
│   ├── Acme.Saas.Architecture.Tests/       # ⭐ NetArchTest — katman ihlali = 0
│   ├── Acme.Saas.Persistence.IntegrationTests/  # Testcontainers (gerçek PG)
│   ├── Acme.Saas.ManagementApp.bUnitTests/ # Blazor component
│   └── Acme.Saas.WebApi.IntegrationTests/  # API uçtan uca
│
├── tools/
│   └── Acme.Saas.MigrationRunner/      # CLI: 4 DB migration koşucusu
│
├── docs/
│   ├── adr/                            # Mimari Karar Kayıtları
│   ├── phases/                         # faz-sonu mimari haritalar
│   └── wiki/                           # mimari, onboarding, güvenlik
│
└── .github/workflows/
    └── ci.yml                          # ⭐ build + test + SAST + SCA + secret-scan
```

## Bağımlılık yönü (Clean Arch — kesin kural)

```
Presentation ──► Application ──► Domain ──► SharedKernel
       │              │
       └──────────────┴────────► Infrastructure ──► (Application arayüzlerini implemente eder)
Domain / Application  ──X──►  Infrastructure   (YASAK — NetArchTest denetler)
```

## CleanTenant'a göre yeni eklenenler (1. günden)

- ⭐ `Acme.Saas.Architecture.Tests` — katman ihlallerini otomatik yakalar.
- ⭐ `Persistence/Outbox/` — domain event'lerin güvenilir dağıtımı (CleanTenant'ta
  event'ler üretiliyor ama dağıtılmıyordu; burada 1. günden bağlı).
- ⭐ `.github/workflows/ci.yml` — her PR'da build+test+güvenlik taraması.
- OpenTelemetry, health check, rate limiting, idempotency behavior.

## Temel config dosyalarının iskeletleri

> Bu örnekler `skeleton/files/` altında ayrı dosyalar olarak da bulunur.

### `global.json`
```json
{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }
```

### `Directory.Build.props` (özet)
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

### `Directory.Packages.props` (Central Package Management — özet)
```xml
<Project>
  <PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="MediatR" Version="..." />
    <PackageVersion Include="FluentValidation" Version="..." />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="..." />
    <PackageVersion Include="Dapper" Version="..." />
    <PackageVersion Include="Serilog.AspNetCore" Version="..." />
    <PackageVersion Include="NetArchTest.Rules" Version="..." />
    <PackageVersion Include="Testcontainers.PostgreSql" Version="..." />
    <!-- ... -->
  </ItemGroup>
</Project>
```

### `scripts/env-migrate.ps1` (özet — 4 DB)
```powershell
param([string]$Env = "Development")
$env:ASPNETCORE_ENVIRONMENT = $Env
foreach ($ctx in "CatalogDbContext","MainDbContext","LogDbContext","AuditDbContext") {
    dotnet ef database update `
        --project src/Infrastructure/Acme.Saas.Persistence `
        --context $ctx
}
```

### `.github/workflows/ci.yml` (özet)
```yaml
name: ci
on: [push, pull_request]
jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - run: dotnet build --configuration Release
      - run: dotnet test  --configuration Release   # Testcontainers Docker ister
      - run: dotnet list package --vulnerable --include-transitive   # SCA
      # - gitleaks (secret tarama) + (opsiyonel) CodeQL/SAST adımları
```
