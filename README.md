# CleanTenant

Çoklu kiracılı (multi-tenant) site / bina yönetimi SaaS platformu.

| Alan | Değer |
|---|---|
| Platform | .NET 10 (stable) |
| Mimari | Clean Architecture + CQRS (MediatR) |
| Veri Tabanı | PostgreSQL × 4 (Catalog / Main / Log / Audit) |
| Cache | Redis |
| Multi-Tenancy | Hibrit (Shared + Dedicated DB) |
| Yazma | EF Core |
| Okuma | Dapper |

## Uygulamalar

- **CleanTenant.ManagementApp** — Blazor Server + MudBlazor. Sistem / Tenant / Company / Yapı yöneticileri için yönetim paneli.
- **CleanTenant.PortalApp** — Blazor Server + MudBlazor. Malik (Hissedar) / Sakin / Kiracı portal'ı.
- **CleanTenant.MobilApp** — MAUI Blazor Hybrid (Android & iOS). Çift personalı (Management / Portal) mobil uygulama.
- **CleanTenant.WebApi** — Mobil ve dış istemciler için RESTful API.

## Solution Yapısı

```
src/
├── Core/
│   ├── CleanTenant.SharedKernel       Result, BaseEntity, UrlCode, ITenantContext, IClock
│   ├── CleanTenant.Domain             Aggregate, entity, value object, domain event
│   └── CleanTenant.Application        Command, Query, Handler, Validator, Pipeline Behavior
├── Infrastructure/
│   ├── CleanTenant.Infrastructure.Persistence       EF Core DbContext + Dapper okuma
│   ├── CleanTenant.Infrastructure.Identity          ASP.NET Core Identity + JWT + 2FA
│   ├── CleanTenant.Infrastructure.Logging           Serilog → Log DB
│   ├── CleanTenant.Infrastructure.Caching           Redis distributed cache
│   └── CleanTenant.Infrastructure.BackgroundJobs    Hangfire (Postgres backed)
└── Presentation/
    ├── CleanTenant.WebApi             REST API host
    ├── CleanTenant.ManagementApp      Blazor yönetim
    ├── CleanTenant.PortalApp          Blazor portal
    └── CleanTenant.MobilApp           MAUI Hibrit

tests/
├── CleanTenant.Domain.UnitTests
├── CleanTenant.Application.UnitTests
├── CleanTenant.Infrastructure.IntegrationTests   (Testcontainers + gerçek PostgreSQL)
└── CleanTenant.WebApi.IntegrationTests           (WebApplicationFactory)
```

## Hızlı Başlangıç

### Gereksinimler
- .NET 10 SDK (stable, 10.0.203 veya üzeri patch)
- Docker Desktop
- PowerShell 7+
- Git
- MAUI workload: `dotnet workload install maui`

### Kurulum

```powershell
# 1) SDK doğrulaması (10.0.203 görünmeli)
dotnet --version

# 2) Restore + build
dotnet restore
dotnet build

# 3) Test
dotnet test
```

> Faz v0.1.2 itibarıyla Docker compose ile DB stack'i ayağa kalkacaktır:
> `scripts/env-up.ps1 -Env Development`

## Ortamlar

| Ortam | Amaç | Veri | Erişim |
|---|---|---|---|
| Development | Yerel geliştirme | Zengin sahte seed | Geliştiriciler |
| Test | CI + otomatik test + manuel QA | Testcontainers + minimum fixture | CI + QA |
| Demo | Satış demoları | Düzenli vitrin verisi | Demo kullanıcıları |
| Production | Canlı müşteriler | Gerçek müşteri verisi | Operasyon |

Her ortam için ayrı `appsettings.<Env>.json`, `.env.<env>` (gitignored) ve `docker-compose.<env>.yml`.

## Geliştirme Kuralları

Proje kuralları `docs/phases/v0.1/README.md` dosyasında ve `docs/memory-snapshots/` altındaki versiyonlu snapshot'larda detaylıdır. Özet:

- Her sınıf / interface / enum / record `///` XML doc taşır.
- Bir dosyada tek tip (one type per file).
- Manual mapping (AutoMapper yok).
- Dokümantasyon ve yorumlar **Türkçe**; kod tanımlayıcıları İngilizce.
- `DateTime.Now` yasak; sadece `DateTime.UtcNow` / `IClock`.
- Türkçe karakter case-insensitive arama: `unaccent + lower`.
- Faz kapanışı kapısı: tüm testler yeşil + güvenlik kontrolü temiz.

## Versiyonlama

Faz başına `docs/phases/vX.Y/` altında versiyonlu doküman seti tutulur. Memory kural seti güncellendikçe `docs/memory-snapshots/vNNN_YYYY-MM-DD_HHmm/` altında snapshot alınır.

## Lisans

Özel / Proprietary. © 2026 CleanTenant.
