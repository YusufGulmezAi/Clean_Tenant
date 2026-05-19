---
name: Lokalizasyon Kuralları
description: DB-tabanlı çoklu dil (TR/EN/AR/RU/DE), LocalizedResource entity, LocalizationStore singleton, DbStringLocalizer, fallback chain, RTL (Arabic), culture cookie + User.PreferredCulture, ~480 anahtar (v0.2.10)
type: project
originSessionId: 61d8f930-a87d-4a38-a2cf-e582e02a5421
---
## Desteklenen Diller (5)

| Culture | Dil | Not |
|---|---|---|
| `tr-TR` | Türkçe | **Varsayılan + birincil**; her zaman tam dolu |
| `en-US` | English | İkinci fallback; explicit yoksa `"[EN] {tr}"` machine-stub seed |
| `ar-SA` | العربية | **RTL** — MudRTLProvider + body/html `dir="rtl"` |
| `ru-RU` | Русский | Machine-stub seed |
| `de-DE` | Deutsch | Machine-stub seed |

BCP-47 culture kodu kullanılır. `LocalizedResource.Culture` 16 karakter sınırı (yeterli marj).

## Veri Modeli

### `LocalizedResource` entity (Catalog DB, `localized_resources` tablosu)

| Alan | Tip | Not |
|---|---|---|
| `Id` | `Guid` (UUID v7) | `BaseEntity` |
| `Key` | `string(256)` | Dot-notation, PascalCase — örn. `User.Read.Description`, `Roles.New.SubmitButton`, `NavMenu.LookUpTables` |
| `Culture` | `string(16)` | BCP-47 |
| `Value` | `string(4000)` | Çeviri değeri; parametreli string'ler `string.Format` ile (örn. `"Hoşgeldin, {0}."`) |
| `IsMachineTranslated` | `bool` | True → admin revize edene kadar UI'da "revizyon gerekli" uyarısı |
| + `BaseEntity` | — | CreatedAt/By, UpdatedAt/By, IsDeleted, DeletedAt/By, RowVersion (xmin) |

### Index'ler
- **Unique:** `(Key, Culture)` — `ix_localized_resource_key_culture`
- **Lookup:** `(Culture)` — `ix_localized_resource_culture`
- **Global query filter:** `IsDeleted = false`

### Migration
- `20260519140924_AddLocalizationSchema` (v0.2.10.a)

## Çözüm Pipeline'ı

### `LocalizationStore` (singleton, in-memory)
- **Startup'ta** tüm aktif `LocalizedResource` kayıtlarını DB'den yükler (`ReloadAsync`).
- Yapı: `Dictionary<culture, Dictionary<key, value>>` — sync O(1) lookup.
- `Get(culture, key)` → `string?`; bulunamazsa null (fallback caller tarafında).
- **Admin update sonrası** `/system/localization` (v0.2.10.g) `ReloadAsync` tetikler — store atomik (lock) yenilenir.
- Bellek maliyeti küçük; büyürse lazy per-culture loading'e geçilebilir.

### `DbStringLocalizer` (`IStringLocalizer` impl)
ASP.NET Core'un `IStringLocalizer` arayüzü; component'ler `@inject IStringLocalizer<T> L` ile alır; `L["Key"]` veya `L["Key", arg1]` (parametreli) çağırır.

**Fallback zinciri (v0.2.10):**
1. `CultureInfo.CurrentUICulture.Name` (aktif kültür)
2. `en-US` (varsayılan ortak baz)
3. `tr-TR` (geliştirme dili — daima dolu)
4. `[Key]` raw — dev için "eksik çeviri" uyarısı (`LocalizedString.ResourceNotFound = true`)

`CurrentUICulture`'ı `UseRequestLocalization` middleware request cookie/header'ından set eder.

### Seeding
- `LocalizationCatalog` — ~480 anahtar, modüller halinde gruplanmış:
  - Permission Module/Description (60)
  - Common UI — kaydet/iptal/sil/onayla vb. (24)
  - Navigation Menu (28)
  - Layout — AppBar/Footer/Error (14)
  - DataTable (8)
  - Login + 2FA — Challenge/PreAuth/Enrollment (40+)
  - Tenants/Companies/Roles list+CRUD+form (90+)
  - BuildingSchema (60+)
  - Audit Explorer / Banks / LookUp (50+)
  - Settings + Home + About + NotFound (20+)
  - Page titles (12)
  - Error format strings + tooltip'ler (15)
- `LocalizationSeeder` — idempotent; her seed'de değişmemiş anahtarları atlar, yeni gelenleri ekler.
- **EN bootstrap:** Catalog'da explicit EN yoksa `"[EN] {tr}"` machine-stub + `IsMachineTranslated=true`. Aynı pattern RU/DE için de geçerli.

## Culture Resolution (Request başına)

ASP.NET Core `RequestLocalizationMiddleware` aşağıdaki sıra ile karar verir (default provider'lar):
1. **Query string** (`?culture=...&ui-culture=...`)
2. **`.AspNetCore.Culture` cookie** (`c={culture}|uic={culture}` formatı)
3. **`Accept-Language` header**
4. **Default:** `tr-TR`

Sonuç `CultureInfo.CurrentCulture` ve `CultureInfo.CurrentUICulture`'a set edilir; request scope'unda kalır.

## Culture Değiştirme: Cookie-Driven

### `POST /auth/change-culture` (v0.2.10.d)
- `AuthEndpoints.ChangeCultureFormAsync`
- Form fields: `culture` (zorunlu), `returnUrl` (opsiyonel)
- AllowAnonymous — login öncesi de değiştirilebilir
- İşlem:
  1. `.AspNetCore.Culture` cookie set (`c={culture}|uic={culture}`, MaxAge 365 gün, SameSite=Lax, IsEssential=true)
  2. Giriş yapmışsa (`UserId` claim varsa) `User.PreferredCulture` DB'ye yaz
  3. `returnUrl` veya `/` 'a redirect
- AppBar dropdown bu endpoint'e form-post ile çağrı yapar.

### Login Sonrası Otomatik Culture (v0.2.10.d)
- `SignInAsync` başarılı login sonrası `ApplyUserPreferredCultureAsync` çağırır
- `User.PreferredCulture` DB'den okunur; doluysa cookie'ye yazılır
- Null ise sistem varsayılanı (TR) zaten kullanılır
- **Sonuç:** Her oturumda kullanıcının dili otomatik gelir, sekme bazlı sıfırlanmaz.

## RTL (Arabic) Desteği

### MudBlazor seviyesi
- `MudRTLProvider RightToLeft="@isRtl"` — MudBlazor bileşenlerini mirror'lar
- `isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft`

### DOM seviyesi (custom CSS sayfalar için)
- `<body dir="rtl">` ve `<html dir="rtl">` — JS interop ile set edilir
- `wwwroot/js/cleantenant.js` → `cleantenant.setBodyDirection(dir)`
- Layout component (`MainLayout`) culture değiştikçe çağırır

### Kapsam
- **ManagementApp + PortalApp:** ✅ (v0.2.10.f)
- **MobilApp (MAUI):** ❌ — MAUI native `FlowDirection.RightToLeft` ileride; v0.2.10 kapsamı dışı

### CSS önerisi
- Logical properties tercih edilir: `margin-inline-start` (`margin-left` yerine), `padding-inline-end`, vb. — RTL'de otomatik mirror'lanır.

## Anahtar İsimlendirme Konvansiyonu

**Format:** `<Module>.<Context>.<Item>` — PascalCase dot-notation, max 256 karakter.

Örnekler:
- `Roles.New.SubmitButton`
- `Roles.List.Title`
- `NavMenu.LookUpTables`
- `Permissions.Tenant.Create.Description`
- `Common.Save`, `Common.Cancel`, `Common.Delete`
- `Validation.Email.Invalid`
- `Errors.AUTH-2FA-CHALLENGE-NOT-FOUND`

**Anti-pattern:** camelCase (`errors.user.notFound`), boşluklu key, dinamik concat.

## Error Code'ları ile İlişki

- Error catalog (`AUTH-001`, `VAL-002`, ...) sistem tanımlayıcısıdır (mesaj değil).
- Localization `Errors.<CODE>` → user-facing mesajı kültür başına çözer.
- Validation: aynı pattern → `Validation.<Rule>.<Field>`.

## Formatlama Kuralları

- **Tarih / sayı / para birimi:** Her zaman `CultureInfo` kuralları üzerinden formatlanır — hiçbir hardcoded string olmaz.
- **Para birimi:** Tenant'ın billing currency'sine bağlıdır, kullanıcının UI culture'una değil. Örn. UI English olsa bile billing TRY ise `123,45 ₺` render edilir.
- **Tarih:** `TimeZoneInfo` "Turkey Standard Time" + culture format string'i (`d`, `g`, `F`).

## Admin UI (v0.2.10.g — Sıradaki Alt-Faz)

- `/system/localization` sayfası (henüz commit edilmedi)
- Permission: `System.Localization.Manage`
- Yapacakları:
  - Tüm anahtarların kültür başına değerlerini grid'de göster
  - `IsMachineTranslated=true` olanlar vurgulanır
  - Inline edit + save → DB update + `LocalizationStore.ReloadAsync()`
  - Filter: missing translations per culture; module filter
  - Import/Export (ileride — JSON/Excel)

## Kapsam Notu (Önemli)

- **Faz 0/1 backend lokalize EDİLMEMİŞTİR.** Audit message'ları, log mesajları, exception mesajları İngilizce kalır (developer dili).
- **Yalnız Presentation katmanı kapsamdadır:** SystemArea (12 sayfa), TenantArea (8 sayfa), Auth (Login + 2FA Challenge + PreAuth Enroll + Enroll, 10 sayfa), Home/About/Settings/NotFound, Form/DataTable/PermissionPicker/CompanyDetailTabs bileşenleri.
- Backend lokalizasyonu (örn. multi-tenant tenant-specific override'lar) Faz 2+ için potansiyel kapsam.
