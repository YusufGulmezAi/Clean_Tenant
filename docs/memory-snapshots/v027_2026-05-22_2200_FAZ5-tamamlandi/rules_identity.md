---
name: Kimlik ve Yetkilendirme Kuralları
description: Merkezi identity, hibrit JWT + Redis session store, scope hiyerarşisi (System/Tenant/Company/Unit), sekme başına bağlam, support mode, sistem rolleri, role ownership (TenantId/CompanyId), MinimumRoleScope, RoleAccessGuard, SessionLoaderBehavior — v0.2.10 itibarıyla güncel
type: project
originSessionId: 61d8f930-a87d-4a38-a2cf-e582e02a5421
---
## Merkezi Identity
- **Tek global kullanıcı deposu** Catalog DB'de yaşar (tenant registry ile birlikte).
- Bir kullanıcı birden çok scope'ta, birden çok rol taşıyabilir.
- Auth sağlayıcı: **ASP.NET Core Identity** + hiyerarşik atama için custom store.
- `User : IdentityUser<Guid>, IAuditable, ISoftDeletable, IHasUrlCode` — `IdentityUser` base olduğu için `BaseEntity`'den miras alamaz; audit/soft-delete alanları arabirimlerle manuel eklenir.

### `User` Önemli Alanları
| Alan | Tip | Not |
|---|---|---|
| `UrlCode` | `string(9)` | Base58 |
| `FirstName`, `LastName` | `string` | — |
| `Tckn` | `string(11)?` | TC Kimlik veya YKN (her ikisi aynı checksum); login identifier olabilir |
| `TcknVerified` | `bool` | False ise TCKN ile login kabul edilmez |
| `Vkn` | `string(10)?` | Vergi Kimlik Numarası |
| `VknVerified` | `bool` | False ise VKN ile login kabul edilmez |
| `LastLoginAt`, `LastLoginIp` | — | Audit + yeni cihaz tespiti |
| `PreferredCulture` | `string?` | **v0.2.10** — BCP-47 (örn. `tr-TR`), login sonrası `.AspNetCore.Culture` cookie'ye yazılır |
| `RowVersion` | `uint` | PostgreSQL `xmin` |

`IdentityUser`'dan otomatik gelen: `Email`, `EmailConfirmed`, `PhoneNumber`, `PhoneNumberConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `TwoFactorEnabled`, `LockoutEnabled`, `LockoutEnd`.

## Auth Modeli: Hibrit JWT + Redis Session Store

CleanTenant **stateless saf JWT** kullanmaz; **hafif JWT + Redis-backed session store** kombinasyonunu kullanır. Sektör pratiği (Stripe, GitHub, Auth0 benzeri):
- JWT **sadece bir referans** (signature güvencesiyle imzalı, küçük).
- Yetki bilgileri (roller, permission'lar, scope) **Redis'te** session kaydında.
- Server-side revocation, anlık yetki değişimi mümkün.

### JWT İçeriği (Minimum — ~250 byte)
```json
{
  "sub":  "<userId>",
  "sid":  "<sessionId>",     // Redis lookup key
  "ctx":  "<contextId>",     // sekme/persona izolasyonu için
  "iat":  <issued-at>,
  "exp":  <expiry>,
  "iss":  "cleantenant",
  "aud":  "cleantenant"
}
```
Hassas / dinamik bilgi (roller, permission'lar, scope, persona) JWT'de değil; Redis session'da.

**Blazor Server için ek cookie claim'leri** (`AuthEndpoints.SignInWithSessionAsync`):
`ClaimTypes.NameIdentifier=<userId>`, `user_id`, `sid`, `ctx`, `scope`, ve aktif bağlam için `tenant_id`/`tenant_name`/`company_id`/`company_name`. SignalR circuit'inde `HttpUserContext` null olduğundan AppBar bağlamı cookie claim'lerinden okur.

### Redis Session Yapısı
Key: `session:{sessionId}` — TTL access token süresi + 30 dk padding (kullanıcı aktifse otomatik uzar).

```json
{
  "userId": "<guid>",
  "contextId": "<guid>",
  "email": "...",
  "userName": "...",
  "scopeLevel": "System | Tenant | Company | Unit",
  "tenantId": "<guid?>",
  "companyId": "<guid?>",
  "unitId": "<guid?>",
  "roles": ["TenantAdmin"],
  "permissions": ["Tenant.Read", "Invoice.Approve", ...],
  "personaSide": "Management | Portal",
  "isSystemSession": false,
  "supportSessionId": "<guid?>",
  "supportMode": "None | ReadOnly | WriteEnabled | FullImpersonation",
  "issuedAt": "...",
  "lastActivity": "..."
}
```

Ek index: `user:{userId}:sessions` → set of active sessionIds (toplu revocation için).

## Auth Akışları

### Login
1. `UserManager.CheckPasswordAsync` + lockout kontrolü.
2. 2FA aktifse → token doğrulama (v0.1.5.c).
3. Roller + permission'lar Catalog DB'den çekilir.
4. Yeni `sessionId` üretilir.
5. **Redis'e session yaz** (TTL + user index'e ekle).
6. JWT (`sub`, `sid`, `ctx`) üretilir; refresh token DB'ye yazılır (uzun ömürlü).
7. `TokenPair` döner.
8. **v0.2.10.d:** `User.PreferredCulture` DB'den okunup `.AspNetCore.Culture` cookie'sine yazılır.

### Her HTTP İsteği
1. Bearer JWT alınır.
2. Signature doğrulanır (HMAC SHA-256).
3. `sid`'den **Redis lookup** yapılır.
4. Redis'te yoksa → **401 Unauthorized** (revoked ya da TTL doldu).
5. Varsa → `HttpUserContext` Redis'teki bilgilerle doldurulur.
6. `lastActivity` güncellenir (sliding TTL).

### Refresh
1. Refresh token DB'de bulunup doğrulanır (rotation chain, replay tespiti).
2. Aynı `sessionId` üzerinde yeni JWT üretilir (Redis session devam eder).
3. Eski refresh revoke edilir; yenisi DB'ye yazılır.

### Logout
1. Redis'ten `session:{sessionId}` silinir.
2. `user:{userId}:sessions` set'inden çıkarılır.
3. İlişkili refresh token DB'de revoke edilir.
4. **Çalınan token bile bir sonraki istekte 401 alır.**

### Context Switch
1. Mevcut kullanıcı yeni scope seçer (Tenant/Company/Unit değişimi).
2. Yeni `sessionId` üretilir; **eski session aktif kalır** (diğer sekmeler etkilenmez).
3. Yeni roller + permission'lar yeni scope için Redis'e yazılır.
4. Yeni JWT (yeni `sid`) döner.

### Anlık Revocation Senaryoları
- **Yetki değişimi (rol/permission düzenlendi):** Etkilenen kullanıcıların `user:{userId}:sessions` set'i çekilir, her session Redis'te güncellenir (yeni roller + permission'lar) → sonraki istek anında yeni yetkilerle.
- **Kullanıcı kilitlendi:** Tüm session'ları silinir; tüm sekmelerden atılır.
- **Şüpheli aktivite:** Admin "tüm session'ları sonlandır" der → toplu silme.
- **Tenant suspend:** O tenant'a bağlı tüm session'lar invalidate.

## Scope Hiyerarşisi
Rol atamaları yalnız aşağıdaki scope seviyelerinden biriyle yapılır:
```
System          (SaaS işletmecisi personeli; tüm tenant'lar üstü)
  Tenant        (tek bir tenant kapsamı)
    Company     (bir tenant içinde tek şirket; aynı şirkette birden çok rol mümkün)
      Unit      (bir şirket altında tek bağımsız bölüm; Malik/Hissedar/Sakin/Kiracı)
```
- **Building rol scope'u DEĞİL.** Yapı entity'si Unit'leri gruplamak için var; rol bağlamaları doğrudan Unit'tedir.
- **Numerik sıralama:** `System=1 < Tenant=2 < Company=3 < Unit=4`. Küçük değer = geniş yetki.

## Built-in Sistem Rolleri (7, Hard-Coded)
SaaS işletmecisinin personeli için. Esnek değil — yeni System rolü tanımlanamaz.

| Rol | Sorumluluk |
|---|---|
| **Developer** | Kod geliştirme, debug için tam erişim |
| **SystemAdmin** | Sistem ayarları, tenant onboarding, ortam yönetimi |
| **CustomerSupport** | Müşteri destek görüşmeleri; Support Mode'a girer |
| **TechnicalSupport** | Teknik destek; log/audit görüntüleme |
| **Accountant** | Fatura, ödeme, abonelik yönetimi |
| **Manager** | Yönetici raporları, iş metrikleri |
| **Sales** | Satış / demo erişimi, lead yönetimi |

Permission haritalaması ManagementApp "Rol Yönetimi" ekranı ile yapılır.

## Bootstrap (İlk System Admin)
- **Development / Demo:** `env-seed` ile otomatik. Sabit credential `.env.<env>`'den; idempotent.
  - Yusuf'un Developer hesabı: `yusuf.gulmez.ai@gmail.com`, "YUSUF GÜLMEZ".
- **Test:** Yalnız rol katalogu.
- **Production:** Seed kullanıcı oluşturmaz. `init-system-admin` CLI ile manuel, interaktif şifre prompt.

## Hibrit Multi-Tenancy ile Etkileşim
- `Users` Catalog DB'de global (tenant'tan bağımsız).
- `UserRoleAssignments` her atamayı taşır: `(UserId, RoleId, ScopeLevel, TenantId?, CompanyId?, UnitId?)` + DB seviyesinde scope tutarlılık `CHECK constraint`.
- Aynı kullanıcı System scope + birden çok Tenant/Company/Unit rolü taşıyabilir.

## Veri Modeli (v0.2.10 itibarıyla güncel)

### `Role : IdentityRole<Guid>, IAuditable, ISoftDeletable, IHasUrlCode`
- `UrlCode` (9-char Base58)
- `Scope` — `ScopeLevel` enum (System/Tenant/Company/Unit)
- **`TenantId : Guid?`** — `null` ise **global** rol (built-in + System-defined ortak roller); dolu ise yalnız o tenant'a ait. **v0.2.8.b** ile eklendi.
- **`CompanyId : Guid?`** — `TenantId` dolu olmadan dolu olamaz. CompanyAdmin'in kendi sitesi için açtığı roller.
- `Description` (nullable string)
- `IsBuiltIn` — true ise seed'lenmiştir, silinemez/yeniden adlandırılamaz
- **Unique index:** `(NormalizedName, Scope)` bileşik — aynı isim farklı scope'larda mümkün (örn. "Admin" hem Tenant hem Company seviyesinde). **NormalizedName alone NOT unique.** Custom rol oluşturulurken bu sayede tenant-bazlı isim çakışması engellenmez (gerekirse uygulama tarafında ek kontrol).
- Migration: `AddRoleTenantCompanyScoping` (v0.2.8)

### `Permission : BaseEntity`
- `Code` — sistem genelinde unique; format `"<Module>.<Action>[.<Qualifier>]"` (örn. `Tenant.Create`, `User.Manage.Tenant`).
- `Description`, `Module` (gruplama)
- **`MinimumRoleScope : ScopeLevel`** — **v0.2.8** ile eklendi. Bu izni tutabilmek için rolün **en az** hangi scope'a sahip olması gerek. Filtre kuralı: `role.Scope <= permission.MinimumRoleScope` olduğunda izin atanabilir. Örnekler:
  - `Tenant.Create` → `MinimumRoleScope=System` → yalnız System rolleri tutabilir
  - `User.Read` → `MinimumRoleScope=Tenant` → System + Tenant rolleri tutabilir
  - `Unit.Read` → `MinimumRoleScope=Unit` → tüm scope'lar tutabilir
  - `ScopeLevel.None` (0) = "scope kısıtlaması yok" (seeder güncellemesi henüz uygulanmamış permission'lar için)
- Migration: `AddPermissionMinimumRoleScope` (v0.2.8)

### Diğer
- `RolePermissions` — rol → permission eşleme join.
- `UserRoleAssignments` — `(UserId, RoleId, ScopeLevel, TenantId?, CompanyId?, UnitId?, AssignedAt, AssignedBy, ExpiresAt?, IsActive)`.
- `RefreshTokens` — DB'de rotation chain, `TokenHash` SHA-256, `ReplacedByTokenHash`, IP/UserAgent.
- `SupportSessions` — System operatörü destek oturum kaydı.
- `UrlCodeRegistry` — `(UrlCode PK, EntityType, EntityId)` — 9-char Base58 merkezi tablo.
- **Redis:** Aktif auth session'lar (geçici, TTL'li); kullanıcı ↔ session index.

## Role Yönetimi Yetki Kontrolleri (`RoleAccessGuard`)

**Konum:** `Application/Common/Authorization/RoleAccessGuard.cs` (v0.2.8.c). Static utility — handler'larda çağrılır. Hatalar `UnauthorizedAccessException` ile fırlatılır; UI Snackbar gösterir.

### `EnsureCanManageRole(session, role)` — düzenleme/silme yetkisi
- System scope ise bypass.
- `IsBuiltIn=true` rol → "Built-in roller yalnız Sistem tarafından düzenlenebilir" (403).
- `role.TenantId is null` (global rol) → "Global roller yalnız Sistem tarafından yönetilebilir".
- `role.TenantId != session.TenantId` → "Bu rolü yönetme yetkiniz yok (farklı yönetim)".
- `role.CompanyId is not null && != session.CompanyId` → "farklı site".

### `EnsureCanCreateAtScope(session, newRoleScope)`
- System bypass.
- `(int)newRoleScope < (int)session.ScopeLevel` (yani daha geniş scope için rol oluşturma denemesi) → engellenir.

### `EnsurePermissionCeiling(session, requestedPermissionCodes)`
- **Privilege ceiling:** atanmak istenen tüm izinler assigner'ın kendi izin setinde olmalı. System bypass. **Backend mecburi** — UI filter'ı bypass edilebilir.

### `EnsureScopeCeiling(roleScope, requestedPermissions)`
- **Scope ceiling:** atanan her iznin `MinimumRoleScope`'u, rolün scope'undan ≤ (numerik) olmalı. Tenant rolüne System-only izin atanamaz.
- `ScopeLevel.None` olan permission'lar skip edilir (eski kayıt).

## Company Tab Permissions (Detay Sayfası, v0.2.8.d)

`Application/Common/Authorization/CompanyTabPermissions.cs` — Site detay sayfasındaki 8 tab için fine-grained read/write izinleri. **Sistem scope kullanıcısı otomatik bypass.**

| Tab | Read | Write |
|---|---|---|
| Bilgiler (genel info) | `company.info.read` | `company.info.write` |
| İletişim | `company.contact.read` | `company.contact.write` |
| Muhasebe | `company.accounting.read` | `company.accounting.write` |
| Finans | `company.finance.read` | `company.finance.write` |
| İK | `company.hr.read` | `company.hr.write` |
| Puantaj | `company.timesheet.read` | `company.timesheet.write` |
| Bordro | `company.payroll.read` | `company.payroll.write` |
| Satınalma | `company.purchasing.read` | `company.purchasing.write` |

**UI davranışı:** Tab yalnız read varsa görünür; Kaydet butonu yalnız write varsa aktiftir.

## Aynı Tarayıcıda Çoklu Bağlam
- Her sekme bağımsız bağlam çalıştırabilir.
- Her sekme **kendi `sessionId`'sine** sahip → Redis'te ayrı kayıt.
- ManagementApp'te bağlam değişimi → yeni `sessionId` (eski session diğer sekmelerde aktif kalır).
- Token `sessionStorage`'da saklanır (sekme başına izole; `localStorage` paylaşımlı, kullanılmaz).

## Access Token / Refresh Token Süreleri
| Kullanıcı tipi | Access Token TTL | Refresh Token TTL | Redis Session TTL |
|---|---|---|---|
| System | 15 dk | 7 gün | 45 dk (15 + 30 padding) |
| Tenant / Company / Unit | 30 dk | 7 gün | 60 dk (30 + 30 padding) |
| Support Mode | 5 dk | aynı zincir | 10 dk |

Süreler `.env.<env>` dosyasından `JWT_*` ve `SESSION_*` değişkenleriyle override edilebilir.

## Auth Endpoint'leri

### API endpoint'leri (Bearer JWT)
- `POST /api/v1/auth/login` — persona parametresi zorunlu
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout` (Bearer)
- `POST /api/v1/auth/switch-context` (Bearer)
- `POST /api/v1/users/me/sessions/logout-all` (Bearer) — kendi tüm cihazlarımdan çıkış
- `POST /api/v1/users/{userUrlCode}/force-logout` (Bearer + admin scope)
- `POST /api/v1/system/sessions/{sessionId}/revoke` (Bearer + System scope)
- `GET  /api/v1/tenant/audit/support-access` (Bearer + Tenant Admin)
- `GET  /api/v1/system/support-sessions` (Bearer + System scope)
- `POST /api/v1/system/support/enter` (Bearer + SystemScope policy)
- `POST /api/v1/system/support/exit` (Bearer + SupportModeActive policy)
- `POST /api/v1/system/support/elevate` (Bearer + SupportModeActive policy; in-place mutation — JWT yenilenmez)
- `POST /api/v1/system/support/impersonate` (Bearer + SupportModeActive policy; yeni JWT, `sub`=hedef, `ImpersonatedBy`=operatör)
- `POST /api/v1/auth/2fa/verify` (anonim — challenge token ile)
- `POST /api/v1/auth/2fa/send-code` (anonim — Email/SMS kod gönderim)
- `POST /api/v1/auth/2fa/enroll/totp` (Bearer)
- `POST /api/v1/auth/2fa/enroll/totp/confirm` (Bearer — 10 recovery code döner)
- `POST /api/v1/auth/2fa/disable/totp` (Bearer — System son yöntem kontrolü)
- `POST /api/v1/auth/2fa/recovery-codes/regenerate` (Bearer)
- `GET  /api/v1/auth/2fa/methods` (Bearer — aktif yöntemler + recovery sayısı)

### ManagementApp cookie endpoint'leri (Blazor Server, `AuthEndpoints.cs`)
Razor form'ları HTTP POST eder — handler IMediator.Send ile backend'i çağırır, sonra cookie set/sil + redirect.

- `POST /auth/sign-in` — Login form post; başarılıysa cookie + culture cookie set
- `POST /auth/sign-out`, `GET /auth/sign-out` — Logout
- `POST /auth/2fa/verify` — Challenge cookie üzerinden 2FA doğrulama
- `POST /auth/2fa/send-code` — 2FA kod gönderim
- `POST /auth/2fa/enroll-pre-auth/finalize` — Pre-auth enrollment finalize
- `POST /auth/switch-tenant` (Auth) — `SwitchTenantCommand` çağırır, yeni cookie + redirect
- `POST /auth/switch-to-system` (Auth) — System scope'a dönüş
- **`POST /auth/change-culture`** (anonim) — **v0.2.10.d** — `.AspNetCore.Culture` cookie set + (login'liyse) `User.PreferredCulture` DB update

### Cookie İsimleri (Blazor Server)
- `__ct_2fa_chal` (5 dk TTL, HttpOnly, Secure, SameSite=Strict) — 2FA challenge token
- `__ct_2fa_enroll` (10 dk TTL) — Pre-auth enrollment challenge
- `.AspNetCore.Culture` (365 gün, SameSite=Lax, IsEssential) — kültür tercihi

## Login Persona ve Scope Erişim Sınırları

Login endpoint'inde **persona** zorunlu parametre. Persona, login akışında hangi scope'ların erişilebilir olduğunu kesin olarak belirler — güvenlik sınırı.

### Persona Türleri
| Persona | Hangi uygulama gönderir | İzin verilen scope'lar |
|---|---|---|
| **Management** | ManagementApp, MobilApp (Management persona) | System / Tenant / Company |
| **Portal** | PortalApp, MobilApp (Portal persona) | Unit |

### Güvenlik Sınırı (Sıkı)
- **Unit kullanıcıları (Malik / Hissedar / Sakin / Kiracı) ManagementApp'ten LOGIN OLAMAZ.** Persona=Management ise Unit atamaları `availableScopes`'a dahil edilmez; tek Unit yetkisi olan kullanıcı login persona=Management ile geldiğinde "uygun scope bulunamadı" hatası alır.
- **System / Tenant / Company kullanıcıları PortalApp'ten LOGIN OLAMAZ.** PortalApp persona=Portal sabit gönderir; bu personada yalnız Unit scope'ları görünür.
- Bir kullanıcı her iki tarafta da yetkiye sahipse, hangi uygulamadan login olduğu hangi scope'ları görebileceğini belirler. JWT'deki `personaSide` claim'i Redis session'a kaydedilir.

### Primary Scope Seçim Önceliği
- **Persona=Management:** `System > Tenant > Company` (sonra alfabetik — örn. tenant adı). **Unit yok.**
- **Persona=Portal:** Yalnız Unit scope'ları; birden fazla varsa kullanıcı UI'da seçer.

### Login Response'unda `availableScopes`
Persona ile filtre edilmiş liste. Tek scope varsa istemci direkt kullanır; çoklu scope varsa scope seçici sunulur ve istemci `/auth/switch-context` çağırarak geçer.

### Switch-Context Sınırı
`/auth/switch-context` aynı persona'nın scope'ları arasında geçişe izin verir. Persona değiştirmek için kullanıcının logout + yeni persona ile login yapması gerekir (cross-persona oturum atlamasını engelleyici).

## 2FA Politikası
- **System kullanıcıları → ZORUNLU.** Login'de dayatılır, kapatılamaz. En az bir yöntem aktif olmalı.
- **ManagementApp diğer kullanıcıları → opsiyonel.**
- **PortalApp / MobilApp → opsiyonel.**

### Desteklenen 2FA Yöntemleri (3)
Kullanıcı birden çok yöntemi aynı anda aktif edebilir.

| Yöntem | ASP.NET Core Identity Sağlayıcısı |
|---|---|
| **Google Authenticator** (TOTP / RFC 6238) | `AuthenticatorTokenProvider<User>` |
| **E-Posta** | `EmailTokenProvider<User>` (`EmailConfirmed=true` şartı) |
| **SMS** | `PhoneNumberTokenProvider<User>` (`PhoneNumberConfirmed=true` şartı) |

### Recovery Codes
- Enrollment'ta 10 adet tek kullanımlık kod.
- Tüm yöntemler kullanılamaz olursa giriş için.
- ASP.NET Core Identity'nin `GenerateNewTwoFactorRecoveryCodesAsync` API'sıyla yönetilir.

### Şema
- ASP.NET Core Identity built-in tabloları (`IdentityUserTokens`, `IdentityUserClaims`) bu 3 yöntemi + recovery kodları taşır. **Ayrı tablo yok.**

### Login Akışı 2FA Dallanması (v0.1.5.c)
Login response polimorfik tipte (`LoginResult { Status, Tokens?, Challenge? }`):
- `Status="Success"` → TokenPair direkt (kullanıcı 2FA'sız).
- `Status="TwoFactorRequired"` → `TwoFactorChallengeResponse` (5 dk TTL, available methods listesi).
- `Status="EnrollmentRequired"` → System scope + `TwoFactorEnabled=false` (v0.2.2.a pre-auth enrollment akışı).

İstemci `/api/v1/auth/2fa/verify` ile method (Authenticator/Email/Phone/RecoveryCode) + kod gönderir → TokenPair döner.

Email/SMS yöntemleri için ek olarak `/api/v1/auth/2fa/send-code` ile kod istenebilir (challenge token'la).

### System Kullanıcıları İçin Sıkı Kurallar (v0.1.5.c implementasyon)
- **Enrollment zorunlu:** System scope rolü olan + `TwoFactorEnabled=false` kullanıcı login denerse → `AUTH-2FA-ENROLLMENT-REQUIRED` (422).
- **Son yöntem kilidi:** System kullanıcısının tek aktif 2FA yöntemini kapatma denemesi → `AUTH-2FA-LAST-METHOD-LOCK` (403).
- En az bir yöntem (TOTP / E-posta / SMS) yeterli — TOTP şart değil.

### Tasarım Detayları
- **AuthenticatorTokenProvider sunucuda kod ÜRETMEZ** — TOTP secret yalnız kullanıcının authenticator app'inde. Sunucu yalnız `VerifyTwoFactorTokenAsync` ile doğrular. (Test fixture'da Email provider'ı kullanılır.)
- **Challenge token tek kullanımlık:** doğrulamadan sonra Redis'ten silinir (replay engelleme).
- **Sender provider'ları** `Email:Provider` / `Sms:Provider` config'iyle seçilir; Production'da `Console` yasak (boot'ta `InvalidOperationException`).
- **Recovery code:** enrollment'ta 10 adet, `GenerateNewTwoFactorRecoveryCodesAsync(user, 10)`; ASP.NET Identity default formatı (`XXXXX-XXXXX`).

## Şifre Politikası
Tek tip politika tüm kullanıcılar için (Identity default'undan daha sıkı):
- **Minimum 8 karakter**
- En az 1 büyük + 1 küçük + 1 rakam + 1 özel karakter
- Min 4 benzersiz karakter
- Son 5 şifre tekrar kullanılamaz
- 90 günde bir değiştirme uyarısı (zorunlu değil; ileride sıkılaştırılabilir)

## Login Bildirimi (Yeni Cihaz/IP)
- System kullanıcısı yeni cihaz/IP'den login → e-posta uyarısı.
- ManagementApp ve Portal kullanıcıları için opsiyonel (kullanıcı tercihi).
- "Bilinen cihaz" listesi user profile'ında; çıkış yapılabilir.

## IP Whitelist (Opsiyonel)
- System kullanıcıları için sistem düzeyinde IP whitelist.
- Ofis IP'leri / VPN range'leri config'te.
- Whitelist boş ise tüm IP'lere izin (default).

## Support Mode (System Operatör → Tenant Bağlamı)

### Yaklaşım: Hibrit
- **Default: Context Elevation.** Operatör kendi kimliğini korur, görüntüleme bağlamını hedef tenant'a değiştirir. Salt okunur erişim.
- **Opsiyonel: True Impersonation.** Operatör hedef kullanıcının kimliğine bürünür (`impersonatedBy` etiketi).
- **Yazma yetkisi:** Operatör sebep yazar (min 20 karakter), yazma açılır, audit'e kayıt. **Sade akış** — müşteri anlık onayı v1'de yok.

### Akış
1. System kullanıcı ManagementApp gizli bölümünden tenant seçer.
2. Onay modalı: salt okunur, sebep, devam.
3. Backend: `SupportSession` kaydı + Redis'te yeni `sessionId` (supportMode=ReadOnly).
4. UI banner: "🔧 SUPPORT MODE: Acme Corp — Çık".
5. Opsiyonel "Yazma yetkisi al" → sebep + Redis update (supportMode=WriteEnabled), banner kırmızı.
6. Opsiyonel "Bir kullanıcı olarak gör" → kullanıcı seç + sebep → Redis update (supportMode=FullImpersonation).
7. "Çıkış" → SupportSession.EndedAt + Redis session sil → orijinal System session'a dön.

### Session Yaşam Döngüsü Detayı (v0.1.5.b.2 ile sabitlendi)
- **Enter / Exit / Impersonate** — yeni `sessionId` + yeni JWT (her aşamada `sid` değişir).
- **Elevate (ReadOnly → WriteEnabled)** — JWT yenilenmez; mevcut Redis session in-place mutate edilir (`AuthSession.SupportMode = "WriteEnabled"`).
- `AuthSession.OriginalSessionId` — operatörün System scope orijinal session'ına geri dönüş için. Exit'te orijinal Redis'te yoksa `SUP-004` ile yeniden login zorunlu.
- `AuthSession.ImpersonatedBy` — yalnız `FullImpersonation` modunda dolu; JWT'nin `sub`'u hedef kullanıcı olsa da audit trail için operatör korunur.
- Impersonate akışı `UserRoleAssignment` join'leriyle hedef kullanıcının o scope'taki gerçek rol + permission listesini Redis'e yazar (operatörün değil).

### SupportSessions Tablosu (Catalog DB)
- `Id`, `UrlCode`, `OperatorUserId`, `TargetTenantId`, `TargetCompanyId?`, `TargetUserId?`
- `Mode` (smallint: ReadOnly | WriteEnabled | FullImpersonation)
- `Reason` (zorunlu, min 20 karakter)
- `StartedAt`, `EndedAt`, `WriteActionCount`, `CustomerNotified`
- `IpAddress`, `UserAgent`

### Şeffaflık (Tenant Admin Görünürlüğü)
- Tenant Admin `/tenant/audit/support-access` sayfasında tüm Support Session geçmişini görür.
- KVKK uyumu için kritik; Faz 1 kapsamında.

## Mobil Uygulama Login Akışı
- Sadece Management rolleri varsa → ManagementApp ekranları.
- Sadece Portal rolleri varsa → PortalApp ekranları.
- İkisi de varsa → persona seçim ekranı; seçim Redis session'ında `personaSide`.

## Yetkilendirme Uygulanışı

### MediatR Pipeline
**DI sırası (Identity registration `services.Insert(0, ...)` ile en başa eklenir):**

1. **`SessionLoaderBehavior<TRequest, TResponse>`** — v0.2.10. Blazor Server SignalR circuit'inde `HttpUserContext.Current` null kalabilir (middleware Razor component event'lerinde tetiklenmez). Bu behavior `HttpContext.User`'dan `sid` claim'ini okur ve Redis'ten async session yükler. **Sadece HttpUserContext null ise çalışır** — normal HTTP request'lerde middleware zaten doldurmuştur.

2. **`AuthorizationBehavior`** — `[RequirePermission]` attribute kontrolü; SessionPermissionChecker kullanır. Reddederse `AUTH-PERMISSION-DENIED`.

3. **`ValidationBehavior`** — FluentValidation IValidator<T> setini çalıştırır; çoklu ihlal toplanır.

4. **`LoggingBehavior`** — Information seviye; request adı, user, elapsed.

### `IHttpUserContext` (Application contract)
- `Current : AuthSession?` — aktif Redis session projection'ı.
- `SessionLookupMiddleware` HTTP request'lerinde doldurur; SignalR'da SessionLoaderBehavior doldurur.

### `GetCurrentSessionInfoQuery` (v0.2.10)
**Konum:** `Application/Features/Auth/CurrentSession/`

UI bileşenleri (Blazor Server) Redis'teki `Permissions` listesine direkt erişemez (component init'inde `ICurrentSessionAccessor` null gelir). Bu sorgu MediatR pipeline'ı üzerinden çağrılır — SessionLoaderBehavior session'ı doldurur, sonra handler `IsAuthenticated / IsSystem / PermissionCodes` projection'ı döner. **PermissionPicker** bileşeni bunu kullanarak privilege ceiling'i UI'da gösterir.

### Defense in Depth
- Tenant/Company/Unit scope işlemleri: request hedef ID'leri Redis session scope'uyla eşleşmeli.
- Hata: standart error code (`AUTH-xxx`).

## API Anahtarları (Service-to-Service)
- Hash'lenmiş saklanır, tenant + permission set scope'lu, süreli, rotate edilebilir, revoke edilebilir.
- Redis session pattern'i API key'ler için de kullanılır (lookup → permission cache).
