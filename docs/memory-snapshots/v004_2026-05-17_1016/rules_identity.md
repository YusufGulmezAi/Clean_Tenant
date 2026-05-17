---
name: Kimlik ve Yetkilendirme Kuralları
description: Merkezi identity, scope hiyerarşisi (System/Tenant/Company/Unit), tarayıcı sekme başına bağlam, support mode, sistem rolleri
type: project
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
## Merkezi Identity
- **Tek global kullanıcı deposu** Catalog DB'de yaşar (tenant registry ile birlikte).
- Bir kullanıcı birden çok scope'ta, birden çok rol taşıyabilir.
- Auth sağlayıcı: **ASP.NET Core Identity** + hiyerarşik atama için custom store.

## Scope Hiyerarşisi (Kesinleşmiş)
Rol atamaları yalnız aşağıdaki scope seviyelerinden biriyle yapılır:
```
System          (tüm tenant'lar üstü; SaaS işletmecisi personeli)
  Tenant        (tek bir tenant kapsamı)
    Company     (bir tenant içindeki tek şirket; aynı şirkette birden çok rol mümkün)
      Unit      (bir şirket altındaki tek bağımsız bölüm; Malik/Hissedar/Sakin/Kiracı)
```
- **Building (Yapı) rol scope'u DEĞİL.** Building entity'si Unit'leri gruplamak için var olabilir; rol bağlamaları doğrudan Unit'tedir.

## Built-in Sistem Rolleri (7, Hard-Coded)
Bunlar SaaS işletmecisinin (yani siz ve ekibinin) personeli için; tenant'lara ait değildir. Esnek değil, yeni System rolü tanımlanamaz.

| Rol | Sorumluluk |
|---|---|
| **Developer** | Kod geliştirme, debug için tam erişim |
| **SystemAdmin** | Sistem ayarları, tenant onboarding, ortam yönetimi |
| **CustomerSupport** | Müşteri destek görüşmeleri; Support Mode'a girer |
| **TechnicalSupport** | Teknik destek; log/audit görüntüleme, daha geniş erişim |
| **Accountant** | Fatura, ödeme, abonelik yönetimi |
| **Manager** | Yönetici raporları, iş metrikleri |
| **Sales** | Satış / demo erişimi, lead yönetimi |

Permission haritalaması Faz 1'de ManagementApp "Rol Yönetimi" ekranı ile yapılacak; v0.1.4'te yalnız rol adları seed'lenir.

## Bootstrap (İlk System Admin)
- **Development / Demo:** `env-seed` ile otomatik. Sabit credential'lar `.env.<env>` dosyasından okunur. İdempotent (her çağırışta yoksa oluşturulur, varsa atlanır).
  - Yusuf'un Developer hesabı: `yusuf.gulmez.ai@gmail.com`, "YUSUF GÜLMEZ", şifre `.env.development`'tan.
- **Test:** Seed minimum — yalnız rol katalogu, kullanıcı yok (testler kendi fixture'ını oluşturur).
- **Production:** Seed kullanıcı oluşturmaz. Deploy sonrası **bir kez** CLI ile: `dotnet run --project tools/CleanTenant.MigrationRunner -- init-system-admin --email ... --first-name ... --last-name ...` (şifre interaktif prompt).

## Hibrit Multi-Tenancy (Identity ile etkileşim)
- `Users` tablosu Catalog DB'de global (tenant'tan bağımsız).
- `UserRoleAssignments` her atamayı taşır: `(UserId, RoleId, ScopeLevel, TenantId?, CompanyId?, UnitId?)`.
- Bir kullanıcının System scope'ta atama varsa "system user" olarak işlem görür.
- Aynı kullanıcı aynı zamanda Tenant/Company/Unit scope'larında da atamalar taşıyabilir (örn. çalışan + aynı zamanda kendi sitesinde Malik).

## Veri Modeli
- `Users` — Catalog DB'de global identity.
- `Roles` — System / Tenant / Company / Unit-scoped rol tanımları.
- `Permissions` — granular permission kataloğu.
- `RolePermissions` — rol → permission eşlemesi.
- `UserRoleAssignments` — `(UserId, RoleId, ScopeLevel, TenantId?, CompanyId?, UnitId?)`.
- `RefreshTokens` — rotation chain, hashed.
- `SupportSessions` — System operatörünün tenant'a girişlerinin oturum kaydı (aşağıda detay).

## Aynı Tarayıcıda Çoklu Bağlam
- Her tarayıcı sekmesi bağımsız bir bağlam çalıştırabilir.
- ManagementApp'te bağlam değişimi (Tenant / Company / Unit seçimi) → backend **yeni bağlam-bağımlı JWT** üretir.
- Her sekme kendi token'ını `sessionStorage`'da tutar (`localStorage` paylaşımlı; kullanılmaz).
- Refresh token da sekme-bağımlı; rotation chain her sekmenin kendi içinde.
- Sekme kapanınca o bağlamın oturumu biter — bilinçli güvenlik kazancı.

## JWT Claim'leri (bağlam başına)
- `userId`, `contextId` (sekme + bağlam birleşimi unique)
- `scopeLevel` (System / Tenant / Company / Unit)
- `tenantId?`, `companyId?`, `unitId?` (mevcut scope'a kadar dolu)
- `roles[]` (bu bağlamda aktif olanlar)
- `permissions[]` (rollerden türetilmiş)
- `personaSide` (Management / Portal) — mobil persona seçiminde belirlenir
- **Support Mode'da ek:** `isSystemSession`, `impersonatedUserId?`, `supportSessionId`, `supportMode` (`ReadOnly | WriteEnabled | FullImpersonation`)

## Access Token / Refresh Token Süreleri
| Kullanıcı tipi | Access Token TTL | Refresh Token TTL |
|---|---|---|
| System | **15 dk** | 7 gün |
| Tenant / Company / Unit | **30 dk** | 7 gün |
| Support Mode (System içinden) | 5 dk | (yeni context, refresh aynı zincirde) |

Süreler `.env.<env>` dosyasından `JWT_ACCESS_TOKEN_MINUTES_*` değişkenleriyle override edilebilir.

## Context-Switch Endpoint'i
- `POST /api/v1/auth/switch-context` — hedef scope'a yeni JWT.
- `POST /api/v1/system/enter-support-mode` — System kullanıcı için (aşağıda Support Mode).
- `POST /api/v1/system/exit-support-mode`
- `POST /api/v1/system/elevate-to-write`
- `POST /api/v1/system/impersonate-user` — opsiyonel; True Impersonation.

## 2FA Politikası
- **System kullanıcıları → ZORUNLU.** Login'de dayatılır, kapatılamaz. En az bir yöntem aktif olmalı.
- **ManagementApp diğer kullanıcıları → opsiyonel** (kullanıcı kendi açar).
- **PortalApp / MobilApp → opsiyonel.**

### Desteklenen 2FA Yöntemleri (3)
Kullanıcı **birden çok** yöntemi aynı anda aktif edebilir (birinden gelmezse diğeri).

| Yöntem | Açıklama | ASP.NET Core Identity Sağlayıcısı |
|---|---|---|
| **Google Authenticator** (TOTP / RFC 6238) | QR kod ile telefon uygulamasına eklenir; uygulama 30 saniyede bir 6 haneli kod üretir | `AuthenticatorTokenProvider<User>` |
| **E-Posta** | 6 haneli kod doğrulanmış e-postaya gönderilir; süreli (5 dk) | `EmailTokenProvider<User>` (`EmailConfirmed=true` şartı) |
| **SMS** | 6 haneli kod doğrulanmış telefon numarasına gönderilir; süreli (5 dk) | `PhoneNumberTokenProvider<User>` (`PhoneNumberConfirmed=true` şartı) |

### Recovery Codes
- Enrollment sırasında 10 adet tek kullanımlık recovery code üretilir.
- Kullanıcı bunları güvenli bir yerde saklamalı (kağıda yazma / parola yöneticisi).
- 3 yöntemin hepsi kullanılamaz hale gelirse (telefon kaybı + e-posta erişimsizliği) recovery code ile giriş.
- ASP.NET Core Identity'nin `GenerateNewTwoFactorRecoveryCodesAsync` API'sıyla yönetilir.

### Şema
- ASP.NET Core Identity'nin built-in tabloları (`IdentityUserTokens`, `IdentityUserClaims`) bu 3 yöntemi + recovery code'ları taşır. **Ayrı tablo açmıyoruz.**
- `User.TwoFactorEnabled` (bool) — herhangi bir yöntem aktif mi.
- TOTP secret, recovery code'lar `IdentityUserTokens`'da hashed olarak.
- Kullanıcının hangi yöntemleri aktif ettiği `User.EmailConfirmed`, `User.PhoneNumberConfirmed` ve `IdentityUserTokens`'taki `Authenticator` token varlığından türetilir.

## Şifre Politikası
Tek tip politika tüm kullanıcılar için (ASP.NET Core Identity default'undan daha sıkı):
- **Minimum 8 karakter**
- En az 1 büyük harf
- En az 1 küçük harf
- En az 1 rakam
- En az 1 alfa-nümerik olmayan karakter (özel karakter)
- En son 5 şifre tekrar kullanılamaz
- 90 günde bir şifre değiştirme uyarısı (zorunlu değil; ileride sıkılaştırılabilir)

## Login Bildirimi (Yeni Cihaz/IP)
- System kullanıcısı yeni cihaz veya yeni IP'den login olursa e-posta uyarısı gönderilir.
- ManagementApp ve Portal kullanıcıları için opsiyonel (kullanıcı tercihi).
- "Bilinen cihaz" listesi user profile'ında yönetilir; çıkış da yapılabilir.

## IP Whitelist (Opsiyonel)
- System kullanıcıları için tenant düzeyinde değil, **sistem düzeyinde** IP whitelist desteklenir.
- Ofis IP'leri / VPN range'leri config dosyasında.
- Whitelist boş ise tüm IP'lere izin verilir (default).
- Aktif olduğunda whitelist dışı IP'den System login denemesi engellenir + audit'e kayıt.

## Support Mode (System Operatör → Tenant Bağlamı)

### Yaklaşım: Hibrit
- **Default: Context Elevation.** Operatör kendi kimliğini korur, görüntüleme bağlamını hedef tenant'a değiştirir. Salt okunur erişim.
- **Opsiyonel: True Impersonation.** Operatör hedef kullanıcının kimliğine bürünür (`impersonatedBy` etiketi taşır); müşterinin tam görüş açısını görmek için.
- **Yazma yetkisi:** Operatör "Sebep" yazar (zorunlu, min 20 karakter), yazma açılır, audit'e kayıt. **Sade akış** — müşteri anlık onay vermez (mevcut implementasyonda).

### Akış
1. System kullanıcısı ManagementApp gizli `/internal/system` bölümünden tenant seçer.
2. Onay modalı: salt okunur, sebep, devam.
3. Backend: `SupportSession` kaydı oluşturur, yeni JWT (Tenant scope + `isSystemSession=true`, `supportMode=ReadOnly`).
4. UI'da kalıcı banner: "🔧 SUPPORT MODE: Acme Corp — Çık".
5. Opsiyonel "Yazma yetkisi al" → sebep modalı → yeni JWT (`supportMode=WriteEnabled`), banner kırmızı.
6. Opsiyonel "Bir kullanıcı olarak gör" → kullanıcı seç + sebep → True Impersonation JWT (`supportMode=FullImpersonation`).
7. "Çıkış" → SupportSession.EndedAt setlenir; orijinal System JWT'ye geri dönülür.

### SupportSessions Tablosu (Catalog DB)
- `Id` (uuid PK), `UrlCode` (9 karakter, audit linki için)
- `OperatorUserId` (uuid, FK → Users)
- `TargetTenantId` (uuid, FK → Tenants)
- `TargetCompanyId` (uuid, null), `TargetUserId` (uuid, null — impersonation'da)
- `Mode` (smallint enum: `ReadOnly | WriteEnabled | FullImpersonation`)
- `Reason` (text, zorunlu, min 20 karakter)
- `StartedAt`, `EndedAt`
- `WriteActionCount` (int) — oturum içi mutation sayısı
- `CustomerNotified` (bool, gelecekteki müşteri-onay akışı için)

### Şeffaflık (Tenant Admin Görünürlüğü)
- Tenant Admin `/tenant/audit/support-access` sayfasında **tüm Support Session geçmişini** görür: kim, ne zaman, hangi sebeple, hangi modda, kaç write aksiyonu.
- KVKK uyumu için kritik; Faz 1 kapsamında.

## Mobil Uygulama Login Akışı
- Yalnız Management rol'leri varsa → ManagementApp ekranlarına yönlendir.
- Yalnız Portal rol'leri (Malik/Sakin/Kiracı) varsa → PortalApp ekranlarına yönlendir.
- İkisi de varsa → persona seçim ekranı; seçim JWT `personaSide` claim'ine işlenir.
- App içi persona değişimi token yeniler.

## Yetkilendirme Uygulanışı
- MediatR `AuthorizationBehavior` her handler öncesi permission kontrolü.
- Tenant/Company/Unit scope işlemleri: request hedef ID'leri **JWT claim'leriyle eşleşmeli** (defense in depth — JWT iddiası + DB filter aynı sonucu vermeli).
- Hata: standart error code (`AUTH-xxx`).

## API Anahtarları (Service-to-Service)
- Hash'lenmiş saklanır, tenant + permission set scope'lu, süreli, rotate edilebilir, revoke edilebilir.
