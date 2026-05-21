# Faz 0 — Temel Altyapı (v0.1)

| Alan | Değer |
|---|---|
| **Faz Numarası** | v0.1 |
| **Durum** | Planlama tamamlandı, uygulama başlatılacak |
| **Başlangıç Tarihi** | 2026-05-17 |
| **Tahmini Süre** | 5–7 oturum |
| **Paralelleştirme** | Yok (foundation, sıralı) |
| **Onaylayan** | Yusuf |

---

## 1. Faz Misyonu

Bu fazın sonunda **hiçbir iş özelliği (feature) yok** ama tüm projenin üzerine inşa edileceği temel iskelet hazır olacak:

- Solution yapısı ve Clean Architecture katmanları kurulu.
- 4 mantıksal PostgreSQL veri tabanı (**Catalog / Main / Log / Audit**) + Redis, Docker üzerinde çalışıyor.
- Identity skeleton (kullanıcı kaydı, JWT üretimi, refresh token rotation, 2FA TOTP iskeleti, context-switch endpoint'i) ayakta.
- Global response envelope (`ApiResponse<T>`) ve 7 adet MediatR pipeline behavior kayıtlı.
- Audit interceptor + Serilog → Log DB akışı çalışıyor.
- Çoklu dil altyapısı (TR/EN seed ile) hazır.
- Hangfire (background jobs) altyapısı kayıtlı.
- Test projeleri (Testcontainers + xUnit + bUnit) yeşil.
- CI workflow (build + test + lint + security scan) çalışıyor.
- 4 ortam (Development / Test / Demo / Production) için config ve sır yönetimi yapısı kurulu.
- Faz 0 doküman seti (`docs/phases/v0.1/`) tam.

**Faz 1'in başlangıç noktası:** "Hazır lego'ları kullanarak ilk iş özelliklerini yazmak."

---

## 2. Çıkış Kriterleri (Definition of Done)

Faz 0'ın "tamamlandı" sayılması için **hepsinin yeşil** olması gerekir:

- [ ] Tüm alt fazlar tamamlandı.
- [ ] Planlanan tüm testler geçti.
- [ ] Test kapsama oranı hedefleri sağlandı (Domain+Application ≥ %85, toplam ≥ %70).
- [ ] Güvenlik kapısı geçildi — `security-report.md` temiz.
- [ ] CI pipeline yeşil, gerekli kontroller `required` olarak işaretli.
- [ ] Tüm faz dokümanları (`README.md`, `design.md`, `test-plan.md`, `test-report.md`, `security-report.md`, `CHANGELOG.md`) güncel ve Türkçe.
- [ ] Memory snapshot güncel (`docs/memory-snapshots/`).
- [ ] Conventional Commits ile commit'lendi ve `develop` branch'ine push edildi.

---

## 3. Sistem Gereksinimleri (Geliştirici Makinesi)

| Bileşen | Versiyon | Kontrol |
|---|---|---|
| .NET SDK | 10.0 (stable) | `dotnet --version` |
| Docker Desktop | Güncel | `docker --version` |
| PowerShell | 7+ | `$PSVersionTable.PSVersion` |
| Git | 2.40+ | `git --version` |
| MAUI Workload | net10.0 uyumlu | `dotnet workload list` |
| Node.js | (sadece Blazor tooling için gerekirse) | `node --version` |

Eksik bileşenler `scripts/setup-env.ps1 -Env Development` ile listelenir ve kurulum yönergesi gösterilir.

---

## 4. Alt Faz Listesi ve Bağımlılık Sırası

| Sıra | Kod | Başlık | Bağımlı Olduğu |
|---|---|---|---|
| 1 | v0.1.1 | Solution & Proje İskeleti | — |
| 2 | v0.1.2 | Docker Compose + 4 Ortam | v0.1.1 |
| 3 | v0.1.3 | SharedKernel (BaseEntity, Result, UrlCode) | v0.1.1 |
| 4 | v0.1.4 | Catalog DB + Tenant Registry + Identity Şeması | v0.1.2, v0.1.3 |
| 5 | v0.1.5 | Identity + JWT + Refresh + 2FA İskelet | v0.1.4 |
| 6 | v0.1.6 | Global Response Envelope + MediatR Pipeline | v0.1.5 |
| 7 | v0.1.7 | Serilog + Log DB + Audit Interceptor | v0.1.6 |
| 8 | v0.1.8 | Localization Altyapısı | v0.1.7 |
| 9 | v0.1.9 | Hangfire (Background Jobs) | v0.1.7 |
| 10 | v0.1.10 | Test Altyapısı | v0.1.5 |
| 11 | v0.1.11 | GitHub Actions CI/CD | v0.1.10 |
| 12 | v0.1.12 | Faz Dokümantasyonu (tamamlama) | sürekli |
| 13 | v0.1.13 | Git Init + İlk Commit & Push | tüm üstündekiler |

### 4.1. v0.1.1 — Solution & Proje İskeleti

**Ne yapılacak:**
- `CleanTenant.sln` ve Clean Architecture katman projeleri.
- `Directory.Build.props` (TargetFramework=net10.0, Nullable=enable, TreatWarningsAsErrors=true, analyzer paketleri).
- `Directory.Packages.props` (Central Package Management — tüm paket sürümleri tek yerden).
- `.editorconfig`, `.gitignore`, `.gitattributes`, kök `README.md` (Türkçe).
- MAUI projesi iskelet halinde dahil (hello-world ötesi yok).

**Neden:** Clean Architecture'ın iç → dış bağımlılık akışı fiziksel olarak ayrılmış katmanlarla zorlanır. Central Package Management NuGet sürüm çakışmalarını engeller. Analyzer'lar ilk satır koddan itibaren kuralları dayatır.

**Neden şimdi:** Hiçbir kod yazılmadan önce iskelet olmalı — sınıflar yanlış katmana koyulursa retrofit acıdır.

### 4.2. v0.1.2 — Docker Compose + 4 Ortam Yapısı

**Ne yapılacak:**
- `compose/docker-compose.yml` (base) + her ortam için override.
- Servisler: `postgres-catalog`, `postgres-main`, `postgres-log`, `postgres-audit`, `redis`, `seq` (sadece dev).
- PostgreSQL init script: `citext`, `unaccent`, `pg_trgm`, `uuid-ossp` extension'larını oluştur.
- `.env.<env>.example` dosyaları.
- `scripts/env-up.ps1`, `env-down.ps1`, `env-reset.ps1`, `env-migrate.ps1`, `env-seed.ps1` (+ `.sh` varyantları).

**Neden:** 4 ortamda aynı yöntemle çalışabilmek. Yerel geliştirmenin host'tan bağımsız ve izole olması. Sırların `.env` dosyalarında, repo dışında olması.

**Neden şimdi:** Migration ve test öncesi DB'lerin ayağa kalkıyor olması zorunlu.

### 4.3. v0.1.3 — SharedKernel

**Ne yapılacak:**
- `Result` / `Result<T>` pattern (Success/Failure + Error listesi).
- `BaseEntity` (Id `Guid`, UrlCode `string(9)`, audit alanları, RowVersion, TenantId?).
- `IUrlCodeGenerator` + `Base58UrlCodeGenerator` (9 karakter, Guid'den üretilir, çarpışmada retry).
- `ITenantContext`, `IClock` + `SystemClock`.
- `TurkishStringNormalizer` (I/İ/ı/i normalizasyonu).

**Neden:** Bu primitif'ler tüm projede yatay olarak kullanılacak. Önce toplanır, sonra her katman referans verir.

**Neden şimdi:** DbContext'ler ve entity'ler bu primitif'lere bağımlı.

### 4.4. v0.1.4 — Catalog DB + Tenant Registry + Identity Şeması

**Ne yapılacak:**
- `CatalogDbContext`.
- Tablolar: `Tenants`, `TenantConnections`, `Users`, `Roles`, `Permissions`, `RolePermissions`, `UserRoleAssignments`, `RefreshTokens`, `UrlCodes`.
- İlk EF Core migration.
- `ITenantConnectionFactory` (TenantId → connection string → açık bağlantı).
- Dev/Demo ortamı için seed: System rol katalogu + ilk admin kullanıcı.

**Neden:** Catalog DB tüm multi-tenancy routing'in ve global identity'nin başlangıç noktası. UrlCode pattern'i ilk burada uygulanır.

**Neden şimdi:** Identity adımı bu şemaya bağımlı.

### 4.5. v0.1.5 — Identity + JWT + Refresh Token + 2FA İskelet

**Ne yapılacak:**
- ASP.NET Core Identity'nin custom store ile Catalog DB'ye bağlanması.
- `IJwtTokenService` (context-bound token üretimi — `tenantId`, `companyId?`, `unitId?`, `roles`, `permissions` claim'leriyle).
- `IRefreshTokenService` (rotating, hashed, revocation list).
- Endpoint'ler: `POST /api/v1/auth/login`, `/refresh`, `/logout`, `/switch-context`, `/2fa/enroll`, `/2fa/verify`.
- 2FA: System kullanıcıları için zorunlu; ManagementApp diğer kullanıcılar opt-in.
- `AuthorizationBehavior` (MediatR pipeline'a kayıtlı) + `IPermissionChecker` + `[RequirePermission("...")]` attribute.

**Neden:** Tüm endpoint'lerin yetkilendirme tabanı bu. Multi-context per tab davranışı baştan doğru kurgulanmalı (cookie değil, JWT + sessionStorage uyumlu).

**Neden şimdi:** Pipeline behavior'lar ve sonraki tüm endpoint'ler Identity'ye bağımlı.

### 4.6. v0.1.6 — Global Response Envelope + MediatR Pipeline

**Ne yapılacak:**
- `ApiResponse<T>`, `ApiError`, `ApiMeta`, `ApiFieldError`, `Pagination` tipleri.
- `ErrorCodeCatalog` — DB seed ile dolar (modül-bazlı kodlar: `AUTH-001`, `VAL-001`, vb.).
- MediatR registrasyonu + 7 pipeline behavior:
  1. `UnhandledExceptionBehavior`
  2. `LoggingBehavior`
  3. `PerformanceBehavior`
  4. `AuthorizationBehavior`
  5. `ValidationBehavior` (FluentValidation 11.x)
  6. `TransactionBehavior` (sadece Command'lar)
  7. `CachingBehavior` (sadece ICacheableQuery)
- Global exception middleware → ApiResponse formatına çevirir.

**Neden:** Tüm endpoint'ler aynı zarfı dönmeli; her command/query otomatik validasyon, yetkilendirme, transaction'dan geçmeli.

**Neden şimdi:** Auth endpoint'leri bile bu envelope'u kullanmalı; sonradan retrofit yerine baştan doğru.

### 4.7. v0.1.7 — Serilog + Log DB + Audit Interceptor

**Ne yapılacak:**
- Serilog config: Console + File + PostgreSQL (Log DB).
- `LogDbContext` + log şeması (Properties jsonb, ay bazında partition).
- `AuditDbContext` + audit şeması.
- EF Core `SaveChangesInterceptor` — ChangeTracker'dan otomatik audit kayıt üretir.
- `CorrelationIdMiddleware`.
- Serilog enricher'ları: UserId, TenantId, CorrelationId, IpAddress, UserAgent.
- PII redaction (şifre, token, kart numarası).

**Neden:** Kritik işlemler ilk gün loglanmalı. Audit interceptor sayesinde handler yazarken audit kaydını unutmak imkânsız.

**Neden şimdi:** Identity akışları logging olmadan kör akar.

### 4.8. v0.1.8 — Localization Altyapısı

**Ne yapılacak:**
- `LocalizationResources` tablosu (Catalog DB içinde `localization` schema).
- `ILocalizationService` (Redis cache'li, fallback zinciri: kültür → TR → key).
- Culture middleware: `?lang=` → `Accept-Language` → kullanıcı tercihi → tenant default → TR.
- Seed: TR + EN için temel hata kodu mesajları, validation mesajları, UI label'ları.
- AR/RU/DE için boş satır iskeleti (sonradan doldurulur).

**Neden:** Hata mesajları ilk endpoint'ten itibaren çoklu dil yetenekli dönmeli. DB-based yapı runtime düzenleme imkânı verir.

**Neden şimdi:** Response envelope'un `Error.Message` alanını dolduran servis hazır olmalı.

### 4.9. v0.1.9 — Hangfire

**Ne yapılacak:**
- Hangfire kurulumu (Postgres backed, Main DB içinde `hangfire` schema).
- Hangfire dashboard endpoint (yetki gated).
- `IJobScheduler` abstraction.
- Örnek scheduled job'lar: Demo DB günlük reset, JWT key rotation reminder.

**Neden:** Scheduled görevler için altyapı. Faz 1+ iş özellikleri job'lara ihtiyaç duyacak.

**Neden şimdi:** Test altyapısı ve CI bu servisi tanımalı; sonradan eklenirse retrofit gerekir.

### 4.10. v0.1.10 — Test Altyapısı

**Ne yapılacak:**
- Paketler: xUnit, FluentAssertions, NSubstitute, Testcontainers.PostgreSql, bUnit, WireMock.Net, Bogus, Coverlet.
- `WebApplicationFactory<Program>` test base.
- `TestcontainersPostgresFixture` (gerçek PostgreSQL spin-up).
- Örnek testler (yeşil olmalı):
  - Domain: `BaseEntity.UrlCode` 9 karakter Base58 üretimi.
  - Application: ValidationBehavior çalışıyor.
  - Integration: Login uçtan uca (Testcontainer DB + JWT + RefreshToken).
  - Türkçe karakter: `unaccent + lower` SQL fonksiyonu I/İ/ı/i için doğru.
- Coverage collection.

**Neden:** Faz kapanış kapısı için çalışan test altyapısı şart. Testcontainers pattern'i baştan benimsenir.

**Neden şimdi:** CI bu testlere ihtiyaç duyacak.

### 4.11. v0.1.11 — GitHub Actions CI/CD

**Ne yapılacak:**
- `.github/workflows/build-test.yml` (build + test + coverage upload).
- `.github/workflows/lint.yml` (analyzer + format check).
- `.github/workflows/security-scan.yml` (gitleaks + vulnerable package + OWASP ZAP baseline).
- `.github/dependabot.yml` (FluentValidation v12+ ignore listesi dahil).
- `.github/PULL_REQUEST_TEMPLATE.md` + `.github/ISSUE_TEMPLATE/`.

**Neden:** Her PR otomatik test + lint + security'den geçmeli.

**Neden şimdi:** Faz 0 push'undan itibaren kapı aktif olmalı.

### 4.12. v0.1.12 — Faz Dokümantasyonu (tamamlama)

**Ne yapılacak:** Faz boyunca canlı tutulan dokümanların kapanış kontrolü:
- `README.md` (bu dosya) — güncel.
- `design.md` — mimari kararlar + ADR'ler.
- `test-plan.md` — test stratejisi.
- `test-report.md` — koşum sonuçları.
- `security-report.md` — güvenlik kontrol listesi sonuçları.
- `CHANGELOG.md` — alt faz başına versiyonlu kayıt.

**Neden:** Kural gereği her faz versiyonlu doküman seti taşır.

**Neden şimdi:** Faz kapanış ritüelinin parçası.

### 4.13. v0.1.13 — Git Init + İlk Commit & Push

**Ne yapılacak:**
- `git init` + `develop` branch.
- Conventional Commits ile alt fazların commit'lenmesi (her alt faza ayrı commit veya gruplanmış commit'ler).
- GitHub repo (kullanıcıdan repo adı ve private/public tercihi alınır).
- Push.

**Neden:** Faz tamamlanma ritüeli.

**Neden şimdi:** Faz 0 bittiğinde, kural gereği.

---

## 5. Test Planı (Özet)

Detaylar `test-plan.md`'de. Faz 0'ın kapanması için yeşil olması gereken testler:

1. **Unit tests** — Domain primitives (BaseEntity, Result, UrlCodeGenerator).
2. **Pipeline behavior tests** — Validation, Authorization, Transaction temel akışları.
3. **Identity integration test** — Login + Refresh + Context-switch uçtan uca.
4. **Multi-tenancy izolasyon testi** — Tenant A, Tenant B verisini göremesin (shared mod).
5. **Türkçe karakter testi** — `unaccent + lower` ile I/İ/ı/i sorgusu doğru.
6. **Timezone testi** — UTC kayıt + farklı timezone'dan sorgu doğru aralık döner.
7. **Localization testi** — Hata kodu TR ve EN'de farklı mesaj döner.
8. **Audit testi** — Entity update sonrası audit satırı üretildi.
9. **Health check** — `/health`, `/health/ready`, `/health/live` 200 döner.

---

## 6. Güvenlik Kapısı

Detaylar `security-report.md`'de. Faz 0 kapanışı için:

- gitleaks taraması temiz (`.env*` dahil edilmemiş).
- `dotnet list package --vulnerable` boş.
- OWASP A1–A10 review notları (auth + envelope yüzeyi için).
- JWT signing key uzunluğu ≥ 256-bit doğrulandı.
- 2FA enrollment akışı manuel doğrulandı (System kullanıcı için zorunlu).
- HTTPS, HSTS, CSP header'ları yapılandırıldı.

---

## 7. Riskler ve Açık Konular

| Risk | Etki | Azaltma |
|---|---|---|
| .NET 10 ekosistem paket uyumu (FluentValidation 11.x, Hangfire, MudBlazor, MAUI) | Orta | v0.1.1'de paket sürümleri çekilirken doğrulanır; uyumsuzlukta plan revize edilir |
| MAUI workload geliştirici makinesinde kurulu olmayabilir | Düşük | `setup-env` script'i kontrol eder, kurulum komutunu gösterir |
| GitHub repo URL'i belirlenmedi | Düşük | v0.1.13'te kullanıcıdan alınır |
| Prod Vault stratejisi netleşmedi | Düşük | Faz 0'da `.env.production` köprü modu; Faz 2-3'te Vault entegrasyonu |

---

## 8. Paralelleştirme

**Faz 0'da paralel ajan kullanılmıyor.** Foundation katmanı her alt fazın bir öncekine bağımlı olduğu için sıralı uygulanır. Paralelleştirme Faz 2+ özellik fazlarında devreye girecek.

---

## 9. Faz Sonrası

Faz 0 bittiğinde:
- `develop` branch'ine push edilmiş, etiketlenmemiş bir baseline.
- Faz 1 (henüz tanımlanacak — beklenen kapsam: ilk iş entity'leri — Tenant, Company, Building, Unit yönetimi; ManagementApp temel ekranları) için sağlam zemin.
- Tüm kural seti `v001+` memory snapshot'larında izlenebilir.
