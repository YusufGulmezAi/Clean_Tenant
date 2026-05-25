# Acme.Saas — Proje İşletim Kılavuzu (CLAUDE.md)

Bu dosya bu repoda çalışan herkes (insan + AI) için BAĞLAYICI kurallardır.
Varsayılan davranışı geçersiz kılar. İhlal = PR reddi.

## 0. Altın Kurallar
- **Dil:** Tüm doküman/yorum/commit açıklaması **Türkçe**; tüm kod, tanımlayıcı,
  entity/property/enum/dosya adı **İngilizce**. Türkçe tanımlayıcı kabul edilmez.
- **UI kararları:** Layout/renk/tema/komponent gibi görsel/UX kararı TEK BAŞINA
  alınmaz; önce kullanıcıya danışılır.
- **Doğrulama önce:** "Bitti/çalışıyor/geçti" demeden önce komutu çalıştır, çıktıyı
  göster. Kanıtsız başarı iddiası yok.
- **TDD:** Üretim kodu öncesi başarısız test. Refactor mevcut testlerle korunur.

## 1. Mimari (Clean Architecture)
- Katmanlar: `Core` (Domain, Application, SharedKernel) → `Infrastructure` (ilgi
  alanına göre AYRI projeler: Persistence, Identity, Caching, Logging, Storage,
  BackgroundJobs, Export) → `Presentation` (Web/Api/App).
- **Bağımlılık yönü:** dışarıdan içeriye. Core hiçbir Infrastructure'a referans
  vermez. Bu kural **mimari testle** (NetArchTest) otomatik denetlenir.
- **CQRS + MediatR:** Her iş davranışı bir Command/Query + Handler. Cross-cutting
  concern'ler pipeline behavior'da (Authorization, Validation, Caching, Logging,
  Idempotency) — handler'da TEKRAR ETMEZ.
- Dikey dilim (vertical slice) yapısı: `Features/<Area>/<UseCase>/` altında
  Command + Handler + Validator + Result bir arada.

## 2. Domain Tasarımı
- **Zengin aggregate, anemik DEĞİL.** Setter'lar `private`; nesne yalnızca statik
  fabrika (`Create`/`Record`) veya davranış metoduyla geçerli kurulur. Koleksiyonlar
  `IReadOnlyCollection`, mutasyon yalnız aggregate metoduyla.
- **Invariant aggregate'te korunur** (ör. `SUM(satırlar) == toplam`); her davranış
  sonunda doğrulanır.
- **Domain event'ler ÜRETİLİR ve DAĞITILIR.** `BaseEntity` event tamponu tutar;
  `SaveChanges` sonrası **Outbox** üzerinden güvenilir dağıtım yapılır (event kaybı
  yok, en-az-bir-kez). Olay tanımlayıp dağıtmamak yasak (ölü kod).
- Value Object'ler immutable `record`; marker arayüzler (`IAggregateRoot`,
  `ITenantScoped`, `IHasUrlCode`) tipsel garanti sağlar.

## 3. Veri Erişimi
- **Yazım EF Core, okuma Dapper** (karmaşık/raporlama sorguları).
- **Multi-tenancy:** `ITenantScoped` → global query filter ile `tenant_id` izolasyonu.
  Her tenant-scoped entity için **cross-tenant sızıntı testi** ZORUNLU.
- **Migration akışı:** migration üret → `scripts/env-migrate` ile TÜM DB'lere uygula
  → ondan sonra app/seeder çalıştır. Atlanırsa "relation does not exist".
- Standartlar: `UrlCode` (9-char Base58) paylaşılabilir kimlik; tüm zaman UTC +
  görüntüde yerel TZ; soft-delete + audit alanları `BaseEntity`'de; optimistic
  concurrency (xmin/RowVersion).

## 4. API & Yanıt
- Global **Result/Response envelope**; başarı/hata tek tip. HTTP'de ProblemDetails.
- **Hata kodu kataloğu** (ör. `COL-001`): her hata kod + Türkçe mesaj.
- **FluentValidation** ile girdi doğrulama, ValidationBehavior'da merkezi.
- API versiyonlama (`/v1`), rate limiting, idempotency key (yazma uçları).

## 5. Güvenlik (banka-seviyesi hedefi → kanıtla)
- AuthN + scope-bazlı AuthZ (System/Tenant/Company/Unit); System için 2FA zorunlu.
- Secret'lar repoda DEĞİL; prod'da vault. Şifreleme at-rest + in-transit.
- **PII etiketleme** (`[Sensitive]`) + audit'te otomatik redaksiyon.
- **KVKK:** veri saklama/silme politikası, açık rıza, veri sahibi talepleri.
- Her faz kapanışında **güvenlik kapısı**: SAST + bağımlılık taraması + tehdit
  gözden geçirme; bulgular kapatılmadan faz kapanmaz.

## 6. Gözlemlenebilirlik
- **Serilog** → ayrı Log DB; **OpenTelemetry** trace/metric. Yapısal log (mesaj
  şablonu, PII yok). Ayrı **Audit DB** (kim-ne-ne zaman, değişiklik delta'sı).
- Health check (liveness/readiness); correlation/trace id her istekte.

## 7. Lokalizasyon
- DB-tabanlı kaynaklar, çok dil (TR/EN/AR/RU/DE); Arapça için RTL.

## 8. Test & Kalite
- **Test piramidi:** Domain unit (en ucuz, invariant'lar burada) → Application unit
  → Infrastructure integration (Testcontainers) → UI component → API integration.
- **Mimari testler** katman ihlallerini yakalar.
- **Faz-bazlı zorunlu test + güvenlik kapısı** (slice bazında değil, faz kapanışında).
- CI yeşil olmadan merge yok.

## 9. DevOps & Süreç
- Docker, ortam başına compose + secret (Development/Test/Demo/Production).
- **CI/CD:** build + test + SAST + SCA + secret-scan; conventional commits + semver.
- Her faz kapanışında: `docs/phases/vX.Y/` mimari harita (Mermaid+PNG) + ADR'ler +
  memory snapshot.
- Build/run öncesi çakışan portu kapat; `scripts/env-run` ile başlat.

## 10. Definition of Done (bir iş "bitti" sayılır eğer)
- [ ] Davranış testle kaplı (önce kırmızı görüldü) ve tüm testler yeşil
- [ ] Mimari testler + lint + format geçti
- [ ] Hata yolları Result/katalog ile dönüyor, log/audit üretiyor
- [ ] Multi-tenant ise izolasyon testi var
- [ ] Güvenlik: yeni girdi doğrulanmış, PII etiketli, yetki kontrolü var
- [ ] Türkçe doküman + (gerekiyorsa) ADR + commit conventional
