# Faz v0.2 — CHANGELOG (Faz 1)

Bu dosya, Faz 1 (UI başlangıç + ManagementApp) kapsamında yapılan tüm alt-faz değişikliklerini versiyon damgalı kayıt altına alır. Son giriş en üsttedir.

**Faz 0 kapanış belgesi:** [../v0.1/v0.1-FINAL-ARCHITECTURE-MAP.md](../v0.1/v0.1-FINAL-ARCHITECTURE-MAP.md)
**Faz 0 CHANGELOG:** [../v0.1/CHANGELOG.md](../v0.1/CHANGELOG.md)

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
