---
type: mimari
status: aktif
date: 2026-05-21
tags:
  - kimlik
  - auth
  - scope
related:
  - "[[Kararlar/ADR-001 Hibrit JWT + Redis Session]]"
---

# Kimlik & Auth Mimarisi

## Kimlik Merkezi

Tüm kullanıcılar **Catalog veritabanında** tek bir `AspNetUsers` tablosunda saklanır. Kullanıcı, birden fazla tenant veya şirkette farklı rollerle aktif olabilir — her biri bağımsız bir `UserRoleAssignment`.

## Kapsam Seviyeleri (ScopeLevel)

| Kapsam | Açıklama | ManagementApp'te görünür? |
|---|---|---|
| **System** | Platform yöneticisi | Evet |
| **Tenant** | Bir tenant'ın yöneticisi | Evet |
| **Company** | Bir şirketin (site/apartman) yöneticisi | Evet |
| **Unit** | Daire/ofis sahibi / kiracı | Hayır (PortalApp) |

`ScopeSelector.FilterByPersona(Management)` Unit scope'u ManagementApp'ten filtreler.

## Aktif / Pasif Ayrımı

| Bayrak | Yer | Anlamı |
|---|---|---|
| `User.IsActive` | `AspNetUsers` | **Global ban** — tüm scope'larda erişimi keser |
| `UserRoleAssignment.IsActive` | `UserRoleAssignments` | **Scope-bazlı** — yalnız o tenant/şirket erişimini keser |

`LoginFinalizer.BuildScopeOptionsAsync` yalnız `IsActive = true` atamaları döner.

## İlk Giriş Şifre Zorlaması (`RequiresPasswordChange`)

```
Login başarılı
    │
    ├─ RequiresPasswordChange == true
    │      ↓
    │  PasswordChangeChallengeStore'a kayıt (Redis, 15 dk)
    │  Cookie: __ct_pwd_chg
    │  Yönlendirme: /change-password
    │      ↓
    │  CompletePasswordChangeCommand
    │      ↓
    │  UserManager.ResetPasswordAsync + RequiresPasswordChange=false
    │      ↓
    │  LoginFinalizer.FinalizeAsync → normal token
    │
    └─ RequiresPasswordChange == false → normal akış
```

Yeni kullanıcılar (`CreateSystemUserCommand`, `CreateTenantUserCommand`) `RequiresPasswordChange = true` ile oluşturulur.

## Şifre Sıfırlama Akışı (Forgot Password)

```
/forgot-password → POST /auth/forgot-password
    │
    ├─ RequestPasswordResetCommand
    │      IVerificationCodeService.GenerateAsync("pwd-reset:{userId}", 15dk)
    │      IEmailSender → OTP e-posta
    │
    ├─ Cookie: __ct_pwd_rst (e-posta adresi)
    └─ Yönlendirme: /reset-password

/reset-password → POST /auth/reset-password
    │
    ├─ Cookie'den e-posta oku
    ├─ ResetPasswordWithCodeCommand
    │      IVerificationCodeService.VerifyAsync("pwd-reset:{userId}", code)
    │      UserManager.ResetPasswordAsync
    │      RequiresPasswordChange = false
    │
    └─ Yönlendirme: /login?info=password-reset-success
```

## 2FA Akışı

Ayrıntı: [[Kararlar/ADR-001 Hibrit JWT + Redis Session]]

- System kullanıcıları için **zorunlu** (pre-auth enrollment)
- `TwoFactorChallengeStore` (Redis, 5 dk)
- Desteklenen yöntemler: `Email`, `Sms`, `Authenticator`

## Token Yapısı

```
JWT (Access Token, minimal payload):
  sub  → userId
  sid  → sessionId   ← Redis lookup anahtarı
  ctx  → contextId   ← sekme izolasyonu
  iat, exp, iss, aud

Redis Session (tam yetki bilgisi):
  roller, izinler, kapsam, tenantId, companyId,
  personaSide, supportMode, lastActivity
```

## Çoklu Bağlam (Multi-Context)

Her tarayıcı sekmesi bağımsız bir `ContextId` taşır. Kullanıcı aynı anda birden fazla tenant'ta çalışabilir; `SwitchTenantCommand` yeni `ContextId` ile token yeniler.

## İlgili Dosyalar

- `src/Core/CleanTenant.Application/Common/Auth/` — Auth interfaces (LoginResult, IPasswordChangeChallengeStore, IVerificationCodeService)
- `src/Core/CleanTenant.Application/Features/Auth/` — Login, PasswordChange, PasswordReset, TwoFactor komutları
- `src/Infrastructure/CleanTenant.Infrastructure.Caching/` — Redis implementasyonları
- `src/Presentation/CleanTenant.ManagementApp/Auth/AuthEndpoints.cs` — Cookie endpoint'leri
