---
name: Localization Rules
description: TR/EN/AR/RU/DE support, DB-based resources, RTL handling for Arabic, locale-aware formatting
type: project
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
## Supported Languages
- **TR** (Türkçe) — default, primary
- **EN** (English)
- **AR** (العربية) — RTL
- **RU** (Русский)
- **DE** (Deutsch)

## Resource Storage
- **DB-based** localization, not `.resx`. Reason: enables runtime edits without redeploy and multi-tenant overrides.
- Table `LocalizationResources(Key, Culture, Value, Module, UpdatedAt)`.
- `Key` is namespaced by module: `errors.user.notFound`, `validation.email.invalid`, `ui.button.save`.
- Cache in Redis with culture-scoped keys; invalidate on update from admin UI.
- Fallback chain: requested culture → `TR` (default) → key literal (with warning log).

## Error Codes ↔ Localization
- Error catalog (`USR-001`, etc.) is the system identifier.
- Localization resolves `errors.<code>` → user-facing message per culture.
- Validation messages: same pattern, `validation.<rule>.<field>`.

## Culture Resolution per Request
- Order: `?lang=` query → `Accept-Language` header → user profile preference → tenant default → `TR`.
- Stored in `CultureInfo.CurrentUICulture` for the request scope.

## RTL (Arabic)
- ManagementApp + PortalApp: `<html dir="rtl" lang="ar">` when culture is `ar`.
- MudBlazor: set `MudThemeProvider.RightToLeft = true` for AR.
- MAUI: set `FlowDirection.RightToLeft` on root layouts when AR is active.
- CSS: use logical properties (`margin-inline-start` instead of `margin-left`).

## Formatting
- Dates, numbers, currency — always formatted via `CultureInfo` rules; never hardcoded.
- Currency symbol depends on context (tenant's billing currency), not user culture — render `123,45 ₺` even when UI is in English if billing is TRY.

## Admin UI
- Translation management screen lets authorized users edit `LocalizationResources` per culture.
- Missing-translation report: query for keys without entries in target culture.
- Import/Export translations as JSON/Excel.
