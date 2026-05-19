---
name: Proje Mevcut Durumu — v0.2.10 Lokalizasyon (f tamam, g kaldı)
description: v0.2.10 e bloğu + f tamamlandı; yalnız g (admin sayfası) kaldı, sonra commit + tag
type: project
originSessionId: 96f774c4-d60a-46bd-a954-eb6e63f04679
---
Son commit'lenen tag: **v0.2.7** (`7bb9b7b`). Bu tag'ten sonraki tüm v0.2.10 lokalizasyon işi **commit edilmemiş** (~55+ değişen dosya + yeni dosyalar). e bloğu + f kapanışında topluca tek commit (`v0.2.10`) atılacak — yalnız g kaldı.

**Why:** Lokalizasyon a→g sırasıyla yürütülüyor; her alt-faz işin doğal kesim noktası. g (admin sayfası) tamamlandıktan sonra toplu test + commit + tag.

**How to apply:** Yeni oturumda önce `git status --short` ile uncommitted işin kapsamını doğrula; `LocalizationCatalog.cs` mevcut anahtar envanteri için master kaynaktır (~480+ anahtar). Sonra g'den devam et.

---

## v0.2.10 Lokalizasyon — Alt-faz Takibi

| Alt-faz | Kapsam | Durum |
|---------|--------|-------|
| a | `LocalizedResource` entity + `User.PreferredCulture` + migration | ✅ |
| b | `LocalizationStore` (singleton) + `DbStringLocalizer` + fallback (current→en-US→tr-TR→[Key]) | ✅ |
| c | `LocalizationSeeder` + `LocalizationCatalog` (~390 anahtar) | ✅ |
| d | AppBar dropdown + `/auth/change-culture` + login User.PreferredCulture | ✅ |
| e.1 | Catalog 391 anahtar (Permission/Module/Common/Nav/Layout/Form/Audit/Banks/LookUp/BuildingSchema/Auth) | ✅ |
| e.2 | Layout: NavMenu, MainLayout, DataTable (+ DataTable code-behind) | ✅ (2026-05-19) |
| e.3 | Form bileşenleri: TenantForm, CompanyForm, RoleForm, PermissionPicker | ✅ (2026-05-19) |
| e.4 | SystemArea sayfaları (Tenants, Companies, Roles, Banks, Audit, LookUp — 12 sayfa) | ✅ (2026-05-19) |
| e.5 | TenantArea sayfaları (Companies, Roles, BuildingSchema, Settings — 8 sayfa) | ✅ (2026-05-19) |
| e.6 | Auth (Login, 2FA Challenge, 2FA PreAuth Enroll, authenticated Enroll) + Home/About/Settings + NotFound — 10 sayfa | ✅ (2026-05-19) |
| f | RTL (Arabic) — `MudRTLProvider` + `<body dir="rtl">` JS interop (ManagementApp + PortalApp) | ✅ (2026-05-19) |
| **g** | **`/system/localization` admin sayfası (`System.Localization.Manage` izniyle) — anahtar düzenleme UI** | **⏸ SIRADAKI** |

---

## Bir Sonraki Oturumda Yapılacak: g

### g — `/system/localization` Admin Sayfası
**Hedef:** System operatör DB'deki lokalizasyon anahtarlarını UI'dan düzenleyebilsin.

Yapılacaklar:
1. **Permission backend:** `Permission.System.Localization.Manage` zaten LocalizationCatalog'da tanımlı; PermissionCatalog'da (Catalog seed) backend tarafına eklenmesi gerek (SystemAdmin built-in rol'ün izin listesine de).
2. **Application katmanı:**
   - `GetLocalizationEntriesQuery(culture, searchTerm, onlyMachineTranslated, page, pageSize)` → DataGrid kaynağı (sayfalı, filtreli)
   - `LocalizationEntryDto(Key, Culture, Value, IsMachineTranslated, UpdatedAt, UpdatedBy?)`
   - `UpdateLocalizationEntryCommand(key, culture, value)` → DB update + `LocalizationStore.Refresh(key)` veya tam reload trigger
3. **UI:** `/system/localization` sayfası (Authorize `[RequirePermission("System.Localization.Manage")]`). DataTable'da Key, TR, EN, AR/RU/DE kolonları (culture dropdown ile seçilen culture göster); satır editing veya drawer ile düzenleme. `IsMachineTranslated` chip'i ile machine-stub'lar işaretli.
4. **Filtreler:** Culture seçici + arama (key/value contains) + "yalnız machine-translated" toggle.
5. Audit Interceptor zaten `LocalizedResource` değişikliklerini Audit DB'ye yazacak (no extra work).

NavMenu'da `Sistem Yönetimi` grubuna eklenir — `NavMenu.Localization` = "Dil Kaynakları" anahtarı zaten hazır, şu an `Disabled=true` & `phase="Faz 1.7"` chip ile placeholder. Permission gate aktive edilince link açılır.

### g Sonrası: v0.2.10 Test + Commit + Tag
1. Manuel test: TR/EN/AR culture'larında her ana akış sayfasına gir, raw `[Key]` fallback'i hiçbir yerde görünmediğini doğrula
2. AR'da RTL spot-check: Login + Dashboard + RolesListPage + RoleEditPage (drawer sağda, navbar mirror)
3. Build temiz (`dotnet build CleanTenant.slnx`)
4. LocalizationSeeder app restart sonrası yeni anahtarları DB'ye seed etmeli (manuel doğrula)
5. v0.2.10 docs: `docs/phases/v0.2/v0.2.10-FINAL-ARCHITECTURE-MAP.md` (Mermaid + PNG, 18 bölüm — feedback_faz_sonu_mimari_haritasi.md kuralına göre)
6. CHANGELOG.md'e v0.2.10 final entry'si yaz (mevcut in-progress entry'yi güncelle)
7. Toplu commit: `v0.2.10 — Lokalizasyon (Tam Tarama + RTL + Admin)`
8. Tag

---

## Konsept Bilgileri (Lokalizasyon Mimari Özeti)

- **Key naming:** Dot-notation, düz hiyerarşi (`User.Read.Description`, `Roles.New.SubmitButton`, `NavMenu.LookUpTables`)
- **Fallback zinciri:** current culture → en-US → tr-TR → `[Key]` raw (dev uyarısı)
- **Aktif diller:** TR (default), EN. AR f alt-fazında RTL ile aktive edildi. RU/DE iskelet
- **EN bootstrap:** Catalog'da explicit EN yoksa `"[EN] {tr}"` machine-stub + `IsMachineTranslated=true`
- **DI:** `services.AddScoped<IStringLocalizer, DbStringLocalizer>()` — non-generic, doğrudan `@inject IStringLocalizer Loc`
- **Razor inject pattern:** `@inject IStringLocalizer Loc`, sonra `@Loc["Key"]` veya `@Loc["Key", arg]`
- **Code-behind partial sınıf:** `[Inject] IStringLocalizer Loc` (razor'da `@inject` yapma — CS0102 duplicate member)
- **Static method'larda Loc gerekirse:** instance method'a dönüştür (BuildingSchema label fonksiyonları, Audit ActionLabel, Login/TwoFactor MessageFor)
- **NavMenu Matches() pattern:** Localize edilmiş başlık üzerinde Contains; kullanıcı aktif UI dilinde aratabilir
- **SubmitButtonText pattern:** Shared form bileşenlerinde `string?` parameter, null/boş ise `Common.Save` fallback (TenantForm, CompanyForm, RoleForm)
- **ExportColumns pattern:** static readonly idi → instance field, OnInitialized'da Loc ile inşa
- **MarkupString:** Loc değeri HTML içeriyorsa (`<b>...</b>`) `(MarkupString)Loc[...].Value` ile sarmala
- **LocalizationCatalog güncellendiğinde:** App restart sonrası `LocalizationSeeder` yeni anahtarları DB'ye yazar

### RTL Mimari (f alt-fazı)
- **MudBlazor 8.5.1'de doğru komponent ismi:** `MudRTLProvider` (büyük R-T-L) — `MudRtlProvider` (camelCase) yanlış
- **LanguageOption.IsRtl bayrağı:** `LanguageService.Supported` listesinde ar-SA → true
- **`<body dir="rtl">` mekanizması:** `cleantenant.setBodyDirection(dir)` JS fonksiyonu, MainLayout `OnAfterRenderAsync(firstRender)`'da çağrılır. Idempotent — cookie-driven culture değişimi tam reload tetiklediği için her load'da yeniden set
- **PortalApp:** Kendi `cleantenant.js` dosyası yoktu → `App.razor`'a inline `<script>` ile `setBodyDirection` helper eklendi. PortalApp'te `LanguageService` referansı yok → inline `CurrentUICulture.StartsWith("ar")` kontrol
- **MobilApp:** RTL kapsam dışı (MAUI Blazor native `Window.FlowDirection` ayrı strateji, ileriki faz)

---

## Lokalize Edilmemiş Bilinçli Kararlar

- Faz chip'leri ("Faz 1.4", "Faz 1.6" vb.) raw bırakıldı — geliştirici işareti
- App version string'leri (`v0.2.2.c`) raw — dinamik değer
- Phone format pattern `"0(5XX) XXX-XX-XX"` raw — locale-bağımsız mask
- ApartmentLayout numerik gösterimler `"1+0"/"1+1"/...` raw — locale-bağımsız format
- Status / BillingTier / TenantStatus / CompanyStatus enum değerleri `enum.ToString()` raw — lokalize için ayrı enum-resource stratejisi (ileride)
- Home roadmap items, About Faz 0 listesi raw — geliştirici notları
- LanguageSettings radio item etiketleri raw — Faz 1.7 placeholder (gerçek seçici aktive edildiğinde Culture.* kullanılacak)
- Theme display name'leri (`CleanTenantThemes.DisplayName`) — ürün-spesifik isimler, raw
- `Pages/Error.razor` (ASP.NET default şablon) lokalize edilmedi

---

## Önceki Faz Geçmişi (Kısa Özet)

Son commit'lenen tag'ler kronolojik:
- **v0.1.x** — Faz 0 (Backend: Auth + 2FA + Multi-scope + Support Mode + MediatR pipeline + 4 DB hibrit + 146 yeşil test)
- **v0.2.1** — ManagementApp Shell + 4 Tema + MudBlazor
- **v0.2.2 → v0.2.2.b** — Auth UI (Login + 2FA Challenge + Pre-auth Enrollment + UX iyileştirme)
- **v0.2.3.a/b** — Main DbContext + Company entity + Switch-Tenant UI
- **v0.2.4.c/d** — Company CRUD + Form bileşeni (CHANGELOG'da yok, commit'lerden takip edilebilir)
- **v0.2.5.a→e** — Permission/Role Readers + Role CRUD + WebApi + ManagementApp Role UI (CHANGELOG'da yok)
- **v0.2.6** — Audit Explorer
- **v0.2.7** — PortalApp Shell MVP + CompanyCreatePage TenantId fix
- **(uncommitted)** v0.2.8/v0.2.9 izleri commit'lerde görünmüyor — direkt v0.2.10'a sıçranmış olabilir veya in-progress
- **(uncommitted) v0.2.10** — Lokalizasyon (Tam Tarama + RTL + Admin) — ŞU AN

CHANGELOG.md'de eksik kayıtlar: v0.2.4.x, v0.2.5.x, v0.2.8, v0.2.9. v0.2.10 in-progress entry'si bu güncellemeyle eklendi (docs/phases/v0.2/CHANGELOG.md).

---

## v0.3 Planı (v0.2.10 Bittikten Sonra)

Plan dosyası: `C:\Users\yusuf\.claude\plans\soft-doodling-sutton.md` (henüz v0.3 plan yazılmadı)

Kapsam:
- Domain: `Unit`, `Resident`, `UnitResident` entity'leri (Main DB)
- Application: Unit/Resident CRUD handler'ları
- WebApi: UnitEndpoints, ResidentEndpoints
- ManagementApp: CompanyArea Unit listesi, Resident yönetimi
- PortalApp: Sakin self-servis (kendi birim bilgisi)
