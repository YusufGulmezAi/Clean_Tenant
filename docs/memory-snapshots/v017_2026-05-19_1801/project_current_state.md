---
name: Proje Mevcut Durumu — v0.2.10 Lokalizasyon Devam Ediyor
description: v0.2.10 e.2 alt-fazı tamamlandı (Layout/NavMenu/DataTable lokalize); sıradaki e.3 Form bileşenleri
type: project
originSessionId: 96f774c4-d60a-46bd-a954-eb6e63f04679
---
Son commit'lenen tag: **v0.2.7** (`7bb9b7b`); ardından **uncommitted v0.2.10 lokalizasyon işi** sürmekte (commit yok, ~50 dosya stage'siz).

**Why:** Lokalizasyon a→e.2 sırasıyla yürütülüyor; her alt-faz işin doğal kesim noktası. Faz tamamlanıp test edildikten sonra tek commit (`v0.2.10`) atılacak.

**How to apply:** Yeni oturumda `git status --short` ile uncommitted işin kapsamını gör; `LocalizationCatalog.cs` mevcut anahtar envanteri için master kaynaktır.

---

## v0.2.10 Lokalizasyon — Alt-faz Takibi

| Alt-faz | Kapsam | Durum |
|---------|--------|-------|
| a | `LocalizedResource` entity + `User.PreferredCulture` + migration | ✅ |
| b | `LocalizationStore` (singleton) + `DbStringLocalizer` + fallback (current→en-US→tr-TR→[Key]) | ✅ |
| c | `LocalizationSeeder` + `LocalizationCatalog` (~390 anahtar) | ✅ |
| d | AppBar dropdown + `/auth/change-culture` + login User.PreferredCulture | ✅ |
| e.1 | Catalog 391 anahtar (Permission/Module/Common/Nav/Layout/Form/Audit/Banks/LookUp/BuildingSchema/Auth) | ✅ |
| **e.2** | **Layout: NavMenu, MainLayout, DataTable (+ DataTable code-behind)** | **✅ (2026-05-19)** |
| e.3 | Form bileşenleri (TenantForm, CompanyForm, RoleForm, PermissionPicker) | ⏸ |
| e.4 | System Area (Tenants, Companies, Roles, Banks, Audit, LookUp) | ⏸ |
| e.5 | Tenant Area (Companies, Roles, BuildingSchema, Settings) | ⏸ |
| e.6 | Auth (Login, 2FA) + Home/About/Settings + NotFound | ⏸ |
| f | RTL (Arabic) — `body[dir="rtl"]` + MudBlazor `RightToLeftProvider` | ⏸ |
| g | `/system/localization` admin sayfası (`System.Localization.Manage` izniyle) | ⏸ |

---

## e.2 — Kapsam (2026-05-19 tamamlandı)

Değişen dosyalar:
- `LocalizationCatalog.cs` — 4 yeni anahtar: `DataTable.SearchPlaceholder`, `DataTable.DefaultTitle`, `DataTable.Export.NoColumns.Excel`, `DataTable.Export.NoColumns.Pdf`
- `Components/Layout/NavMenu.razor` — `Matches()` filter localize değer üzerinde çalışacak şekilde refactor
- `Components/Layout/MainLayout.razor` — AppBar/user menu/footer/error-ui
- `Components/Shared/DataTable.razor` + `.razor.cs` — `IStringLocalizer` inject; `SearchPlaceholder` nullable yapıldı (null → `DataTable.SearchPlaceholder` fallback)

Kararlar:
- Faz chip'leri ("Faz 1.6" vb.) raw bırakıldı (geliştirici işareti, kullanıcı görmesi kasıtlı)
- Footer'da app version raw, "Yönetim" suffix `NavMenu.Tenant` anahtarı üzerinden
- Razor inject pattern: `@inject IStringLocalizer Loc` (DI'da non-generic `IStringLocalizer` register edilmiş)
- Tuple başlıklarında `Loc["Key"].Value` (LocalizedString → string explicit)

---

## Konsept Bilgileri (Lokalizasyon Mimari Özeti)

- **Key naming:** Dot-notation, düz hiyerarşi (`User.Read.Description`, `Roles.New.SubmitButton`, `NavMenu.LookUpTables`)
- **Fallback zinciri:** current culture → en-US → tr-TR → `[Key]` raw (dev uyarısı)
- **Aktif diller:** TR (default), EN. AR/RU/DE iskelet sonra
- **EN bootstrap:** Catalog'da explicit EN yoksa `"[EN] {tr}"` machine-stub + `IsMachineTranslated=true`
- **DI:** `services.AddScoped<IStringLocalizer, DbStringLocalizer>()` — non-generic, doğrudan `@inject IStringLocalizer Loc`
- **NavMenu Matches() pattern:** Localize edilmiş başlık üzerinde Contains; kullanıcı aktif UI dilinde aratabilir
- **LocalizationCatalog güncellendiğinde:** App restart sonrası `LocalizationSeeder` yeni anahtarları DB'ye yazar

---

## Sıradaki: e.3 — Form Bileşenleri

Lokalize edilecek dosyalar:
- `Components/Shared/TenantForm.razor` (+ varsa code-behind)
- `Components/Shared/CompanyForm.razor` + `CompanyForm.razor.cs`
- `Components/Shared/RoleForm.razor` + `RoleForm.razor.cs`
- `Components/Shared/PermissionPicker*.razor`

Hazır anahtarlar Catalog'da: `TenantForm.*`, `CompanyForm.*`, `Roles.Form.*`, `Permissions.Picker.*`.

---

## v0.3 Planı (Lokalizasyon Bittikten Sonra)

Plan dosyası: `C:\Users\yusuf\.claude\plans\soft-doodling-sutton.md` (henüz v0.3 plan yazılmadı)

Kapsam:
- Domain: `Unit`, `Resident`, `UnitResident` entity'leri (Main DB)
- Application: Unit/Resident CRUD handler'ları
- WebApi: UnitEndpoints, ResidentEndpoints
- ManagementApp: CompanyArea Unit listesi, Resident yönetimi
- PortalApp: Sakin self-servis (kendi birim bilgisi)
