---
name: Proje Mevcut Durumu — v0.2.11.g commit edildi, v0.3 iptal
description: v0.2.11.g commit edildi; v0.3 iptal — yeniden planlama yapılacak; faz v0.2.11 devam ediyor
type: project
originSessionId: 7e3ff30c-d6de-45ab-976e-8a70ec68b74b
---
Son commit'lenen tag: **v0.2.10**. Faz 1'in lokalizasyon kapanışı — TR/EN/AR/RU/DE + RTL + DB-tabanlı + `/system/localization` admin sayfası. Mimari harita: `docs/phases/v0.2/v0.2.10-FINAL-ARCHITECTURE-MAP.md` (kompakt 9 bölüm Mermaid).

**Why:** Faz 1 hâlâ devam ediyor — NavMenu'de "Faz 1.4" (SystemSettings), "Faz 1.5" (Users/PermissionCatalog), "Faz 1.6" (LogViewer/Units/CompanySettings/SupportAccess/AuditHistory/TenantUsers), "Faz 1.7" (Residents/Invoices) chip'leriyle placeholder linkler duruyor. v0.3 sıradaki alt-faz olarak Unit/Resident domain modelini açar.

**How to apply:** Yeni oturumda `git log --oneline -5` ve `git tag | tail -5` ile gerçek durumu doğrula. v0.2.10 tag'inden sonra commit varsa burada yansıtmamış olabilir; o değişiklikleri ayrıca incele.

---

## v0.2.10 — Final Özet

**Lokalizasyon mimari taşları:**
- `LocalizedResource` entity (Catalog DB) + `LocalizationStore` singleton in-memory + `DbStringLocalizer` (`IStringLocalizer` impl, scoped)
- Fallback zinciri: current culture → en-US → tr-TR → `[Key]` raw (dev uyarısı)
- Cookie-driven culture change (`/auth/change-culture`) + `User.PreferredCulture` persist
- RTL: `MudRTLProvider` + `<body dir="rtl">` JS interop (ManagementApp + PortalApp)
- 615 anahtar (TR + EN explicit; AR/RU/DE machine-stub `"[<CULTURE>] {tr}"` placeholder)
- 1230 satır seed (615 key × 2 culture; AR/RU/DE on-demand)

**v0.2.10.g — Admin sayfası bileşenleri:**
- `/system/localization` Razor sayfa (Audit Explorer pattern: filtre paneli + MudDataGrid + sağdan edit drawer)
- Application slice: `Features/System/Localization/` (7 dosya — DTO + Filter + PageResult + Query/Handler + Command/Handler/Validator) + `ILocalizationCacheRefresher` abstraction
- Infrastructure: `LocalizationCacheRefresher` concrete (`LocalizationStore.ReloadAsync` köprüsü)
- WebApi: `GET` + `PUT /api/v1/system/localization/entries`
- Permission: `System.Localization.Manage` PermissionCatalog'da + SystemAdmin baseline seed (`SystemAdminBaselinePermissions` listesi — ileride genişler)
- LocalizationCatalog: 27 yeni `LocalizationManage.*` anahtarı (TR + EN explicit)
- NavMenu: "Dil Kaynakları" link aktif (Sistem Yönetimi grubu, Translate ikonu)

**Türkçe arama desteği:** Query handler `ToLowerInvariant().Contains()` kullanır → EF Core PostgreSQL `LOWER()` Unicode case-folding'e çevirir. Ö↔ö, Ü↔ü, Ş↔ş, Ç↔ç, Ğ↔ğ doğru eşleşir. İ↔ı dotted/dotless Türkçe-spesifik durumu ileride PostgreSQL `tr-x-icu` collation ile genişletilebilir.

**Cache invalidation:** Update sonrası `IsMachineTranslated = false` auto-set + `LocalizationStore.ReloadAsync` sync — UI refresh ettiğinde NavMenu/sayfa label'ları canlı yansır. Multi-instance senaryosu için Redis pub-sub invalidation v0.3+'ta düşünülecek (mevcut `CacheInvalidationSubscriber` pattern'i örnek).

---

## Sıradaki Faz

**v0.3 iptal edildi** — kullanıcı yeniden planlama yapacak. v0.2.11 ile devam ediliyor.

Mevcut commit: `0f2ce00` — v0.2.11.g

---

## Önceki Faz Geçmişi (Kısa)

Tag'lenen ana kapsamlar kronolojik:
- **v0.1.x** — Faz 0 (Backend: Auth + 2FA + Multi-scope + Support Mode + MediatR pipeline + 4 DB hibrit + 146 yeşil test) — `v0.1.7` final
- **v0.2.1** — ManagementApp Shell + 4 Tema + MudBlazor
- **v0.2.2.x** — Auth UI (Login + 2FA + UX iyileştirme)
- **v0.2.3.a/b** — Main DbContext + Company entity + Switch-Tenant UI
- **v0.2.4.c/d** — Company CRUD + Form
- **v0.2.5.a→e** — Permission/Role Readers + Role CRUD + WebApi + UI
- **v0.2.6** — Audit Explorer
- **v0.2.7** — PortalApp Shell MVP + CompanyCreatePage TenantId fix
- **v0.2.10** — Lokalizasyon (final, admin sayfası dahil) — **Faz 1 lokalizasyon kapanışı**

v0.2.4.x / v0.2.5.x / v0.2.8 / v0.2.9 alt-fazlar tek tek tag almayıp birleşik commit'lerle ilerlemişlerdir; CHANGELOG'da görünür ama git tag yok.
