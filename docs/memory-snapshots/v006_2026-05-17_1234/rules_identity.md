---
name: Kimlik ve Yetkilendirme Kuralları
description: Merkezi identity, hibrit JWT + Redis session store, scope hiyerarşisi (System/Tenant/Company/Unit), sekme başına bağlam, support mode, sistem rolleri
type: project
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
## Merkezi Identity
- **Tek global kullanıcı deposu** Catalog DB'de yaşar (tenant registry ile birlikte).
- Bir kullanıcı birden çok scope'ta, birden çok rol taşıyabilir.
- Auth sağlayıcı: **ASP.NET Core Identity** + hiyerarşik atama için custom store.

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

Permission haritalaması Faz 1'de ManagementApp "Rol Yönetimi" ekranı ile yapılır; v0.1.4'te yalnız rol adları seed'lendi.

## Bootstrap (İlk System Admin)
- **Development / Demo:** `env-seed` ile otomatik. Sabit credential `.env.<env>`'den; idempotent.
  - Yusuf'un Developer hesabı: `yusuf.gulmez.ai@gmail.com`, "YUSUF GÜLMEZ".
- **Test:** Yalnız rol katalogu.
- **Production:** Seed kullanıcı oluşturmaz. `init-system-admin` CLI ile manuel, interaktif şifre prompt.

## Hibrit Multi-Tenancy ile Etkileşim
- `Users` Catalog DB'de global (tenant'tan bağımsız).
- `UserRoleAssignments` her atamayı taşır: `(UserId, RoleId, ScopeLevel, TenantId?, CompanyId?, UnitId?)`.
- Aynı kullanıcı System scope + birden çok Tenant/Company/Unit rolü taşıyabilir.

## Veri Modeli
- `Users` — Catalog DB'de global identity.
- `Roles` — System / Tenant / Company / Unit scope rol tanımları.
- `Permissions` — granular permission kataloğu.
- `RolePermissions` — rol → permission eşlemesi.
- `UserRoleAssignments` — kullanıcı → rol → scope.
- `RefreshTokens` — DB'de rotation chain, hashed.
- `SupportSessions` — System operatörü destek oturum kaydı.
- **Redis:** Aktif auth session'lar (geçici, TTL'li); kullanıcı ↔ session index.

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

## Auth Endpoint'leri (Faz 0 boyunca dolacak)
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout` (Bearer)
- `POST /api/v1/auth/switch-context` (Bearer)
- `POST /api/v1/system/enter-support-mode` (Bearer + System rol)
- `POST /api/v1/system/exit-support-mode` (Bearer)
- `POST /api/v1/system/elevate-to-write` (Bearer + Support session aktif)
- `POST /api/v1/system/impersonate-user` (Bearer + Support session aktif)
- `POST /api/v1/auth/2fa/enroll` (v0.1.5.c)
- `POST /api/v1/auth/2fa/verify` (v0.1.5.c)

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
- MediatR `AuthorizationBehavior` her handler öncesi permission kontrolü (Redis'ten).
- Tenant/Company/Unit scope işlemleri: request hedef ID'leri Redis session scope'uyla eşleşmeli (defense in depth).
- Hata: standart error code (`AUTH-xxx`).

## API Anahtarları (Service-to-Service)
- Hash'lenmiş saklanır, tenant + permission set scope'lu, süreli, rotate edilebilir, revoke edilebilir.
- Redis session pattern'i API key'ler için de kullanılır (lookup → permission cache).
