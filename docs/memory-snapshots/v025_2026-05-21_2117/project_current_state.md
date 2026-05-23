---
name: Proje Mevcut Durumu — v0.2.12 tamamlandı
description: v0.2.12 tag'lendi ve push edildi; kapsamlı kullanıcı yönetimi revizyonu — 9 görev tamamlandı. Obsidian wiki kuruldu.
type: project
originSessionId: 44c488d1-5627-41e6-a9f2-b4539fbed9b3
---
Son tag: **v0.2.12** (commit `b644d05`, push edildi).

**Obsidian Wiki:** `d:\Projeler\CleanTenant\docs\wiki\CleanTenant\` altında kuruldu (2026-05-21).
Vault yapısı: `_prompts/`, `Fazlar/` (v0.0 eğitim + v0.1 + v0.2), `Kararlar/` (ADR-001–004), `Mimari/`, `Specs/budget-module/`, `Keşif/`, `Fikirler/`.

**Why:** v0.2.12 oturumunda (2026-05-21) kapsamlı kullanıcı yönetimi revizyonu yapıldı.

**How to apply:** Yeni oturumda `git status` ve `git log --oneline -3` ile gerçek durumu doğrula.

---

## v0.2.12 — Oturum Özeti (2026-05-21, tag + push tamamlandı)

### Tamamlanan 9 Görev

**A — NavMenu yeniden yapılanması**
- System context: Tenant + Company linkleri `MudNavGroup "Yönetimler"` içine alındı
- Tenant context: Kullanıcılar ve Roller top-level link, `_canManageUsers` permission gating

**B — RequiresPasswordChange: User entity + migration + login akışı**
- `User.RequiresPasswordChange` boolean property eklendi
- EF migration: `20260521133910_AddUserRequiresPasswordChange` (catalog DB)
- `LoginCommandHandler`: şifre doğrulamasından sonra `IssuePasswordChangeChallengeAsync`
- `IPasswordChangeChallengeStore` + `RedisPasswordChangeChallengeStore` (key: `{prefix}:pwd-chg:{token:N}`)
- `CompletePasswordChangeCommand/Handler` → `LoginFinalizer.FinalizeAsync`
- `AuthEndpoints`: `PasswordChangeRequired` redirect + `/auth/change-password` endpoint
- `ChangePasswordPage.razor` (`/change-password`, EmptyLayout, AllowAnonymous)
- `CreateSystemUserCommandHandler` + `CreateTenantUserCommandHandler`: `RequiresPasswordChange = true`

**C — Doğrulama kodu servisi (Redis OTP store)**
- `IVerificationCodeService` interface + `RedisVerificationCodeService`
- Key pattern: `{prefix}:otp:{key}`, 6 hane, single-use verify

**D — Şifre sıfırlama akışı (forgot-password / reset-password)**
- `RequestPasswordResetCommand/Handler`: OTP e-postayla gönderir (enumeration korumalı)
- `ResetPasswordWithCodeCommand/Handler`: OTP doğrulama + UserManager password reset
- `AuthEndpoints`: `/auth/forgot-password` + `/auth/reset-password` endpoint'leri, `__ct_pwd_rst` cookie
- `ForgotPasswordPage.razor` (`/forgot-password`) + `ResetPasswordPage.razor` (`/reset-password`)
- Login sayfasına "Şifremi unuttum" linki + `?info=password-reset-success` success alert

**E — LookupUserByIdentifierQuery**
- `LookupUserByIdentifierQuery(UserLookupType, string Value)` + handler
- `UserLookupType`: Tckn, Vkn, Phone, Email
- `[RequirePermission("System.Users.Manage", "Tenant.Users.Manage")]`

**F — UserOnboardingWizard.razor**
- State machine: Idle → Searching → Partial / UserFound / NewUser
- 500ms debounce ile TCKN/VKN/Phone/Email arama
- UserFound: bulunan kullanıcıya rol ata (`AssignUserToTenantCommand`)
- NewUser: yeni kullanıcı oluştur (`CreateTenantUserCommand` / `CreateSystemUserCommand`)
- UserListPanel: Tenant scope'ta tek "Kullanıcı Ekle" butonu wizard'ı açar

**G — Scope-bazlı aktif/pasif komutları**
- `DeactivateTenantUserCommand/Handler`: tenant-scope assignment.IsActive = false
- `ReactivateTenantUserCommand/Handler`: tenant-scope assignment.IsActive = true
- `UserListPanel`: Deactivate/Reactivate scope-aware (Tenant → assignment, System → global)

**H — Tenant kapsamlı kullanıcı görünümü**
- `ListUsersQueryHandler`: `isTenantView` ile hem Tenant-scope hem Company-scope atamalar birleştirilir
- `UserListItem.IsActive`: tenant view'da active assignment'tan türetilir (global flag değil)
- Deactivated tenant users artık listede görünür → reactivate butonu çalışır

**I — Build + doğrulama**
- 0 hata, 0 uyarı — tam çözüm build
- ManagementApp https://localhost:7212 ayakta (localization seeder 120 yeni key ekledi)
- `/forgot-password`, `/reset-password`, `/login` HTTP 200

---

## Önceki Faz Geçmişi (Kısa)

- **v0.1.x** — Faz 0 (Backend: Auth + 2FA + Multi-scope + 146 test)
- **v0.2.1** — ManagementApp Shell + 4 Tema + MudBlazor
- **v0.2.2.x** — Auth UI (Login + 2FA + UX)
- **v0.2.3–v0.2.5** — Company/Role CRUD + Permission system
- **v0.2.6** — Audit Explorer
- **v0.2.7** — PortalApp Shell MVP
- **v0.2.10** — Lokalizasyon (TR/EN/AR/RU/DE + admin sayfası)
- **v0.2.11** — PermissionPicker yeniden tasarım + CompanyForm bölüm grupları + Tab'lı Tenant form
