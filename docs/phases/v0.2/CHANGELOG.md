# Faz v0.2 — CHANGELOG (Faz 1)

Bu dosya, Faz 1 (UI başlangıç + ManagementApp) kapsamında yapılan tüm alt-faz değişikliklerini versiyon damgalı kayıt altına alır. Son giriş en üsttedir.

**Faz 0 kapanış belgesi:** [../v0.1/v0.1-FINAL-ARCHITECTURE-MAP.md](../v0.1/v0.1-FINAL-ARCHITECTURE-MAP.md)
**Faz 0 CHANGELOG:** [../v0.1/CHANGELOG.md](../v0.1/CHANGELOG.md)

---

## v0.2.13.e — 2026-05-23 — Süper Yetkili Roller & Erişim

**Mimari harita**: [v0.2.13.e-FINAL-ARCHITECTURE-MAP.md](v0.2.13.e-FINAL-ARCHITECTURE-MAP.md)

### Kapsam
Süper yetkili `TenantAdmin`/`CompanyAdmin` rolleri (izin seed), her yeni Site'a zorunlu CompanyAdmin provisioning, Tenant→Site izin cascade'i, kullanıcı bazlı kalıcı tema (v0.2.13.d), Siteler liste tasarımının bağlamlar arası birleştirilmesi, Context Switcher geçiş deneyimi.

### Alt-faz Değişiklik Özeti

| Alt-faz | Özet |
|---|---|
| **d** | Avatar menü sadeleştirme (2FA/Ayarlar kaldır) + tema profil > Tema sekmesi + `UserThemeService` (DB kalıcı tema) + `AddUserThemePreferences` migration + Siteler `CompaniesListView` birleştirme + Context Switcher overlay & dashboard yönlenmesi |
| **e** | `TenantAdmin`/`CompanyAdmin` izin seed + `CreateCompany` CompanyAdmin provisioning + `IScopePermissionResolver` cascade + `SwitchContext` guard genişletme + 21 faz kapanış testi |

### Mimari Kararlar
- **Built-in rolleri doldur, yeni rol türetme**: TenantAdmin = Tenant+Company (78 izin), CompanyAdmin = Company (33 izin). System/Unit hariç (privilege ceiling).
- **Cascade aşağı yönlü** (Tenant → tüm siteler); yukarı sızma yok. Ortak `IScopePermissionResolver` ile tek yerde.
- **Support Mode v2 korundu** — System operatör geçişi (SupportSession + `AllowSystemWriteAccess`) değiştirilmedi.
- **Tema DB'de tek kaynak** — cihazlar arası taşınır, her login'de uygulanır, default "Kurumsal Mavi".
- **Şema değişikliği minimum** — Company admin `UserRoleAssignment` ile izlenir (migration gerektirmez; yalnız tema kolonları).

### Doğrulama
- ✓ `dotnet build CleanTenant.slnx` — 0 hata / 0 uyarı
- ✓ 21 yeni test yeşil (11 Application unit + 5 ManagementApp bUnit + 5 Infrastructure integration)
- ✓ Seeding canlı doğrulandı (TenantAdmin/CompanyAdmin izin atamaları + listening)

### Not
v0.2.13 umbrella fazının diğer parçaları (Muhasebe / Bütçe / Tahakkuk / Yapı Şeması ve v0.2.12) bu CHANGELOG'a işlenmemiştir; bu giriş yalnız bu oturumun kapsamını belgeler.

---

## v0.2.11 — 2026-05-20 — UX & Form İyileştirmeleri

**Tag**: `v0.2.11` · **Mimari harita**: [v0.2.11-FINAL-ARCHITECTURE-MAP.md](v0.2.11-FINAL-ARCHITECTURE-MAP.md)

### Kapsam
TenantForm tab yapısı, Türkiye coğrafya verisi bootstrap, Audit Explorer filtre yenileme, Banks modülü genişletme, FluentValidator i18n, PermissionPicker 2 kademeli akordiyon yeniden tasarımı, CompanyForm bölüm grupları.

### Alt-faz Değişiklik Özeti

| Alt-faz | Özet |
|---|---|
| **a** | VKN regex fix + LookUp permission + MudBlazor i18n + genel UX |
| **b** | Tenant genişletme (İletişim/Sözleşme alanları) + Province/District/Neighborhood entity + CSV bootstrap (81 il, 973 ilçe) + LookUp UX |
| **c** | UI cleanup: Tenants bağlam butonu kaldır, Roles Scope kolonu kaldır, Search sol-üst taşı |
| **d** | TenantForm → 4 tab (Genel/İletişim/Sözleşme/Paket) + adres cascade dropdown + Context Switcher token refresh + Validator i18n |
| **e** | Banks: `IsActive`, `SupportsEft`/`SupportsPos`/`SupportsTahsilat` + tablo kolonları |
| **f** | Audit Explorer: DB-distinct dropdown filtreler + ilk yüklemede boş tablo (performans) |
| **g** | PermissionPicker: 2 kademeli akordiyon (Modül→Entity), CRUD ikon butonları, seçili badge; CompanyForm: Site Bilgileri/Kimlik/İletişim bölüm grupları; 2 yeni lokalizasyon anahtarı |

### Mimari Karar: PermissionPicker 2 Kademeli Akordiyon

- Modül seviyesi MudExpansionPanel (eskiden de vardı) altına **entity seviyesi** MudExpansionPanel eklendi
- Her entity paneli başlığı: entity adı + `seçili/toplam` badge (mor renk — seçim varsa aktif)
- CRUD butonları: single-letter `MudButton` → `div + MudIcon` (CheckCircle/Circle, CSS `--clr` custom property)
- Kullanıcı entity gruplarını tek tek açıp kapatabilir; modül seviyesinde grup seç/kaldır korundu

### Mimari Karar: TenantForm Tab Yapısı

- `TenantFormMode` enum üzerinden read-only / gizli alan kontrolü korundu
- Adres cascade: `Province → District → Neighborhood` — her seçim bir alt listeyi sıfırlayıp yeniden yükler
- `ILookUpCatalogReader` inject (Infrastructure DI) — Blazor component'te doğrudan reader çağrısı
- `MudDatePicker` ↔ `DateOnly?` köprüsü: component `DateTime?` ister, model `DateOnly?` tutar; private bridge property

---

## v0.2.10 — 2026-05-19 — Lokalizasyon (TR/EN/AR/RU/DE + RTL + DB-tabanlı + Admin)

**Tag**: `v0.2.10` · **Mimari harita**: [v0.2.10-FINAL-ARCHITECTURE-MAP.md](v0.2.10-FINAL-ARCHITECTURE-MAP.md)

### Kapsam
DB-tabanlı çoklu dil altyapısı (TR/EN/AR/RU/DE), tüm UI yüzeyinin lokalizasyonu, RTL (Arapça) desteği ve `/system/localization` admin yönetim sayfası. Faz 0/1 backend lokalize edilmemiştir — yalnız Presentation katmanı kapsamdadır.

### Mimari Karar: DB-Backed Localization (DbStringLocalizer + LocalizationStore)

- ASP.NET Core `IStringLocalizer` arayüzünün **DB-backed** implementasyonu — `.resx` yerine `LocalizedResource` entity (Catalog DB)
- **Singleton `LocalizationStore`** uygulama başlangıcında tüm çevirileri belleğe yükler — runtime'da Loc çağrısı O(1) sözlük araması
- **Fallback zinciri:** current culture → en-US → tr-TR → `[Key]` raw (eksik çeviri için dev uyarısı)
- **Key naming:** Dot-notation düz hiyerarşi (`Roles.New.SubmitButton`, `NavMenu.LookUpTables`)
- **EN bootstrap:** Catalog'da explicit EN yoksa `"[EN] {tr}"` machine-stub + `IsMachineTranslated=true`

### Mimari Karar: Cookie-Driven Culture Change

- Dil değişimi `/auth/change-culture` form-post endpoint'i üzerinden cookie set + (giriş yapılmışsa) `User.PreferredCulture` DB persist
- Endpoint sonrası tam reload — `CultureInfo.CurrentUICulture` request kapsamına yeniden uygulanır

### Mimari Karar: RTL via MudRTLProvider + JS Interop

- AR culture aktif olduğunda `MudRTLProvider RightToLeft=true` MudBlazor bileşenlerini mirror'lar
- `<body dir="rtl">` ve `<html dir="rtl">` JS interop (`cleantenant.setBodyDirection`) ile DOM-level RTL custom CSS sayfalarda da çalışır
- ManagementApp + PortalApp kapsamda; MobilApp (MAUI native FlowDirection) kapsam dışı

### Alt-faz Durum Tablosu

| Alt-faz | Kapsam | Durum |
|---------|--------|-------|
| a | `LocalizedResource` entity + `User.PreferredCulture` + migration | ✅ |
| b | `LocalizationStore` (singleton) + `DbStringLocalizer` + fallback | ✅ |
| c | `LocalizationSeeder` + `LocalizationCatalog` (~390 anahtar) | ✅ |
| d | AppBar dropdown + `/auth/change-culture` + login User.PreferredCulture | ✅ |
| e.1 | Catalog 391 anahtar (Permission/Module/Common/Nav/Layout/Form/Audit/...) | ✅ |
| e.2 | Layout: NavMenu, MainLayout, DataTable (+ DataTable code-behind) | ✅ |
| e.3 | Form bileşenleri: TenantForm, CompanyForm, RoleForm, PermissionPicker | ✅ |
| e.4 | SystemArea sayfaları (Tenants, Companies, Roles, Banks, Audit, LookUp — 12 sayfa) | ✅ |
| e.5 | TenantArea sayfaları (Companies, Roles, BuildingSchema, Settings — 8 sayfa) | ✅ |
| e.6 | Auth (Login, 2FA Challenge, 2FA PreAuth Enroll, authenticated Enroll) + Home/About/Settings + NotFound — 10 sayfa | ✅ |
| f | RTL (Arabic) — `MudRTLProvider` + `<body dir="rtl">` JS interop (ManagementApp + PortalApp) | ✅ |
| **g** | **`/system/localization` admin sayfası + permission seed + Application slice + WebApi endpoint + cache refresher** | ✅ (2026-05-19) |

### Eklenen Anahtar İstatistiği

- LocalizationCatalog.cs: ~480 anahtar
  - Permission Module/Description (60)
  - Common UI (24)
  - Navigation Menu (28)
  - Layout AppBar/Footer/Error (14)
  - DataTable (8)
  - Login + 2FA (40+)
  - Tenants/Companies/Roles list+CRUD+form (90+)
  - BuildingSchema (60+)
  - Audit/Banks/LookUp (50+)
  - Settings + Home + About + NotFound (20+)
  - Page titles (12)
  - Error format strings + tooltips (15)

### Yeni / Güncellenen Dosyalar (özet)

**Domain:**
- `Domain/Localization/LocalizedResource.cs` (yeni entity)
- `Domain/Identity/Users/User.cs` — `PreferredCulture` property

**Infrastructure.Persistence:**
- `Catalog/Configurations/LocalizedResourceConfiguration.cs` (yeni)
- `Catalog/Migrations/20260519140924_AddLocalizationSchema.cs` (yeni)
- `Localization/LocalizationStore.cs` (yeni — singleton in-memory)
- `Localization/DbStringLocalizer.cs` (yeni — `IStringLocalizer` impl)
- `Seeding/LocalizationSeeder.cs` (yeni)
- `Seeding/LocalizationCatalog.cs` (yeni — ~480 anahtar)
- `DependencyInjection.cs` — `AddSingleton<LocalizationStore>` + `AddScoped<IStringLocalizer, DbStringLocalizer>`

**ManagementApp Auth:**
- `Auth/AuthEndpoints.cs` — `POST /auth/change-culture` (+86 satır)

**ManagementApp Services:**
- `Services/LanguageService.cs` — `LanguageOption.IsRtl` bayrağı (f), `IsCurrentCultureRtl()` helper

**ManagementApp Components (Layout + Shared + Pages):**
- _Imports.razor — `Microsoft.Extensions.Localization` using
- Layout/MainLayout.razor — `MudRTLProvider` + AppBar language dropdown + JS interop
- Layout/NavMenu.razor — `Matches()` filter localize değerle çalışır
- Shared/DataTable.razor + .razor.cs — IStringLocalizer inject, ResolvedSearchPlaceholder
- Shared/TenantForm.razor + .razor.cs — Loc + SubmitButtonText nullable + ResolvedSubmitButtonText
- Shared/CompanyForm.razor + .razor.cs — aynı pattern
- Shared/RoleForm.razor + .razor.cs — aynı pattern
- Shared/PermissionPicker.razor — Legend + tooltips
- Pages/SystemArea/* (12 sayfa) — full lokalize, ExportColumns instance field
- Pages/TenantArea/* (8 sayfa) — full lokalize, BuildingSchema label functions instance method
- Pages/Login.razor + TwoFactorChallenge.razor + TwoFactorEnrollmentPreAuth.razor — MessageFor static → instance
- Pages/Home.razor + About.razor + NotFound.razor — lokalize
- Pages/Settings/ThemeSettings.razor + LanguageSettings.razor + TwoFactorEnrollment.razor — lokalize

**ManagementApp wwwroot:**
- `wwwroot/js/cleantenant.js` — `submitCultureChange` (d) + `setBodyDirection` (f)

**PortalApp:**
- `Components/App.razor` — inline `setBodyDirection` helper
- `Components/Layout/MainLayout.razor` — `MudRTLProvider` + JS interop

### Kararlar (Lokalize Edilmemiş Bilinçli)

- Faz chip'leri ("Faz 1.4" vb.) raw — dev marker
- App version string'leri raw — dinamik
- Phone format mask `"0(5XX) XXX-XX-XX"` raw — locale-bağımsız
- Apartment layout numerik (`"1+0"/"1+1"`) raw — format
- Enum `enum.ToString()` raw (Status/BillingTier/CompanyStatus) — ayrı enum-resource stratejisi (ileride)
- Home roadmap items + About Faz 0 listesi raw — dev notes
- `Pages/Error.razor` (ASP.NET default) lokalize edilmedi

### v0.2.10.g — Localization Admin Sayfası

`/system/localization` — System operatör DB'deki çeviri anahtarlarını UI'dan düzenler. Tek-culture görünüm (culture dropdown + arama + "yalnız makine çevirisi" filtresi); sağdan drawer ile inline edit; kaydet sonrası `LocalizationStore.ReloadAsync` → NavMenu / sayfa label'ları canlı yansır.

**Yeni dosyalar (Application):**
- `Common/Localization/ILocalizationCacheRefresher.cs` — provider-agnostic abstraction
- `Features/System/Localization/`:
  - `LocalizationEntryListItem.cs` (DTO)
  - `LocalizationEntryFilter.cs` (filter record)
  - `LocalizationPageResult.cs` (sayfalı sonuç)
  - `GetLocalizationEntriesQuery.cs` + `Handler.cs` (`[RequirePermission("System.Localization.Manage")]`)
  - `UpdateLocalizationEntryCommand.cs` + `Handler.cs` + `Validator.cs` (FluentValidation)

**Yeni dosyalar (Infrastructure.Persistence):**
- `Localization/LocalizationCacheRefresher.cs` — `ILocalizationCacheRefresher` → `LocalizationStore.ReloadAsync` köprüsü

**Yeni dosyalar (Presentation.WebApi):**
- `Endpoints/SystemLocalizationEndpoints.cs` — `GET` + `PUT /api/v1/system/localization/entries`

**Yeni dosyalar (Presentation.ManagementApp):**
- `Components/Pages/SystemArea/LocalizationManagePage.razor` — filtre paneli + MudDataGrid + edit drawer (Audit Explorer pattern'iyle simetrik)

**Güncellenen dosyalar:**
- `Application/Common/Persistence/ICatalogDbContext.cs` — `DbSet<LocalizedResource> LocalizedResources`
- `Infrastructure/Persistence/DependencyInjection.cs` — `ILocalizationCacheRefresher` scoped register
- `Infrastructure/Persistence/Seeding/CatalogSeeder.cs` — `SeedSystemAdminPermissionsAsync` + `SystemAdminBaselinePermissions` baseline listesi
- `Infrastructure/Persistence/Seeding/LocalizationCatalog.cs` — 27 yeni `LocalizationManage.*` anahtarı (TR + EN explicit)
- `Presentation/WebApi/Endpoints/EndpointMappingExtensions.cs` — `MapSystemLocalizationEndpoints` registered
- `Presentation/ManagementApp/Components/Layout/NavMenu.razor` — "Dil Kaynakları" link aktif (Sistem Yönetimi grubu, ikon: Translate)

**Mimari kararlar (g'ye özel):**
- **EF Core (Application via `ICatalogDbContext`)** kullanıldı, Dapper değil — `LocalizedResource` küçük tablo (~3K satır), Audit Explorer pattern'iyle simetri + provider-agnostic mimari.
- **`ILocalizationCacheRefresher` abstraction Application'da, concrete `LocalizationCacheRefresher` Infrastructure'da** — Clean Architecture provider sızıntısı yok; `LocalizationStore.ReloadAsync` sarmalanır.
- **SystemAdmin baseline permission seed**: Developer "tam erişim"den farklı olarak SystemAdmin yalnız sistem yönetim sorumluluğuna giren `SystemAdminBaselinePermissions` listesini alır — şu an sadece `System.Localization.Manage`; ileride aynı listede genişler.
- **Türkçe karakter arama**: PostgreSQL `LOWER()` Unicode case-folding kullanılıyor (`ToLowerInvariant().Contains(lower)` EF Core'da bu çevirimi yapar) — Ö↔ö, Ü↔ü, Ş↔ş, Ç↔ç, Ğ↔ğ doğru eşleşir. İ↔ı dotted/dotless ileride `tr-x-icu` collation ile genişletilir.
- **`IsMachineTranslated` auto-clear**: Admin manuel düzeltme yaptığı için update sonrası flag otomatik false; reload sonrası MachineYes chip'i kalkar.

### Doğrulama

- ✓ `dotnet build CleanTenant.slnx` — 0 hata, 0 uyarı (Adım 8a)
- ✓ ManagementApp dev server: `Now listening on http://localhost:5081`
- ✓ SystemAdmin baseline permission seed log: "1 yeni atama yapıldı — `System.Localization.Manage`"
- ✓ Localization seed: 916 yeni satır + LocalizationStore yüklendi (2 kültür, 1230 kayıt)
- ✓ Manuel UI testi — filtre + edit + cache reload + Türkçe arama (Ö↔ö) doğrulandı
- ✓ Faz mimari haritası: [v0.2.10-FINAL-ARCHITECTURE-MAP.md](v0.2.10-FINAL-ARCHITECTURE-MAP.md) (kompakt 9 bölüm)

### Sonraki Adım

**v0.3 — Unit / Resident Modeli** (Faz 1.6 alt-faz). Domain: `Unit`/`Resident`/`UnitResident` (Main DB); Application: CRUD handler'ları; WebApi: `UnitEndpoints`/`ResidentEndpoints`; ManagementApp: `CompanyArea/Units` + Resident yönetimi; PortalApp: sakin self-servis. v0.3 başlangıcında `docs/phases/v0.3/v0.3-FINAL-ARCHITECTURE-MAP.md` yazılır.

---

## v0.2.7 — 2026-05-19 — PortalApp Shell (MVP)

### Kapsam
Site sakinleri için ayrı Blazor Server uygulaması — kimlik doğrulama + temel shell.

### Eklenen / Güncellenen Dosyalar

**CleanTenant.PortalApp.csproj:**
- MudBlazor paket referansı eklendi; `NoWarn MUD0002;CS1591`

**Auth:**
- [Auth/JwtCookieAuthenticationStateProvider.cs](../../../src/Presentation/CleanTenant.PortalApp/Auth/JwtCookieAuthenticationStateProvider.cs) — ManagementApp ile aynı cookie→ClaimsPrincipal çözümleme
- [Auth/AuthEndpoints.cs](../../../src/Presentation/CleanTenant.PortalApp/Auth/AuthEndpoints.cs) — `MapPortalAuthEndpoints()`: sign-in (PersonaSide.Portal), sign-out, 2FA verify/send; cookie adı `cleantenant.portal`

**Components:**
- [App.razor](../../../src/Presentation/CleanTenant.PortalApp/Components/App.razor) — MudBlazor CSS/JS eklendi, `lang="tr"`
- [Routes.razor](../../../src/Presentation/CleanTenant.PortalApp/Components/Routes.razor) — `CascadingAuthenticationState` + `AuthorizeRouteView` + `RedirectToLogin`
- [RedirectToLogin.razor](../../../src/Presentation/CleanTenant.PortalApp/Components/RedirectToLogin.razor) — Yetkisiz kullanıcıları `/login`'e yönlendirir
- [_Imports.razor](../../../src/Presentation/CleanTenant.PortalApp/Components/_Imports.razor) — MudBlazor + Authorization using'leri eklendi

**Layout:**
- [Layout/EmptyLayout.razor](../../../src/Presentation/CleanTenant.PortalApp/Components/Layout/EmptyLayout.razor) — Login sayfası için minimal layout (MudBlazor provider'ları)
- [Layout/MainLayout.razor](../../../src/Presentation/CleanTenant.PortalApp/Components/Layout/MainLayout.razor) — AppBar (branding + tenant/site chip + çıkış), mini drawer (Anasayfa, Profil)

**Pages:**
- [Pages/Login.razor](../../../src/Presentation/CleanTenant.PortalApp/Components/Pages/Login.razor) — Static SSR login formu (Portal persona)
- [Pages/Home.razor](../../../src/Presentation/CleanTenant.PortalApp/Components/Pages/Home.razor) — Hoşgeldin dashboard (tenant + site adı kartları)
- [Pages/Profile.razor](../../../src/Presentation/CleanTenant.PortalApp/Components/Pages/Profile.razor) — Profil stub (`/profile`)

**Program.cs:**
- ManagementApp ile aynı DI pipeline (Application, Persistence, Identity, Caching, Logging)
- Cookie adı `cleantenant.portal`, persona = Portal

### Doğrulama
- ✓ `dotnet build CleanTenant.PortalApp.csproj` — 0 uyarı / 0 hata

### Sonraki Adım
Faz v0.3 — Unit/Resident modeli + sakin self-servis özellikleri.

---

## v0.2.6 — 2026-05-19 — Audit Explorer

### Kapsam
`audit_entries` tablosunu filtreli ve sayfalı okuyabilen sistem yöneticisi sayfası.

### Eklenen Dosyalar (9 yeni)

**Application:**
- [IAuditDbContext.cs](../../../src/Core/CleanTenant.Application/Common/Persistence/IAuditDbContext.cs) — AuditDbContext'i Application katmanına açan interface (`DbSet<AuditEntry> AuditEntries`)
- [AuditListItem.cs](../../../src/Core/CleanTenant.Application/Features/System/Audit/AuditListItem.cs) — Audit Explorer özet DTO (17 alan)
- [AuditFilter.cs](../../../src/Core/CleanTenant.Application/Features/System/Audit/AuditFilter.cs) — Filtre parametreleri (DateFrom/To, UserId, UserEmail, EntityType, Action, TenantId, CompanyId, Page, PageSize)
- [AuditPageResult.cs](../../../src/Core/CleanTenant.Application/Features/System/Audit/AuditPageResult.cs) — Sayfalı sonuç wrapper (Items, TotalCount, Page, PageSize)
- [GetAuditEntriesQuery.cs](../../../src/Core/CleanTenant.Application/Features/System/Audit/GetAuditEntriesQuery.cs) + [GetAuditEntriesQueryHandler.cs](../../../src/Core/CleanTenant.Application/Features/System/Audit/GetAuditEntriesQueryHandler.cs) — EF Core AsNoTracking, dinamik filtre zinciri, OrderByDescending(Timestamp), Skip/Take sayfalama; maks 200/sayfa

**ManagementApp:**
- [AuditExplorerPage.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/Pages/SystemArea/AuditExplorerPage.razor) — `/system/audit` — MudExpansionPanel filtre paneli + MudDataGrid (Hideable kolonlar) + el ile sayfalama + MudDrawer sağ detay görünümü

### Güncellenen Dosyalar

**Infrastructure:**
- [AuditDbContext.cs](../../../src/Infrastructure/CleanTenant.Infrastructure.Persistence/Audit/AuditDbContext.cs) — `IAuditDbContext` eklendi
- [DependencyInjection.cs](../../../src/Infrastructure/CleanTenant.Infrastructure.Persistence/DependencyInjection.cs) — `services.AddScoped<IAuditDbContext>(...)` kaydı

**ManagementApp:**
- [NavMenu.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/Layout/NavMenu.razor) — "Audit Explorer" → `/system/audit` aktif link (önceki "Faz 1.6 / disabled" yerini aldı)

### UI Kararları
- Varsayılan görünür kolonlar: Tarih/Saat, Entity Tipi, Entity ID (kısa), İşlem chip, Kullanıcı, Yönetim (TenantName), Site ID (kısa), Değişiklikler (truncated)
- Varsayılan gizli (toggle edilebilir): IP Adresi, Tarayıcı, İşletim Sistemi, Endpoint
- MudDataGrid `ShowColumnOptions=true` ile built-in kolon seçici aktif
- Satıra tıklayınca sağ MudDrawer'da tam detay + güzel-formatlanmış JSON

### Kısıtlar
- `CompanyName` ve entity instance adı (EntityName) audit DB'de denormalize edilmemiş; CompanyId ve EntityType gösterilir
- TenantName audit_entries'e denormalize yazıldığından "Yönetim" kolonu TenantId yerine ad gösterir

### Doğrulama
- ✓ `dotnet build CleanTenant.ManagementApp.csproj` — 0 uyarı / 0 hata

---

## v0.2.3.b — 2026-05-18 — Switch-Tenant UI (AppBar dropdown + cross-tenant context)

### Sorun
v0.2.3.a Main DB + Company entity altyapısı tamam ama System scope kullanıcısı (dev admin Developer rolünde) Companies UI'sını henüz göremezdi: global query filter `TenantId == _tenantContext.TenantId` System scope'ta null=null=false döner. Companies CRUD'a geçmeden önce AppBar'da "Aktif Tenant" seçici (Soru 1 B çözümü) altyapısı şart.

### Mimari Karar: SwitchTenantCommand (SwitchContext'ten ayrı)
- Mevcut `SwitchContextCommand` kullanıcının **rol ataması olan** scope'a izin verir; bu System operatörlerin cross-tenant erişimi için engel.
- Yeni `SwitchTenantCommand` Tenant-spesifik:
  - **System scope kullanıcı** (Developer / SystemAdmin) → herhangi bir Active tenant'a geçebilir; permissions + roles System'den miras (cross-tenant operasyonel görünürlük).
  - **Alt scope kullanıcı** → yalnız UserRoleAssignments'ta bulunan tenant'a; permissions o tenant'taki rol atamasından (Tenant > Company öncelik).
- Side-effect: yeni AuthSession Redis'e yazılır, eski sessions silinir, refresh chain `TenantSwitch` reason'ıyla revoke + yeni token, JWT yenilenir.

### Mimari Karar: AccessibleTenants Querysi
- `GetAccessibleTenantsQuery` — dropdown'u dolduran liste.
- System scope kullanıcı: tüm `TenantStatus.Active` tenant'lar.
- Alt scope kullanıcı: `UserRoleAssignments`'ta distinct `TenantId`'lere sahip olduğu tenant'lar.

### Eklenen Dosyalar (5 yeni)

**Application:**
- [GetAccessibleTenantsQuery.cs](../../../src/Core/CleanTenant.Application/Features/Auth/Tenants/GetAccessibleTenantsQuery.cs) + Handler
- [SwitchTenantCommand.cs](../../../src/Core/CleanTenant.Application/Features/Auth/Tenants/SwitchTenantCommand.cs) + Handler

**ManagementApp:**
- [Components/Layout/TenantSwitcher.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/Layout/TenantSwitcher.razor) — AppBar dropdown'u (System scope + multi-tenant kullanıcılar için). 0 tenant erişimi olanlarda render edilmez.

### Güncellenen Dosyalar
- [WebApi AuthEndpoints.cs](../../../src/Presentation/CleanTenant.WebApi/Endpoints/AuthEndpoints.cs) — 2 yeni endpoint: `GET /api/v1/auth/accessible-tenants` + `POST /api/v1/auth/switch-tenant` + `SwitchTenantRequest` record.
- [ManagementApp Auth/AuthEndpoints.cs](../../../src/Presentation/CleanTenant.ManagementApp/Auth/AuthEndpoints.cs) — `POST /auth/switch-tenant` form post handler (HttpContext.SignInAsync ile cookie yenile, dropdown'dan tetiklenir).
- [wwwroot/js/cleantenant.js](../../../src/Presentation/CleanTenant.ManagementApp/wwwroot/js/cleantenant.js) — `submitTenantSwitch(tenantId, returnUrl)` JS helper (dinamik form üretir + post).
- [Components/Layout/MainLayout.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/Layout/MainLayout.razor) — AppBar'a `<TenantSwitcher>` yerleştirildi; aktif tenant id/name `ICurrentSessionAccessor.Current` üzerinden okunur.

### Test Eklemeleri (6 yeni integration test)

[TenantSwitchTests.cs](../../../tests/CleanTenant.WebApi.IntegrationTests/Auth/TenantSwitchTests.cs):
1. AccessibleTenants_anonim_401
2. AccessibleTenants_System_scope_tum_Active_tenantlari_doner (Active + Suspended ayrımı)
3. SwitchTenant_anonim_401
4. SwitchTenant_olmayan_tenant_404
5. SwitchTenant_System_user_herhangi_bir_Active_tenanta_gecebilmeli
6. SwitchTenant_pasif_tenanta_gecis_404

### Doğrulama
- ✓ `dotnet build CleanTenant.slnx` — 0 uyarı / 0 hata.
- ✓ `dotnet test CleanTenant.slnx --no-build` — **202 yeşil test** (196 → 202).

### Sonraki Adım
**v0.2.3.c — DataTable<T> generic component + ExportService** (ClosedXML + QuestPDF). Companies'e geçmeden önce reusable tablo + export altyapısı.

---

## v0.2.2.b — 2026-05-18 — UX İyileştirme + Markalama (Login + 2FA + Layout)

### Kapsam
v0.2.2.a sonrası kullanıcı testinde tespit edilen UX eksikleri:
1. Login sayfasındaki teknik metinler ve "CleanTenant" başlığı
2. 2FA kod giriş alanlarında tek input yerine 6 ayrı kutucuk + auto-focus + Backspace geri + paste
3. Recovery code'ları kullanıcının kaybetmemesi için TXT olarak indirme
4. MainLayout statik SSR olduğu için drawer/dark toggle butonları + cascading reactivity çalışmıyordu

### Mimari Karar: Markalama
- Login + 2FA Enrollment ekranlarında "CleanTenant" → **"Toplu Yapı Yönetimi"** (kullanıcı talebi).
- Login sayfasındaki "Yönetim Paneline Giriş" + "E-posta, TCKN/YKN, VKN veya cep telefonunuzla giriş yapabilirsiniz." paragrafları kaldırıldı — gereksiz teknik metin.

### Mimari Karar: 6 Kutucuk OTP Pattern (DRY)
- Yeni reusable Razor component **`OtpCodeInput.razor`** (InteractiveServer sayfalar için).
- Statik SSR sayfaları (TwoFactorChallenge) için **JS modülü `cleantenant.otpForm`** (saf HTML + hidden field birleştirme + buton aktif/pasif).
- JS davranışları (her iki pattern'da ortak):
  - Otomatik focus geçişi (rakam girilince sonraki kutu)
  - Backspace boş kutuda → önceki kutuyu sil ve oraya odaklan
  - Sol/Sağ ok ile gezinti
  - Paste (clipboard) → 6 hanenin tamamına dağıt
  - Hepsi dolunca submit butonu aktif (statik form'da JS, Razor'da `Disabled="@(_code.Length < 6)"`)
- Recovery code modunda (TwoFactorChallenge) 6 kutucuk yerine XXXXX-XXXXX serbest text (Identity'nin recovery code formatı).

### Mimari Karar: Recovery Codes TXT İndirme
- JS modülü `cleantenant.downloadTextFile(filename, content)` — Blob + temp `<a download>` + revoke.
- Razor component IJSRuntime ile çağırır; recovery code listesi + header (kullanıcı, tarih, uyarı) txt'e yazılır.
- Filename: `cleantenant-recovery-codes-{yyyyMMdd-HHmm}.txt`.

### Mimari Karar: MainLayout InteractiveServer
- `@rendermode InteractiveServer` eklendi. Drawer toggle + dark toggle + cascading state artık çalışır.
- Cascade etkisi: tüm sayfalar (zaten `@rendermode InteractiveServer` taşıyanlar + diğerleri) MainLayout altında interactive olur. EmptyLayout kullanan auth sayfaları (Login/2FA Challenge/Pre-auth) etkilenmez — onlar statik SSR + form post pattern'ı.

### Eklenen Dosyalar (3 yeni)
- [wwwroot/js/cleantenant.js](../../../src/Presentation/CleanTenant.ManagementApp/wwwroot/js/cleantenant.js) — `otpInput`, `otpForm`, `downloadTextFile` JS modülleri. App.razor'da global yüklenir.
- [Components/Shared/OtpCodeInput.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/Shared/OtpCodeInput.razor) — Reusable bileşen (InteractiveServer için), `IAsyncDisposable` ile JS cleanup.

### Güncellenen Dosyalar
- [Components/App.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/App.razor) — `cleantenant.js` script tag eklendi.
- [Components/_Imports.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/_Imports.razor) — `Components.Shared` using.
- [Pages/Login.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/Pages/Login.razor) — başlık + iki paragraf değişikliği.
- [Pages/TwoFactorChallenge.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/Pages/TwoFactorChallenge.razor) — 6 kutucuk + recovery toggle + JS bind (hidden `code` field'ı submit'te dolar).
- [Pages/TwoFactorEnrollmentPreAuth.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/Pages/TwoFactorEnrollmentPreAuth.razor) — `OtpCodeInput` + recovery codes TXT indir butonu.
- [Pages/Settings/TwoFactorEnrollment.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/Pages/Settings/TwoFactorEnrollment.razor) — `OtpCodeInput` + TXT indir butonu.
- [Components/Layout/MainLayout.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/Layout/MainLayout.razor) — `@rendermode InteractiveServer`.
- [tests/.../bUnitTests/Components/LoginTests.cs](../../../tests/CleanTenant.ManagementApp.bUnitTests/Components/LoginTests.cs) — "Yönetim Paneline Giriş" assertion'ı yeni başlığa (Toplu Yapı Yönetimi) güncellendi.

### Doğrulama
- ✓ `dotnet build CleanTenant.slnx` — 0 uyarı / 0 hata.
- ✓ `dotnet test CleanTenant.slnx --no-build` — **196 test başarılı** (Sürekli 196).

### Çözülmemiş Konular (Sonraki Adım)
- **AUTH-002 raporu (kullanıcı):** Seed admin parolası eşleşmiyor — `.env.development`'taki `SEED_ADMIN_PASSWORD` ile login formuna yazılan parola aynı olmalı. Identity lockout (5 yanlış sonrası 15dk) ihtimali için `aspnet_users.lockout_end` kontrolü.
- **NavMenu disabled linkler** (Faz 1.4/1.5/1.6 etiketli): Tıklanmaz olmaları doğru davranış — sayfalar henüz yok.

### Sonraki Adım
**v0.2.3.b — Companies CRUD** (sıra geri döndü). UX akışı stabil, asıl iş ekranlarına geçilir.

---

## v0.2.2.a — 2026-05-18 — Pre-auth 2FA Enrollment Akışı

### Sorun
- Memory `rules_identity` kuralı: System scope kullanıcıları için 2FA enrollment **zorunlu**.
- v0.2.2 sonrası System user ilk login'de `AUTH-2FA-ENROLLMENT-REQUIRED` error fırlatılıyor, enrollment'a köprü yok — kullanıcı `/login`'de tıkanır.
- v0.2.2 CHANGELOG'da bu zaten "Açık Konular → Faz 1.X+" olarak ertelenmişti; dev admin testi sırasında zorunlu hale geldi (kullanıcı raporu).

### Mimari Karar: Pre-auth Enrollment Challenge (Redis, 10dk TTL)
- Yeni `PreAuthEnrollmentChallenge` entity: token + UserId + Email + ContextId + Persona + IpAddress + UserAgent + IssuedAt + VerifiedAt.
- Login akışı System scope + 2FA disabled tespit ederse → challenge oluşturulup Redis'e yazılır + token istemciye döner.
- Pre-auth state: token tek başına yetki aracı (kullanıcı henüz authenticated değil, cookie/JWT yok).
- Akış: Start (QR + secret) → Complete (kod doğrula + 2FA enable + recovery codes) → Finalize (cookie set + TokenPair).
- VerifiedAt set olmadan finalize reddedilir (atlatma engeli).

### Mimari Karar: LoginStatus.EnrollmentRequired
- `LoginResult`'a 3. discriminator değer: `Success / TwoFactorRequired / EnrollmentRequired`.
- Yeni `PreAuthEnrollmentChallengeResponse` record (token + ExpiresAt + Email).
- Eski `Error.Failure("AUTH-2FA-ENROLLMENT-REQUIRED", ...)` → artık `Result<LoginResult>.Success(LoginStatus.EnrollmentRequired, ...)` (akış başarılı, istemci yeni adıma yönlendirilir).

### Eklenen Dosyalar (12 yeni dosya)

**Application:**
- [PreAuthEnrollmentChallenge.cs](../../../src/Core/CleanTenant.Application/Common/Auth/PreAuthEnrollmentChallenge.cs)
- [IPreAuthEnrollmentStore.cs](../../../src/Core/CleanTenant.Application/Common/Auth/IPreAuthEnrollmentStore.cs)
- `Features/Auth/TwoFactor/PreAuthEnrollment/`:
  - [StartPreAuthEnrollmentQuery.cs](../../../src/Core/CleanTenant.Application/Features/Auth/TwoFactor/PreAuthEnrollment/StartPreAuthEnrollmentQuery.cs) + Handler
  - [CompletePreAuthEnrollmentCommand.cs](../../../src/Core/CleanTenant.Application/Features/Auth/TwoFactor/PreAuthEnrollment/CompletePreAuthEnrollmentCommand.cs) + Handler
  - [FinalizePreAuthEnrollmentCommand.cs](../../../src/Core/CleanTenant.Application/Features/Auth/TwoFactor/PreAuthEnrollment/FinalizePreAuthEnrollmentCommand.cs) + Handler

**Infrastructure:**
- [RedisPreAuthEnrollmentStore.cs](../../../src/Infrastructure/CleanTenant.Infrastructure.Caching/TwoFactor/RedisPreAuthEnrollmentStore.cs) — `ct:2fa:preauth-enroll:{token}` key pattern, JSON serialize, 10dk TTL, UpdateAsync kalan TTL'yi korur.

**Presentation:**
- [TwoFactorEnrollmentPreAuth.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/Pages/TwoFactorEnrollmentPreAuth.razor) — InteractiveServer + EmptyLayout + AllowAnonymous. State machine: Loading → QrCode → RecoveryCodes; finalize için HTML form post (cookie set HttpContext gerektirir).

### Güncellenen Dosyalar
- [LoginResult.cs](../../../src/Core/CleanTenant.Application/Common/Auth/LoginResult.cs) — `LoginStatus.EnrollmentRequired` + `PreAuthEnrollmentChallengeResponse` record + `LoginResult.EnrollmentChallenge` opsiyonel alan (geriye uyumlu).
- [LoginCommandHandler.cs](../../../src/Core/CleanTenant.Application/Features/Auth/Login/LoginCommandHandler.cs) — System scope + 2FA disabled tespitinde `IssueEnrollmentChallengeAsync` çağrılır; eski error fırlatılmaz.
- [Caching DependencyInjection.cs](../../../src/Infrastructure/CleanTenant.Infrastructure.Caching/DependencyInjection.cs) — `IPreAuthEnrollmentStore` scoped kayıt.
- [WebApi TwoFactorEndpoints.cs](../../../src/Presentation/CleanTenant.WebApi/Endpoints/TwoFactorEndpoints.cs) — 3 yeni anonim endpoint: `/api/v1/auth/2fa/enroll-pre-auth/{start|complete|finalize}` (mobile + integration test için).
- [ManagementApp AuthEndpoints.cs](../../../src/Presentation/CleanTenant.ManagementApp/Auth/AuthEndpoints.cs) — `SignInAsync` `EnrollmentRequired` durumunda `/2fa/enroll-pre-auth?token=...` redirect; yeni `/auth/2fa/enroll-pre-auth/finalize` form-post endpoint (cookie set + redirect /).
- [Login.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/Pages/Login.razor) — eski `AUTH-2FA-ENROLLMENT-REQUIRED` mesajı yeniden yazıldı (artık normal akışta tetiklenmez, edge case fallback); yeni `AUTH-2FA-ENROLL-CHALLENGE-NOT-FOUND` ve `AUTH-2FA-ENROLL-NOT-VERIFIED` mesajları eklendi.

### Yeni Error Code'ları
- `AUTH-2FA-ENROLL-CHALLENGE-NOT-FOUND` (401) — Token bulunamadı veya süresi doldu.
- `AUTH-2FA-ENROLL-NOT-VERIFIED` (403) — Finalize çağrıldı ama Complete adımı geçilmedi.
- `AUTH-2FA-NOT-ACTIVATED` (422) — Defansif: Complete OK ama user.TwoFactorEnabled hâlâ false.

### Test Eklemeleri (6 yeni integration test)

[PreAuthEnrollmentTests.cs](../../../tests/CleanTenant.WebApi.IntegrationTests/TwoFactor/PreAuthEnrollmentTests.cs):
1. Login_System_user_2FA_disabled_EnrollmentRequired_donmeli
2. Start_secret_ve_otpauth_uri_donmeli
3. Bilinmeyen_challenge_token_start_da_401_donmeli
4. Yanlis_kod_complete_da_401_donmeli
5. Dogrulanmamis_challenge_finalize_403_donmeli
6. Tam_akis_start_complete_finalize_token_donmeli — Start → manuel RFC 6238 TOTP → Complete → Finalize → TokenPair + ikinci finalize 401 (replay engeli) + user.TwoFactorEnabled=true doğrulama.

**RFC 6238 TOTP yardımcısı:** ASP.NET Identity'nin `AuthenticatorTokenProvider.GenerateAsync` boş döndüğü için (kod authenticator app'ten gelir) test'te manuel TOTP hesaplama (Base32 decode + HMAC-SHA1 + counter). `#pragma warning disable CA5350` — RFC 6238 standartı HMAC-SHA1 zorunlu kılar.

### Doğrulama
- ✓ `dotnet build CleanTenant.slnx` — 0 uyarı / 0 hata.
- ✓ `dotnet test CleanTenant.slnx --no-build` — **196 test başarılı** (190 + 6 yeni).
  - 17 Application unit + 75 Domain unit + 31 Infrastructure integration + **40 WebApi integration** (34 + 6 yeni) + 33 ManagementApp bUnit.

### Sonraki Adım
**v0.2.3.b — Companies CRUD:** 5 handler + WebApi endpoint'leri + ManagementApp Companies UI. Auth akışı artık uçtan uca tamam — System dev admin login + enrollment + dashboard erişimi mümkün.

---

## v0.2.3.a — 2026-05-17 — Main DbContext Altyapısı + Company Entity

### Kapsam Ayrımı (v0.2.3 → 0.2.3.a + 0.2.3.b)
- v0.2.3 başlangıçta tek alt-faz planlanmıştı; kullanıcı onayıyla **iki parçaya** ayrıldı.
- **v0.2.3.a (bu kayıt):** Main DB infrastructure — entity, DbContext, EF configuration, migration, DI extension, test fixture genişletme, davranış testleri. CRUD yok.
- **v0.2.3.b (sonraki):** CreateCompany / UpdateCompany / DeleteCompany / GetCompanies / GetCompanyByUrlCode CQRS handler'ları + validator'lar + WebApi endpoint'leri + ManagementApp Companies sayfaları.

### Mimari Karar: Main DB — Tenant İş Varlıkları İçin Ayrı DbContext
- Catalog (Identity + Tenant registry) tek DbContext'ti; Main DB iş verileri için ayrı kayıt.
- 4-DB hibrit mimari netleşti: Catalog + Audit + Log (mevcut) + **Main** (yeni).
- Shared-mode default: tüm tenant'lar tek Main DB'yi paylaşır; dedicated DB tenant'ları için runtime connection resolver Faz 1.X+'a ertelendi.

### Mimari Karar: Global Query Filter via Reflection
- `MainDbContext.OnModelCreating` her `ITenantScoped` entity'sine reflection ile `HasQueryFilter(e => e.TenantId == _tenantContext.TenantId)` uygular.
- Cross-tenant erişim (System scope) bilinçli olarak `IgnoreQueryFilters()` ile yapılır.
- `_tenantContext.TenantId` null ise (henüz seçilmemiş context) filter `null == null` üretir; PostgreSQL'de bu false → boş sonuç döner. Yani System scope'ta `IgnoreQueryFilters` zorunlu.

### Mimari Karar: Company Minimal Skeleton
- Kullanıcı kararı: "Minimal başla — UrlCode + TenantId + Name + LegalName? + Vkn? + Email? + Phone? + Status."
- Building/Unit/Invoice ileride; Company her ikisinin de parent'ı olacak.
- VKN formatı DB CHECK constraint ile dayatılır (`^[1-9][0-9]{9}$`); Application validator (v0.2.3.b) ek FluentValidation katmanı ekler.
- Name `citext` (case-insensitive) + tenant içinde unique (`is_deleted = false` filtreli partial unique index).

### Eklenen Dosyalar (10 yeni dosya)

**Domain:**
- [CompanyStatus.cs](../../../src/Core/CleanTenant.Domain/Tenant/Companies/CompanyStatus.cs) — `Active=1, Suspended=2, Closed=3`.
- [Company.cs](../../../src/Core/CleanTenant.Domain/Tenant/Companies/Company.cs) — `BaseEntity + IAggregateRoot + IHasUrlCode + ITenantScoped`.

**Application:**
- [IMainDbContext.cs](../../../src/Core/CleanTenant.Application/Common/Persistence/IMainDbContext.cs) — `DbSet<Company> Companies` + `SaveChangesAsync`.

**Infrastructure.Persistence:**
- [Main/MainDbContext.cs](../../../src/Infrastructure/CleanTenant.Infrastructure.Persistence/Main/MainDbContext.cs) — sealed DbContext + reflection global query filter.
- [Main/Configurations/CompanyConfiguration.cs](../../../src/Infrastructure/CleanTenant.Infrastructure.Persistence/Main/Configurations/CompanyConfiguration.cs) — IEntityTypeConfiguration: citext Name + `ck_company_vkn_format` CHECK + xmin RowVersion.
- [Main/MainDbContextDesignTimeFactory.cs](../../../src/Infrastructure/CleanTenant.Infrastructure.Persistence/Main/MainDbContextDesignTimeFactory.cs) — internal IDesignTimeDbContextFactory (EF CLI migration için, ScopeLevel.None tenant context'i ile).
- `Main/Migrations/20260517192249_InitialMain.cs` + `Designer` + `MainDbContextModelSnapshot.cs` — EF Core CLI tarafından üretildi.
- [DependencyInjection.cs](../../../src/Infrastructure/CleanTenant.Infrastructure.Persistence/DependencyInjection.cs) — yeni `AddMainPersistence(connectionString, auditConnectionString?)` extension method. Interceptor zinciri Catalog ile aynı: Auditing + UrlCodeGenerating + (opsiyonel) FullAudit.

### Program.cs Bağlamaları
- **WebApi** [ServiceCollectionExtensions.cs](../../../src/Presentation/CleanTenant.WebApi/Configuration/ServiceCollectionExtensions.cs): `ConnectionStrings:Main` opsiyonel okunur; varsa `AddMainPersistence` çağrılır.
- **ManagementApp** [Program.cs](../../../src/Presentation/CleanTenant.ManagementApp/Program.cs): aynı pattern; v0.2.2'deki in-process IMediator zinciri Main DB'yi de görür.
- `.env.development.example` zaten `ConnectionStrings__Main` taşıyordu (placeholder); ek değişiklik gerekmedi.

### Test Eklemeleri (11 yeni test)

**Domain.UnitTests (5 yeni):**
- [CompanyTests.cs](../../../tests/CleanTenant.Domain.UnitTests/Tenant/Companies/CompanyTests.cs):
  - Default constructor empty string + `Guid.Empty` + default status.
  - `ITenantScoped + IAggregateRoot + IHasUrlCode + BaseEntity` marker compliance.
  - `CompanyStatus` enum değerleri stable (1/2/3 numerik) — Theory ile 3 InlineData.

**Infrastructure.IntegrationTests (6 yeni):**
- [MainDbContextTests.cs](../../../tests/CleanTenant.Infrastructure.IntegrationTests/Main/MainDbContextTests.cs):
  - Company eklendiğinde UrlCode otomatik üretilir (9-char Base58).
  - Global query filter aktif tenant dışındaki kayıtları gizler.
  - `IgnoreQueryFilters()` cross-tenant erişimi geri açar.
  - VKN CHECK constraint geçersiz format için ihlal eder.
  - Aynı tenant içinde aynı Name unique index ile engellenir.
  - Company yaratıldığında Audit DB'ye Create kaydı yazılır.

**Fixture Genişletme:**
- [PostgresFixture.cs](../../../tests/CleanTenant.Infrastructure.IntegrationTests/Fixtures/PostgresFixture.cs): üçüncü DB `cleantenant_main` provision + extension'lar + migration. `TenantContext` mock'u testlerin tenant kimliği set etmesi için public.
- Catalog'un `SystemTenantContext` placeholder kayıtı test container'ında override edilir.

### Doğrulama
- ✓ `dotnet build CleanTenant.slnx` — 0 uyarı / 0 hata.
- ✓ `dotnet test CleanTenant.slnx --no-build` — **190 test başarılı** (Faz 0'ın 146 + ManagementApp bUnit 33 + Domain +5 + Infrastructure +6).
  - 17 Application unit + 75 Domain unit + 31 Infrastructure integration + 34 WebApi integration + 33 ManagementApp bUnit.

### Bilinen Sınırlar
- **MigrationRunner Main desteği yok.** Şu an yalnız Catalog migrate eder; Main migration'ları test container'ında otomatik uygulanır, prod/dev için Faz 1.X'te `--db <Catalog|Main|Audit|Log>` argümanı eklenecek.
- **System scope cross-tenant okuması manuel.** Global filter `null == tenantId` false döndüğü için System scope query'lerin `IgnoreQueryFilters()` çağırması zorunlu — bunu MediatR pipeline (v0.2.3.b) için unutmayalım.
- **Companies CRUD handler yok.** v0.2.3.b'de eklenecek.

### Sonraki Adım
**v0.2.3.b — Companies CRUD:** 5 handler (Create/Update/Delete/GetList/GetByUrlCode) + FluentValidation'lar + WebApi `CompanyEndpoints.cs` (5 route) + ManagementApp Companies UI (liste + form). Tenant onboarding wizard'ında (v0.2.4) ilk şirket yaratma akışı bu handler'ları çağıracak.

---

## v0.2.2 — 2026-05-17 — Auth Ekranları (Login + 2FA Challenge + 2FA Enrollment)

### Mimari Karar: In-process MediatR
- Kullanıcı kararı: "Backend'e nasıl çağrı yapılsın? → In-process IMediator."
- ManagementApp WebApi'ye HTTP çağrısı yapmaz. Faz 0 backend handler'ları doğrudan `IMediator.Send` ile çalıştırılır — aynı pipeline (Authorization → Validation → Logging), aynı audit, aynı Redis session.
- ManagementApp.Program.cs WebApi'nin `AddCleanTenantApi` adımlarını **inline** olarak tekrarlar. Faz 1.2'de paylaşılan composition extension'a refactor edilir.

### Mimari Karar: Cookie Auth Scheme
- Default scheme: `CookieAuthenticationDefaults.AuthenticationScheme`.
- Cookie ayarları: `HttpOnly + Secure + SameSite=Strict`, `ExpireTimeSpan=30dk`, `SlidingExpiration=true`.
- Claims: `sid` (session id), `ctx` (context id), `scope` (System/Tenant/Company/Unit).
- `AnonymousAuthenticationStateProvider` → `JwtCookieAuthenticationStateProvider` swap (HttpContext.User cascading state).
- "Beni hatırla" → `IsPersistent=true` + 7 gün `ExpiresUtc`.

### Mimari Karar: Form Post Pattern (Static SSR)
- Login + 2FA Challenge sayfaları **static SSR** — `<form method="POST">` ile `/auth/sign-in` ve `/auth/2fa/verify` endpoint'lerine post.
- Endpoint handler'ları `HttpContext.SignInAsync` ile cookie set'ler — Blazor SignalR circuit problemi yok.
- 2FA Enrollment **InteractiveServer** (QR kod render + state'li akış).

### Eklenen Dosyalar
**Auth/:**
- `AuthEndpoints.cs` — MapPost handlers: `/auth/sign-in`, `/auth/sign-out`, `/auth/2fa/verify`, `/auth/2fa/send-code`.
- `JwtCookieAuthenticationStateProvider.cs` — HttpContext.User → cascading state.

**Components/Layout/:**
- `EmptyLayout.razor` — Login/2FA sayfaları için (AppBar/Drawer'sız minimal).
- `MainLayout.razor` güncel — Logout `/auth/sign-out` + "2FA Yönetimi" link.
- `Routes.razor` güncel — `AuthorizeRouteView` ile kimlik doğrulanmamış kullanıcı `/login`'e yönlendirilir.

**Components/Pages/:**
- `Login.razor` (`/login`) — Static SSR. Email/TCKN/VKN/Telefon tek input + şifre + persona hidden Management + "Beni hatırla" + error display.
- `TwoFactorChallenge.razor` (`/2fa/challenge`) — Method seçici + kod input + Email kod gönder.
- `Settings/TwoFactorEnrollment.razor` (`/settings/2fa/enroll`) — 3 adım state machine (Start → QrCode → RecoveryCodes). QR kod QRCoder ile base64 PNG.
- `NotFound.razor` + `Error.razor` MudBlazor görsel + `EmptyLayout` + `AllowAnonymous`.

**Program.cs:**
- JWT_* env mapping + AddCleanTenantSerilog + backend DI + Cookie auth + MapAuthEndpoints.

### Eklenen Paketler
- **QRCoder 1.6.0** — TOTP QR kodu (MIT).

### Test Eklemeleri (4 yeni bUnit testi)
- `LoginTests` (3): form action + name'ler + "Beni hatırla" + persona hidden.
- `TwoFactorChallengeTests` (1): Token yoksa error alert.
- SupplyParameterFromQuery testleri NavigationManager pattern'ı gerektiriyor (v0.2.2.b).

### Slnx Güncellemesi
`CleanTenant.ManagementApp.bUnitTests.csproj` Solution dosyasına eklendi (v0.2.1'de eklenmişti ama .slnx'e yazılmamıştı; düzeltildi).

### Doğrulama
- ✓ `dotnet build` — 18 proje / 0 uyarı / 0 hata.
- ✓ `dotnet test` — **179 test başarılı** (146 Faz 0 + 33 ManagementApp bUnit).

### Açık Konular (Faz 1.X)
- Switch-context UI (app bar chip + dropdown) → v0.2.3 veya v0.2.4
- Antiforgery POST-only logout → v0.2.3
- `AddCleanTenantApi` paylaşılan extension → v0.2.3
- Pre-auth 2FA enrollment akışı (System kullanıcılar için) → Faz 1.X+
- SignInAsync sonrası SignalR state refresh — JwtCookieAuthenticationStateProvider.NotifyStateChanged() eklendi ama henüz çağrılmıyor (ForceLoad ile cookie zaten okunuyor).

### Sonraki Adım
**v0.2.3 — Main DbContext + Companies tablosu:** Faz 0'da Catalog vardı; tenant business data için Main DB devreye alınır. İlk iş varlığı: `Company` (Şirket).

---

## v0.2.1 — 2026-05-17 — ManagementApp Shell (AdminLTE+ + 4 Tema)

### Mimari Karar: Layout — AdminLTE+ Pateni MudBlazor ile
- Kullanıcı kararı: "Layout AdminLTE+ son sürümüne benzesin." MudBlazor bileşenleriyle yeniden inşa.
- `MudAppBar` üst — logo (ikon + "CleanTenant Yönetim") + dark toggle + tema link + bildirim + kullanıcı menüsü.
- `MudDrawer` sol — Mini variant (collapsed) + OpenMiniOnHover. Gruplandırılmış nested menu: Dashboard / Yönetim / Gözetim / Ayarlar.
- `MudBreadcrumbs` her sayfa içeriğinin üstünde.
- `MudMainContent` + Footer ("© 2026 CleanTenant · v0.2.1 (Faz 1 başlangıç) · Yönetim").

### Mimari Karar: 4 Tema Preset + Runtime Seçici
- Kullanıcı kararı: "Hepsi tema olarak kullanılabilsin ve seçilebilsin." 4 preset:
  - **Kurumsal Mavi** (default) — `#1976D2` / `#FF9800`
  - **Teşkilatsal Yeşil** — `#2E7D32` / `#607D8B`
  - **MudBlazor Mor** — `#594AE2` / `#FF4081`
  - **Koyu Kurumsal** — `#263238` / `#00BCD4`
- Her preset light + dark varyantı taşır.
- `Ayarlar > Tema` sayfası 4 preview kart sunar; kullanıcı tıkladığında runtime'da değişir, tercih `localStorage`'da kalıcı.
- Default: Kurumsal Mavi · Light.

### Mimari Karar: Auth State — JWT Cookie (HttpOnly + Secure + SameSite=Strict)
- Memory'deki `rules_identity` ile uyumlu. SignalR koparsa state korunur (cookie tarayıcıda).
- v0.2.1'de henüz auth ekranı yok → `AnonymousAuthenticationStateProvider` stub. v0.2.2'de gerçek login akışıyla aktive edilir.

### Mimari Karar: Sürüm Numaralandırma — v0.2.x
- Faz 0 v0.1.x kullandı; Faz 1 v0.2.x. Semver v1.0 stable/production-ready için saklı.

### Eklenen Paketler (Directory.Packages.props)
- **MudBlazor 8.5.1** — UI bileşen kütüphanesi (MIT, free).
- **Blazored.LocalStorage 4.5.0** — tema tercihi kalıcılığı.
- **bunit 1.40.0** — Blazor component test framework'u (MIT, free).

### Eklenen ManagementApp Dosyaları (~16 yeni dosya)
**Themes/:**
- [ThemePresetId.cs](../../../src/Presentation/CleanTenant.ManagementApp/Themes/ThemePresetId.cs) — 4 preset enum.
- [CleanTenantThemes.cs](../../../src/Presentation/CleanTenant.ManagementApp/Themes/CleanTenantThemes.cs) — 4 `MudTheme` palette + light/dark + ortak typography/layout.

**Services/:**
- [IThemeService.cs](../../../src/Presentation/CleanTenant.ManagementApp/Services/IThemeService.cs) — abstraction (CurrentPreset, IsDarkMode, ThemeChanged event, Set/Toggle async).
- [LocalStorageThemeService.cs](../../../src/Presentation/CleanTenant.ManagementApp/Services/LocalStorageThemeService.cs) — Blazored.LocalStorage implementation. Anahtarlar: `cleantenant.theme.preset` + `cleantenant.theme.dark`.

**Auth/:**
- [AnonymousAuthenticationStateProvider.cs](../../../src/Presentation/CleanTenant.ManagementApp/Auth/AnonymousAuthenticationStateProvider.cs) — geçici stub (v0.2.2'de gerçek JWT cookie impl ile değişir).

**Components/Layout/:**
- `MainLayout.razor` — AdminLTE+ pateni: `MudThemeProvider` + `MudLayout` + `MudAppBar` + `MudDrawer` + `MudMainContent` + Footer. Theme service'inden tema dinamik uygulanır.
- [NavMenu.razor](../../../src/Presentation/CleanTenant.ManagementApp/Components/Layout/NavMenu.razor) — drawer içeriği. Faz 1.1'de yalnız Dashboard + Ayarlar aktif; diğerleri `Disabled` + `MudChip` "Faz 1.4/1.5/1.6" badge'leriyle ön gösterim.

**Components/Pages/:**
- `Home.razor` (`/`) — Dashboard placeholder. 4 KPI kartı + Faz 1 yol haritası listesi.
- `Settings/ThemeSettings.razor` (`/settings/theme`) — 4 preset preview kart + dark mode switch.
- `Settings/LanguageSettings.razor` (`/settings/language`) — placeholder, Faz 1.7'de aktive olacak çoklu dil radio group.
- `About.razor` (`/about`) — uygulama sürümü + Faz 0 çıktıları özet.

**Program.cs:**
- `AddMudServices()` + `AddBlazoredLocalStorage()` + `AddScoped<IThemeService, LocalStorageThemeService>()`.
- `AddAuthorizationCore()` + `AddCascadingAuthenticationState()` + `AnonymousAuthenticationStateProvider`.
- `AddLocalization(opts => opts.ResourcesPath = "Localization/Resources")`.

**App.razor:**
- MudBlazor CSS + JS bağlama (Roboto + Material Icons fontları).
- `<html lang="tr">` (Türkçe varsayılan).

**_Imports.razor:**
- `@using MudBlazor`, `@using CleanTenant.ManagementApp.Services`, `@using CleanTenant.ManagementApp.Themes`.

**Localization/:**
- [SharedResources.cs](../../../src/Presentation/CleanTenant.ManagementApp/Localization/Resources/SharedResources.cs) — marker tip.
- `SharedResources.tr.resx` — ~17 anahtar Türkçe (örnek scaffold). EN/AR/RU/DE Faz 1.7'de eklenir.

### Test Projesi (yeni)
**`tests/CleanTenant.ManagementApp.bUnitTests/`** — bUnit + xUnit + NSubstitute + FluentAssertions.

- `Themes/CleanTenantThemesTests.cs` (10 test): 4 preset Resolve, Primary hex doğrulama, DisplayName Türkçe, default fallback.
- `Services/LocalStorageThemeServiceTests.cs` (5 test): default, storage'dan yükleme, boş storage default, set/toggle event.
- `Components/MudTestContextBase.cs` — ortak bUnit setup (`AddMudServices` + `JSRuntimeMode.Loose` + theme/auth mock).
- `Components/HomeTests.cs` (3 test): Dashboard başlığı, yol haritası, 4 KPI kart.
- `Components/AboutTests.cs` (2 test): başlık + Faz 0 özet.
- `Components/NavMenuTests.cs` (4 test): drawer linkleri + grup başlıkları + Faz badge'leri + Ayarlar alt linkleri.
- `Components/ThemeSettingsTests.cs` (3 test): 4 preset kart + başlık + dark switch.

### Doğrulama
- ✓ `dotnet build` — 18 proje (yeni `ManagementApp.bUnitTests`) / 0 uyarı / 0 hata.
- ✓ `dotnet test` — **175 test başarılı** (Faz 0'ın 146'sı + 29 yeni bUnit).
  - 17 Application unit + 70 Domain unit + 25 Infrastructure integration + 34 WebApi integration + **29 ManagementApp bUnit**.

### Açık Konular (Sonraki Alt Fazlar)
- **v0.2.2:** Auth ekranları (login, 2FA challenge, recovery code, switch-context).
- **v0.2.3:** Main DbContext + ilk Companies tablosu.
- **v0.2.4:** Tenant onboarding wizard (3 adım: Tenant + Admin + 2FA enrollment).
- **v0.2.5:** Rol-Permission yönetim ekranı (hibrit: matris üst + detay alt) + `[RequirePermission]` 30+ handler yerleştirme.
- **v0.2.6:** Audit Explorer (4 filtre: tarih + email + action + entityType) + Log viewer. Scope-aware: TenantScope → kendi tenant; SystemScope → cross-tenant.
- **v0.2.7:** PortalApp aynı shell + Unit kullanıcı ekranları.

### Sonraki Adım
**v0.2.2 — Auth Ekranları:** Backend Faz 0'da hazır; UI tarafında login formu (email/TCKN/telefon/VKN tek input + persona seçici), 2FA challenge ekranı (TOTP/Email/SMS/RecoveryCode seçici), 2FA enrollment ekranı, switch-context UI'ı. JWT cookie auth state provider gerçek implementasyon.

---

*Faz 1 başlangıcı — UI üzerine inşa edilen tüm alt-fazlar bu shell'i kullanır.*
