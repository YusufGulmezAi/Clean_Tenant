---
name: Proje Mevcut Durumu — v0.2.10 Lokalizasyon (e.6 tamam, f/g kaldı)
description: v0.2.10 e alt-fazlarının tümü tamamlandı (e.1→e.6); RTL ve admin sayfası (f, g) sıradaki
type: project
originSessionId: 96f774c4-d60a-46bd-a954-eb6e63f04679
---
Son commit'lenen tag: **v0.2.7** (`7bb9b7b`). Bu tag'ten sonraki tüm v0.2.10 lokalizasyon işi **commit edilmemiş** (~50+ değişen dosya). e bloğu kapanışında topluca tek commit (`v0.2.10`) atılacak.

**Why:** Lokalizasyon a→g sırasıyla yürütülüyor; her alt-faz işin doğal kesim noktası. f (RTL) ve g (admin sayfası) tamamlandıktan sonra toplu test + commit.

**How to apply:** Yeni oturumda önce `git status --short` ile uncommitted işin kapsamını doğrula; `LocalizationCatalog.cs` mevcut anahtar envanteri için master kaynaktır (~450+ anahtar). Sonra f'den devam et.

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
| **f** | **RTL (Arabic) — `body[dir="rtl"]` + MudBlazor `RightToLeftProvider`** | **⏸ SIRADAKI** |
| g | `/system/localization` admin sayfası (`System.Localization.Manage` izniyle) — anahtar düzenleme UI | ⏸ |

---

## Bir Sonraki Oturumda Yapılacaklar (f → g sırasıyla)

### f — RTL Desteği (Arabic)
**Hedef:** Aktif kültür AR olduğunda tüm UI sağdan-sola akar.

Yapılacaklar:
1. `MainLayout.razor`'a `MudRtlProvider` ekle (mevcut MudThemeProvider ile birlikte). RTL state'i `CultureInfo.CurrentUICulture.Name == "ar-SA"` ile resolve edilir.
2. `<body dir="rtl">` attribute'ı için JS interop ya da App.razor seviyesinde script. AR aktifken `document.body.setAttribute('dir', 'rtl')`, değilse `ltr`.
3. PortalApp ve MobilApp MainLayout'larına da aynı şey uygulanır.
4. Spot-check: kritik sayfalarda (Login, Dashboard, RoleEditPage) AR'da görsel kontrol — MudBlazor zaten RTL-aware ama özel CSS'lerimizde (ör. `ml-2`, `mr-3`) sorun olabilir; `ms-2`, `me-3` MUDBlazor logical class'larına geçiş gerekirse listele.

Test: TR/EN/AR culture'larında üç farklı sayfa (Login, Dashboard, RoleEditPage) ekran görüntüsü ile doğrula.

### g — `/system/localization` Admin Sayfası
**Hedef:** System operatör DB'deki lokalizasyon anahtarlarını UI'dan düzenleyebilsin.

Yapılacaklar:
1. Yeni permission: `System.Localization.Manage` (Catalog'da `Permission.System.Localization.Manage` zaten tanımlı — backend tarafı eklenir).
2. Application katmanı:
   - `GetLocalizationEntriesQuery(culture, searchTerm, onlyMachineTranslated)` → DataGrid kaynağı
   - `UpdateLocalizationEntryCommand(key, culture, value)` → tek anahtar güncelleme + `LocalizationStore.Refresh(key)`
3. UI: `/system/localization` sayfası. DataTable'da Key, TR, EN, AR/RU/DE kolonları; satır editing (in-place) veya drawer ile düzenleme. `IsMachineTranslated` chip'i ile machine-stub'lar işaretli.
4. Filter: culture seçici + arama + "yalnız machine-translated".
5. Audit Interceptor zaten `LocalizedResource` değişikliklerini Audit DB'ye yazacak.

NavMenu'da `Sistem Yönetimi` grubuna eklenir (yeri zaten `NavMenu.Localization` = "Dil Kaynakları" anahtarıyla hazır — şu an permission gate'i eklenince link açılacak).

---

## v0.2.10 Test ve Commit Stratejisi (g sonrası)

1. Manuel test: TR, EN, AR culture'larında her ana akış sayfasına gir, raw `[Key]` fallback'i hiçbir yerde görünmediğini doğrula
2. Build temiz olduğunu doğrula (`dotnet build`)
3. LocalizationSeeder'ın yeni anahtarları DB'ye seed ettiğini app restart sonrası doğrula
4. v0.2.10 docs (faz mimari haritası — `docs/phases/v0.2/v0.2.10-FINAL-ARCHITECTURE-MAP.md`)
5. Toplu commit: `v0.2.10 — Lokalizasyon (Tam Tarama + Geçiş)`
6. Tag

---

## Konsept Bilgileri (Lokalizasyon Mimari Özeti)

- **Key naming:** Dot-notation, düz hiyerarşi (`User.Read.Description`, `Roles.New.SubmitButton`, `NavMenu.LookUpTables`)
- **Fallback zinciri:** current culture → en-US → tr-TR → `[Key]` raw (dev uyarısı)
- **Aktif diller:** TR (default), EN. AR/RU/DE iskelet — f alt-fazında AR aktive edilecek
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

## v0.3 Planı (Lokalizasyon Bittikten Sonra)

Plan dosyası: `C:\Users\yusuf\.claude\plans\soft-doodling-sutton.md` (henüz v0.3 plan yazılmadı)

Kapsam:
- Domain: `Unit`, `Resident`, `UnitResident` entity'leri (Main DB)
- Application: Unit/Resident CRUD handler'ları
- WebApi: UnitEndpoints, ResidentEndpoints
- ManagementApp: CompanyArea Unit listesi, Resident yönetimi
- PortalApp: Sakin self-servis (kendi birim bilgisi)
