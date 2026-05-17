# Faz v0.1 — CHANGELOG

Bu dosya, Faz v0.1 (Temel Altyapı) kapsamında yapılan tüm alt-faz değişikliklerini versiyon damgalı kayıt altına alır. Son giriş en üsttedir.

---

## v0.1.6 — 2026-05-17 — MediatR Pipeline + FluentValidation + Permission Checker

### Mimari Karar: MediatR + Pipeline Behavior'lar
- 14 plain Command/Handler MediatR 11.x'e taşındı; tüm command/query'ler `IRequest<TResponse>` implement eder, handler'lar `IRequestHandler<TRequest, TResponse>` (method adı `Handle`).
- Endpoint'ler artık handler'ı doğrudan inject etmez — `[FromServices] IMediator mediator` + `mediator.Send(command, ct)`.
- MediatR `AddMediatR(assembly)` ile **tüm handler'ları otomatik kayıt** eder; Identity DI'da 14+ manuel handler kaydı silindi.

### Mimari Karar: MediatR 11.x (12.x ÜCRETLİ)
- MediatR 12.0.0'dan itibaren Jimmy Bogard ticari lisans modeline geçti.
- Son ücretsiz (MIT) sürüm **11.1.0** seçildi.
- Aynı şekilde **FluentValidation 11.11.0** (son ücretsiz; 12.x ticari).
- Directory.Packages.props yorumu güncellendi.

### Mimari Karar: Pipeline Sırası
```
1) AuthorizationBehavior  (yetkisiz çağrıyı erkenden reddet → bilgi sızıntısı yok)
2) ValidationBehavior     (input formatını kontrol et; tüm ihlalleri topla)
3) LoggingBehavior        (handler etrafında timing + UserId logla — payload yok)
4) Handler.Handle         (asıl iş)
```
- Hepsi `IServiceCollection.AddTransient(typeof(IPipelineBehavior<,>), typeof(...))` sırasıyla kayıt edildi — MediatR aynı sırada zincirler.

### Application Katmanına Eklenenler
**Common/Authorization:**
- `IPermissionChecker.cs` — Redis session'daki permission listesine karşı kontrol sözleşmesi.
- `RequirePermissionAttribute.cs` — Command/Query'lere ek (OR semantiği, any-of). Şu an handler'lara konmadı; **altyapı hazır**, Faz 1 ManagementApp ile birlikte permission map devreye girince attribute'lar yerleştirilecek.

**Common/Pipeline:**
- `AuthorizationBehavior<TRequest, TResponse>` — `RequirePermission` okur, eksikse `AUTH-PERMISSION-DENIED` (403).
- `ValidationBehavior<TRequest, TResponse>` — Tüm `IValidator<TRequest>`'ları çalıştırır; çoklu ihlal döner.
- `LoggingBehavior<TRequest, TResponse>` — `Information` seviyede `MediatR {Request} user={UserId} elapsed={ms}ms`. Hata durumunda `LogError`.
- `ResultFactoryHelper` — TResponse `Result` veya `Result<T>` için reflection ile `Failure(...)` üretir; behavior'larda tekrar kullanılan helper.

**DependencyInjection.cs (Application):**
- `AddApplicationServices(IServiceCollection)` — tek satırla MediatR + Validators + Behaviors kayıt.

**14 Command/Query → IRequest<TResponse>:**
- LoginCommand, RefreshTokenCommand, LogoutCommand, SwitchContextCommand, LogoutAllSessionsCommand, ForceLogoutUserCommand, RevokeSessionCommand
- EnterSupportModeCommand, ExitSupportModeCommand, ElevateToWriteCommand, ImpersonateUserCommand
- GetSystemSupportSessionsQuery, GetTenantSupportAccessQuery
- VerifyTwoFactorCommand, SendTwoFactorCodeCommand, EnrollTotpCommand, ConfirmTotpEnrollmentCommand, DisableTotpCommand, RegenerateRecoveryCodesCommand, GetTwoFactorMethodsQuery

### Validator'lar (10 yeni)
Inline validation blokları (`if (string.IsNullOrWhiteSpace(...))`) handler'lardan kaldırıldı; her biri Command klasöründe ayrı `XxxCommandValidator.cs` dosyası:
- `LoginCommandValidator` (AUTH-001)
- `RefreshTokenCommandValidator` (AUTH-005)
- `ForceLogoutUserCommandValidator`, `RevokeSessionCommandValidator` (AUTH-012, AUTH-014 — Reason min 20)
- `EnterSupportModeCommandValidator`, `ElevateToWriteCommandValidator`, `ImpersonateUserCommandValidator` (SUP-001, SUP-005, SUP-008 — Reason min 20)
- `VerifyTwoFactorCommandValidator`, `SendTwoFactorCodeCommandValidator`, `ConfirmTotpEnrollmentCommandValidator` (AUTH-2FA-001, -002, -004)

Hata kodları `WithErrorCode(...)` ile korundu — istemcilerin error code katalogu değişmedi.

### Infrastructure
- `SessionPermissionChecker` (Infrastructure.Identity/Authorization) — `IPermissionChecker` Redis session implementasyonu. `ICurrentSessionAccessor.Current.Permissions.Contains(code)`.
- `Identity DI` temizlendi: 14 manuel handler kaydı kaldırıldı; sadece `LoginFinalizer` (yardımcı sınıf, MediatR değil) + `IPermissionChecker` kaldı.

### WebApi
- `AddCleanTenantApi`'ye `services.AddApplicationServices()` eklendi.
- 5 endpoint dosyası (`AuthEndpoints`, `TwoFactorEndpoints`, `SystemEndpoints`, `UserAdminEndpoints`, `TenantAuditEndpoints`) `IMediator.Send` pattern'ına geçti. **Response shape değişmedi** — istemciler için breaking change yok.

### Persistence Csproj (devam)
- `Microsoft.AspNetCore.App` framework reference (v0.1.5.c'de eklenmişti) korundu — `AddDefaultTokenProviders` zincirinin DataProtection bağımlılığı için.

### Eklenen Test'ler (16 yeni Application unit testi)
**Validators/:**
- `LoginCommandValidatorTests` (4 test) — boş alanlar + tek/çoklu hata.
- `EnterSupportModeCommandValidatorTests` (5 test) — Guid.Empty + kısa sebep theory + geçerli akış.

**Pipeline/:**
- `ValidationBehaviorTests` (4 test) — validator yokken pass-through, çoklu hata toplama, geçerli akış, default error code.
- `AuthorizationBehaviorTests` (3 test) — attribute yok pass-through, permission OK pass-through, permission yok 403.

### Doğrulama
- ✓ `dotnet build` — 17 proje / 0 uyarı / 0 hata.
- ✓ `dotnet test` — **142 test başarılı** (17 Application + 70 Domain + 21 Infrastructure + 34 WebApi).
- ✓ Mevcut **126 entegrasyon testi davranış olarak korundu** — refactor sırasında hiçbir endpoint cevabı değişmedi.

### Açık Konular (v0.1.7 / Faz 1'de)
- **`[RequirePermission]` handler'lara henüz konmadı** — Faz 0'da permission rol-map'i yok; Faz 1 ManagementApp "Rol Yönetimi" ekranı ile birlikte attribute'lar handler'lara serpilecek.
- **AND semantiği permission** (`[RequirePermissionAll(...)]`) — şu an OR yeterli; gerek olursa Faz 1+'da eklenecek.
- **LoggingBehavior payload-aware audit** — v0.1.7 audit interceptor ile PII-aware payload kaydı.
- **MediatR Notification (event)** — şu an yalnız Request/Response pattern; Faz 1+'da event bus için Notification'lar eklenebilir.

### Sonraki Adım
**v0.1.7 — Audit Interceptor + Serilog + Log/Audit DB:** SaveChangesInterceptor altyapısı genişletilir; her DB write ayrı audit DB'ye log'lanır; Serilog Log DB'ye yazar; Support Mode `WriteActionCount` artırımı bu interceptor üzerinden olur.

---

## v0.1.5.c — 2026-05-17 — 2FA İskeleti (TOTP + E-posta + SMS + Recovery)

### Mimari Karar: Login Response Polimorfizmi
- `LoginResult { Status, Tokens?, Challenge? }` ile istemci tek tipi deserialize eder.
- `Status="Success"` → mevcut `TokenPair`; `Status="TwoFactorRequired"` → `TwoFactorChallengeResponse` (token + TTL + aktif yöntemler).
- HTTP status hep 200 — 2FA challenge'da bile (REST'çe değil ama istemci dallanması basit).

### Mimari Karar: Challenge Store
- 5 dk TTL'li Redis store (`ct:2fa:challenge:{token}`) — server-side revocable.
- Verify'da kullanılır kullanılmaz silinir (replay engelleme).
- TwoFactorChallenge taşır: UserId, Persona, ContextId, IP, UserAgent, IssuedAt, AvailableMethods.

### Mimari Karar: System Kullanıcıları İçin 2FA Zorunlu
- LoginCommandHandler: kullanıcının System scope rolü varsa **ve** `TwoFactorEnabled=false` ise → `AUTH-2FA-ENROLLMENT-REQUIRED` (422).
- En az bir yöntem yeterli (TOTP / E-posta / SMS). Tüm yöntemleri kapatma denenirse son yöntem System kullanıcısında reddedilir (`AUTH-2FA-LAST-METHOD-LOCK`).

### Identity Wiring
- `AddIdentityCore<User>` zincirine `AddDefaultTokenProviders()` eklendi → 4 sağlayıcı kayıt edildi: TOTP (`Authenticator`), E-posta, SMS (Phone), DataProtector.
- `Microsoft.AspNetCore.App` framework reference Persistence projesine eklendi (AddDefaultTokenProviders bu paketteki extension method'a bağımlı).
- `Microsoft.Extensions.Caching.Memory` + `Configuration.EnvironmentVariables` PackageReference'ları kaldırıldı (artık FrameworkReference'tan geliyor).

### Sender Soyutlamaları
- `IEmailSender` + `ISmsSender` (`Application/Common/Notifications/`) — generic, sadece 2FA için değil; Faz 1'de email confirmation, password reset gibi senaryolarda da kullanılacak.
- `ConsoleEmailSender` + `ConsoleSmsSender` — log'a yazar, gerçek sağlayıcı v0.1.5.c kapsamında yok.
- **Production guard:** `AddCleanTenantNotifications(IConfiguration, IHostEnvironment)` — Production'da `Email:Provider=Console` veya `Sms:Provider=Console` → `InvalidOperationException`. Sessiz hata yok.

### Application Katmanına Eklenenler (16 yeni dosya)
**Common/Auth:**
- `LoginResult.cs` — polimorfik login sonuç tipi + `LoginStatus` enum + `TwoFactorChallengeResponse`.
- `TwoFactorChallenge.cs` — sunucu tarafı challenge bağlamı.
- `ITwoFactorChallengeStore.cs` — challenge persist abstraction.

**Common/Notifications:**
- `IEmailSender.cs`, `ISmsSender.cs`.

**Features/Auth/Login:**
- `LoginFinalizer.cs` — şifre+(2FA) doğrulamasından sonra ortak finalize akışı (scope seçimi, role+permission yükleme, Redis session + JWT + refresh). Hem `LoginCommandHandler` (2FA'sız) hem `VerifyTwoFactorCommandHandler` bunu çağırır.
- `LoginCommandHandler.cs` — 2FA dallanma + System enrollment kontrolü ile yeniden yazıldı.

**Features/Auth/TwoFactor/** (7 alt-klasör):
- `VerifyTwoFactor/` — Command + Handler. Method "Authenticator"/"Email"/"Phone"/"RecoveryCode"; challenge token tek kullanımlık.
- `SendCode/` — Email/SMS provider'larından kod üretip `IEmailSender`/`ISmsSender` ile gönderir.
- `EnrollTotp/` — `ResetAuthenticatorKeyAsync` + secret + `otpauth://totp/CleanTenant:{email}?secret=...&issuer=CleanTenant&digits=6&period=30` URI.
- `ConfirmTotpEnrollment/` — Kullanıcı kodu doğrulanır, `TwoFactorEnabled=true`, 10 recovery code üretilir.
- `DisableTotp/` — AuthenticatorKey silinir (`SetAuthenticationTokenAsync(..., null)`). Son yöntemse + System ise reddedilir.
- `RegenerateRecoveryCodes/` — `GenerateNewTwoFactorRecoveryCodesAsync(user, 10)` ile yenile.
- `GetTwoFactorMethods/` — durum: enabled + aktif provider listesi + geri kalan recovery code sayısı.

### Infrastructure
- `Infrastructure.Caching/TwoFactor/RedisTwoFactorChallengeStore.cs` — `ITwoFactorChallengeStore` Redis implementasyonu.
- `Infrastructure.Identity/Notifications/ConsoleEmailSender.cs`, `ConsoleSmsSender.cs`.
- `Infrastructure.Identity/DependencyInjection.cs`:
  - `LoginFinalizer` + 7 yeni 2FA handler scoped olarak kayıt edildi.
  - `AddCleanTenantNotifications(IConfiguration, IHostEnvironment)` extension method'u eklendi — Production guard + sender registration.
- `Infrastructure.Caching/DependencyInjection.cs` — `ITwoFactorChallengeStore` scoped kayıt.

### WebApi
- `Endpoints/TwoFactorEndpoints.cs` (yeni dosya, 7 endpoint):
  | Endpoint | Auth | Açıklama |
  |---|---|---|
  | `POST /api/v1/auth/2fa/verify` | Anonim | Challenge token + kod ile login finalize |
  | `POST /api/v1/auth/2fa/send-code` | Anonim | Email/SMS yöntemiyle kod gönderim |
  | `POST /api/v1/auth/2fa/enroll/totp` | Bearer | TOTP enrollment başlat (secret + QR URI) |
  | `POST /api/v1/auth/2fa/enroll/totp/confirm` | Bearer | Kod ile onayla, 10 recovery code döner |
  | `POST /api/v1/auth/2fa/disable/totp` | Bearer | TOTP kapat (son yöntemse System için reddedilir) |
  | `POST /api/v1/auth/2fa/recovery-codes/regenerate` | Bearer | 10 yeni recovery code |
  | `GET  /api/v1/auth/2fa/methods` | Bearer | Aktif yöntemler + recovery code sayısı |
- `Configuration/ServiceCollectionExtensions.cs` — `AddCleanTenantApi` imzası `IHostEnvironment` parametresi alacak şekilde genişledi.
- `Program.cs` — `builder.Environment` da geçirilir.
- `EndpointMappingExtensions.cs` — `MapTwoFactorEndpoints` map'lendi.

### Test Fixture'a Eklenenler
- `WebApiFactoryFixture.SeedAsync` — test admin artık 2FA enrolled olarak seed (System scope kullanıcı; `ResetAuthenticatorKeyAsync` + `SetTwoFactorEnabledAsync(true)`).
- `CreateAuthenticatedClientAsync` — login → challenge → fixture `UserManager.GenerateTwoFactorTokenAsync(admin, "Email")` ile kod üret → `/2fa/verify` → Bearer client.
- **Önemli teknik karar:** Authenticator (TOTP) sağlayıcısı sunucuda kod üretemez (secret yalnız kullanıcı app'inde) — fixture Email yöntemini kullanır; production akışında her iki yöntem de geçerli.
- `Infrastructure.IntegrationTests/Fixtures/PostgresFixture.cs` — `AddDataProtection()` eklendi (DataProtectorTokenProvider bağımlılığı).

### Eklenen Test'ler
**`Auth/LoginTests.Gecerli_credentials_2FA_challenge_donmeli`** — eski "TokenPair dönmeli" testi yeni davranışa göre güncellendi.

**`TwoFactor/TwoFactorTests` (10 yeni test):**
1. `Login_2FA_aktif_kullaniciyi_challenge_a_yonlendirmeli`
2. `Yanlis_2FA_kodu_401_donmeli`
3. `SendCode_email_yontemi_200_donmeli`
4. `SendCode_desteklenmeyen_yontem_403_donmeli`
5. `Bilinmeyen_challenge_token_401_donmeli`
6. `Authenticated_kullanici_GetMethods_durumunu_okuyabilmeli`
7. `Authenticated_kullanici_RegenerateRecoveryCodes_uretebilmeli` (10 benzersiz kod)
8. `EnrollTotp_secret_ve_qrUri_donmeli`
9. `DisableTotp_Email_aktif_iken_200_donmeli` (last-method-lock değil)
10. `Bearer_olmadan_enroll_totp_401_donmeli`

### Doğrulama
- ✓ `dotnet build` — 17 proje / 0 uyarı / 0 hata.
- ✓ `dotnet test` — **126 test başarılı** (70 Domain + 21 Infrastructure + 34 WebApi + 1 placeholder).

### Açık Konular (sonraki alt fazlarda)
- **AuthenticatorTokenProvider sunucu tarafı TOTP üretmez** — bu doğru bir tasarım kararı (secret app'te). Test fixture Email yöntemiyle çalışıyor, gerçek istemci TOTP akışı için kullanıcı kendi authenticator app'inden kod okur.
- **`Email:Provider=Smtp` / `Sms:Provider=Twilio` gerçek implementasyonları** Faz 1'de eklenecek; şu an yalnız Console.
- **Recovery code login akışı (loginsiz)** — recovery code ile login için ayrı bir akış yok; kullanıcı önce şifreyle login olur (challenge alır), sonra `/2fa/verify` method=RecoveryCode ile gönderir. Test edilmedi (UI/UX Faz 1'de).
- **System için TOTP zorunluluğu** — şu an "en az bir yöntem" yeterli (kullanıcı kararıyla bağlı kalındı); ileride güvenlik kuralı sıkılaştırılabilir.
- **2FA login bildirimi (yeni cihaz)** — rules_identity.md "Login Bildirimi" bölümü; v0.1.7 audit interceptor ile devreye girecek.

### Sonraki Adım
**v0.1.6 — MediatR Pipeline + FluentValidation + Permission Checker:** Tüm command/query handler'lar MediatR pipeline'ı altında çalışır, `[Behavior]`'lar (ValidationBehavior, AuthorizationBehavior, LoggingBehavior) eklenir, `IPermissionChecker` Redis session permission'larını sorgular. Inline validation'lar FluentValidator'a taşınır.

---

## v0.1.5.b.2 — 2026-05-17 — Support Mode (Enter / Exit / Elevate / Impersonate)

### Mimari Karar: Yeni Session vs In-Place Mutation
- **Enter / Exit / Impersonate** — JWT'nin `sid` claim'i değişiyor; yeni Redis session + yeni access token üretilir.
- **Elevate (ReadOnly → WriteEnabled)** — JWT yenilenmez; mevcut Redis session in-place mutate edilir. İstemcinin token rotasyonuna ihtiyacı yok.
- Operatörün orijinal System session'ı korunur; `OriginalSessionId` üzerinden Exit'te geri dönülür. Orijinal TTL doldu ise yeniden login zorunlu.
- Impersonation yeni `AuthSession` — JWT'nin `sub`'u hedef kullanıcı, `ImpersonatedBy` operatör. Bürünme yine `IsSystemSession=true` taşır.

### Application Katmanına Eklenenler
**`Common/Auth/AuthSession`** genişledi:
- `OriginalSessionId : Guid?` — exit'te dönülecek session (yalnız Support session'larda dolu).
- `ImpersonatedBy : Guid?` — yalnız `FullImpersonation` modunda dolu.
- `SupportMode` artık in-place yükseltme için `set` (önceden `init`).

**`Common/Auth/IAuthSessionStore.UpdateAsync`** — TTL'i koruyarak (veya yenileyerek) mevcut session'ı yeniden yazan API. Elevate ve Exit handler'ları kullanıyor.

**Yeni Application Feature klasörleri (6):**
- `Features/System/EnterSupportMode/` — Command + Handler. `Reason` ≥ 20 karakter; hedef tenant kontrolü; `SupportSession` DB kaydı (Mode=ReadOnly); yeni Tenant scope Redis session.
- `Features/System/ExitSupportMode/` — Command + Handler. `SupportSession.EndedAt` setlenir; orijinal session yoksa `AUTH/SUP-004` ile geri dönülür; orijinale yeni JWT issue edilir.
- `Features/System/ElevateToWrite/` — Command + Handler. Yalnız ReadOnly modunda iken çağrılabilir; SupportSession.Mode → WriteEnabled, session.SupportMode in-place "WriteEnabled".
- `Features/System/ImpersonateUser/` — Command + Handler. Hedef kullanıcı + hedef tenant'ta aktif atama doğrulaması; `SupportSession.TargetUserId` + Mode=FullImpersonation; operatör session'ı silinir, hedef kullanıcı kimliğinde yeni session yaratılır.
- `Features/System/GetSystemSupportSessions/` — Query + DTO + Handler. Cross-tenant denetim listesi; operatör URL kodu / tenant URL kodu / tarih aralığı + sayfalama (max 100).
- `Features/Tenant/GetTenantSupportAccessHistory/` — Query + DTO + Handler. Tenant Admin için yalnız kendi tenant'ında yapılmış destek erişimleri; operatör adı + e-postası join'le zenginleştirilir.

### Infrastructure.Caching
- `RedisAuthSessionStore.UpdateAsync` — JSON serialize + StringSet (yeni TTL). Elevate ve Exit'te kullanılır; user index dokunulmaz çünkü session sahibi kullanıcı değişmiyor.

### Infrastructure.Identity DI
6 yeni handler `AddIdentityServices` içinde scoped olarak kaydedildi:
`EnterSupportModeCommandHandler`, `ExitSupportModeCommandHandler`, `ElevateToWriteCommandHandler`, `ImpersonateUserCommandHandler`, `GetTenantSupportAccessQueryHandler`, `GetSystemSupportSessionsQueryHandler`.

### WebApi Endpoint'leri

**`SystemEndpoints.cs` (genişletildi):**
| Endpoint | Policy | Açıklama |
|---|---|---|
| `POST /api/v1/system/support/enter` | `SystemScope` | Hedef tenant'a ReadOnly Support Mode giriş; yeni JWT döner |
| `POST /api/v1/system/support/exit` | `SupportModeActive` | Support session sona erer; orijinal session'a yeni JWT |
| `POST /api/v1/system/support/elevate` | `SupportModeActive` | ReadOnly → WriteEnabled; JWT yenilenmez |
| `POST /api/v1/system/support/impersonate` | `SupportModeActive` | Tenant kullanıcısına bürünme; yeni JWT döner |
| `GET /api/v1/system/support-sessions` | `SystemScope` | Cross-tenant destek oturumu listesi (denetim) |

**Yeni dosya `Endpoints/TenantAuditEndpoints.cs`:**
| Endpoint | Policy | Açıklama |
|---|---|---|
| `GET /api/v1/tenant/audit/support-access` | `TenantScope` | Tenant Admin'in kendi tenant'ına yapılan destek erişimleri geçmişi |

`EndpointMappingExtensions.MapCleanTenantEndpoints` yeni grubu mapler.

### Test Fixture'a Eklenenler
- `WebApiFactoryFixture.SeedTenantAsync(string name)` — testlerin bir tenant id'sini kolayca elde edebilmesi için Active + Standard + shared DB tenant yaratan helper.

### Eklenen Integration Test'leri (7)
**`Support/SupportModeTests.cs`:**
1. `Enter_elevate_exit_uctan_uca_basariyla_calismali` — Tam akış: login → enter → audit endpoint'i Tenant scope'la erişilebilir mi → elevate → exit; her aşamada beklenen status + token değişimi.
2. `Enter_kisa_reason_400_donmeli` — Reason < 20 karakter validation hatası.
3. `Enter_bilinmeyen_tenant_404_donmeli` — Olmayan tenant id.
4. `Bearer_olmadan_enter_401_donmeli` — Yetkisiz çağrı.
5. `Aktif_support_olmadan_exit_403_donmeli` — System scope session SupportModeActive policy'yi geçemez.
6. `Aktif_support_olmadan_elevate_403_donmeli` — Aynı policy guard'ın elevate'te de çalışması.
7. `GetSystemSupportSessions_listesi_okunabilmeli` — Liste endpoint'i 200.

### Doğrulama
- ✓ `dotnet build` — 17 proje / 0 uyarı / 0 hata.
- ✓ `dotnet test` — **116 test başarılı** (70 Domain + 21 Infrastructure + 24 WebApi + 1 placeholder); regresyon yok.

### Açık Konular (v0.1.7 / sonra)
- **WriteActionCount artırımı** — şu an her zaman 0 kalıyor; audit interceptor (v0.1.7) Support Mode altında yapılan write komutlarını sayacak.
- **Force-logout / revoke endpoint'lerinden Support session'a ulaşma** — şu an target Support session de revoke edilebilir (sessionId verilirse); ileri "Active Support Mode'da operatör force-logout ederse SupportSession.EndedAt setlensin mi?" politikası v0.1.7 audit ile birlikte düşünülecek.
- **Tenant Admin bildirim akışı** — `CustomerNotified` alanı şu an hep `false`; ileri "Sıkı akış"ta (Faz 1+) bildirim gönderildiğinde true setlenecek.
- **Detaylı multi-user test fixture** — impersonate akışını gerçek bir tenant kullanıcısı ile uçtan uca koşacak senaryo, fixture'ın tenant + tenant kullanıcısı seed'iyle genişletilmesi gerek (v0.1.5.b.3 aday).

### Sonraki Adım
**v0.1.5.c — 2FA İskeleti:** TOTP enrollment + verify (`/auth/2fa/enroll`, `/auth/2fa/verify`), Login akışına ikinci faktör entegrasyonu, `IdentityCore`'a `AddDefaultTokenProviders` wiring'i, `AUTH-2FA-REQUIRED` akışının tamamlanması.

---

## v0.1.5.b.1 — 2026-05-17 — Multi-Scope Login + Switch-Context + Force-Logout

### Mimari Karar: Login Persona Zorunluluğu
- Login endpoint'i artık zorunlu `persona` parametresi alır: `Management` veya `Portal`.
- **Unit kullanıcıları (Malik / Hissedar / Sakin / Kiracı) ManagementApp'ten LOGIN OLAMAZ.**
- **System / Tenant / Company kullanıcıları PortalApp'ten LOGIN OLAMAZ.**
- Persona uyumsuz scope'lar `availableScopes` listesinde gözükmez (saldırı yüzeyi minimuma indi).
- `rules_identity.md` + `project_overview.md` (memory) güncellendi; **v007 snapshot** alındı.

### Multi-Scope Login
**Yeni Application dosyaları:**
- `Common/Auth/PersonaSide.cs` — enum (Management / Portal)
- `Common/Auth/ScopeOption.cs` — record (Level + Tenant/Company/Unit ID + adlar)
- `Common/Auth/ScopeSelector.cs` — persona filter + primary scope seçici (System > Tenant > Company → alfabetik)

**`TokenPair` genişledi:** `CurrentScope` + `AvailableScopes` alanları.

**`AuthSession.PersonaSide`:** string → `PersonaSide` enum.

**`LoginCommandHandler`:** persona ile filter, primary scope seçimi, persona için scope yoksa `Forbidden("AUTH-004")`.

### `POST /api/v1/auth/switch-context`
- `SwitchContextCommand` + Handler
- Persona uyumu zorunlu; hedef scope'ta aktif atama olmalı
- Aynı `contextId` (sekme korunur), yeni `sessionId`, refresh token rotation

### Force-Logout 3 Endpoint
- `POST /api/v1/users/me/sessions/logout-all` — kendi tüm cihazlarımdan çıkış
- `POST /api/v1/users/{userUrlCode}/force-logout` — admin force logout (Reason min 20 char)
- `POST /api/v1/system/sessions/{sessionId}/revoke` — System operatör tek session revoke (Reason min 20 char)

### Policy-Based Authorization
**Yeni Infrastructure.Identity dosyaları:**
- `Authorization/AuthorizationPolicies.cs` — policy isim sabitleri
- `Authorization/SessionRequirements.cs` — 4 `IAuthorizationRequirement`
- `Authorization/SessionRequirementHandlers.cs` — base + 4 concrete handler; aktif `AuthSession`'a göre değerlendirir

**4 policy:** `SystemScope` / `TenantScope` / `SupportModeActive` / `SupportWriteEnabled`.

### WebApi Endpoint Yapısı
Yeni dosyalar:
- `Endpoints/UserAdminEndpoints.cs`
- `Endpoints/SystemEndpoints.cs`
- `AuthEndpoints.cs` genişletildi; `EndpointMappingExtensions.cs` yeni group'ları bağlar

### JSON Enum Serialization
- `JsonStringEnumConverter` Minimal API JSON options'a eklendi — enum'lar request/response'larda string olarak (`"Management"`, `"Tenant"`).

### Eklenen Test'ler
- `LogoutAllSessionsTests` (2 test)
- `SwitchContextTests` (3 test)
- `LoginTests`'e persona alanı eklendi

### Doğrulama
- ✓ `dotnet build` — 17 proje / 0 uyarı / 0 hata
- ✓ `dotnet test` — **109 test başarılı** (70 Domain + 21 Infrastructure + 17 WebApi + 1 placeholder)

### Açık Konular (v0.1.5.b.2'de)
- Support Mode 4 endpoint (enter / exit / elevate / impersonate)
- `SupportSession` yaşam döngüsü yönetimi
- Tenant + System destek erişim geçmişi endpoint'leri
- Detaylı force-logout/revoke integration testleri (multi-user fixture)

### Sonraki Adım
**v0.1.5.b.2 — Support Mode Davranışı:** enter/exit/elevate/impersonate endpoint'leri, `SupportSession` yaşam döngüsü, destek erişim geçmişi listeleme.

---

## v0.1.5.a.2 — 2026-05-17 — VKN + YKN + ManagementApp MudBlazor

### UI Kütüphanesi Kararı: ManagementApp Artık MudBlazor
- **`README.md` + `memory/project_overview.md`**: `ManagementApp` "Blazor Server + Razor Components" → "**Blazor Server + MudBlazor**" güncellendi.
- Gerekçe: PortalApp ile aynı kütüphane kullanmak tema, komponent ve özelleştirme paylaşımını basitleştirir; iki uygulama arası tasarım dili tutarlı kalır.
- Memory snapshot **v006** alındı.

### VKN ve YKN Desteği

**YKN (Yabancı Kimlik Numarası):**
- TCKN field'ında kabul edilir; ayrı tip değil. Mernis checksum algoritması her ikisinde aynı.
- YKN ilk hanesi 9; TCKN ilk hanesi 1-8 (genelde).
- `LoginIdentifier` ve `User.Tckn` dokümantasyonu netleştirildi; "TCKN/YKN" olarak adlandırıldı.

**VKN (Vergi Kimlik Numarası):**
- Yeni alanlar: `User.Vkn` (10 char, unique-when-not-null), `User.VknVerified` (bool).
- DB CHECK constraint: `ck_user_vkn_format` — `vkn IS NULL OR vkn ~ '^[1-9][0-9]{9}$'`.
- Yeni migration: `AddUserVknColumns`.
- `LoginIdentifierType.Vkn` enum değeri eklendi.
- `LoginIdentifier.TryParseVkn`: 10 haneli, sadece rakam, ilk hane 0 değil. Checksum algoritması (Gelir İdaresi standardı tartışmalı) ileride eklenebilir.
- `LoginCommandHandler.ResolveUserAsync` Vkn case'i — `VknVerified=true` şartı.

### Resolve Sırası Düzeltmesi
`5551234567` gibi 10-haneli rakam hem geçerli mobil numara hem VKN format'ı; çelişki için **Phone önce** denenir, sonra VKN:
```
Email → TCKN/YKN (11h+checksum) → Phone (5xx başlangıç) → VKN (10h, mobil değil)
```

### Eklenen Test'ler
- `LoginIdentifierTests.YKN_de_Tckn_tipinde_tespit_edilir` (1 Theory case: `99999999990` — geçerli YKN).
- `LoginIdentifierTests.Gecerli_VKN_tespit_edilir` (2 Theory case).
- `LoginIdentifierTests.Gecersiz_VKN_Vkn_tipinde_kabul_edilmez` (3 Theory case).

### Doğrulama
- ✓ `dotnet build` — 17 proje / 0 uyarı / 0 hata
- ✓ `dotnet test` — **104 test başarılı** (70 Domain + 21 Infrastructure + 12 WebApi + 1 placeholder).
- ✓ Migration `AddUserVknColumns` üretildi.

---

## v0.1.5.a.1 — 2026-05-17 — Program.cs Temizliği + Email/TCKN/Telefon Login

### Program.cs Composition Root Temizliği
Önceki Program.cs ~58 satır (env mapping, conn string, 4 service registration, 3 middleware, endpoint mapping inline). Yeni: **8 satır + tek partial sınıf bildirimi**.

Yeni dosyalar `src/Presentation/CleanTenant.WebApi/Configuration/`:
- `EnvironmentMappingExtensions.cs` — `JWT_*` / `SESSION_*` env değişkenlerini `Jwt:*` / `Session:*` section'larına eşler.
- `ServiceCollectionExtensions.cs` — `AddCleanTenantApi(IConfiguration)`: OpenAPI + Persistence + Redis + Identity.
- `ApplicationBuilderExtensions.cs` — `UseCleanTenantPipeline()`: OpenAPI dev + Auth + SessionLookup + Authorization.

Yeni dosya `src/Presentation/CleanTenant.WebApi/Endpoints/`:
- `HealthEndpoints.cs` — `/health` endpoint'i ayrı dosyada.
- `EndpointMappingExtensions.cs` — `MapCleanTenantEndpoints()`: tüm endpoint gruplarını tek satırla.

Program.cs şimdi:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddCleanTenantEnvironmentMappings();
builder.Services.AddCleanTenantApi(builder.Configuration);
var app = builder.Build();
app.UseCleanTenantPipeline();
app.MapCleanTenantEndpoints();
app.Run();
public partial class Program;
```

### Email / TCKN / Cep Telefonu ile Login

Önceki: yalnız e-posta. Yeni: **3 tip identifier**, server otomatik tespit eder.

**Eklenen Application dosyası:**
- `Common/Auth/LoginIdentifier.cs` (+ `LoginIdentifierType` enum) — identifier tipini tespit + normalize.
  - Email: `@` içerir → trim + lowercase.
  - TCKN: 11 haneli, sadece rakam, **Türkiye TCKN algoritması ile checksum doğrulanmış**.
  - Telefon: `+90...` / `05x...` / `5x...` / boşluk/tire/parantez içeren varyantlar → `+905xxxxxxxxx` formatına normalize.

**Şema değişikliği — `User.Tckn` + `User.TcknVerified`:**
- Yeni kolonlar: `tckn char(11) NULL UNIQUE`, `tckn_verified bool NOT NULL`.
- DB CHECK constraint: `ck_user_tckn_format` — `tckn IS NULL OR tckn ~ '^[0-9]{11}$'`.
- Migration: `AddUserTcknColumns`.

**Login akışı:**
- TCKN ile login → `TcknVerified=true` şartı (güvenlik: doğrulanmamış TCKN kabul edilmez).
- Telefon ile login → `PhoneNumberConfirmed=true` şartı.
- Email ile login → değişmedi (UserManager.FindByEmailAsync).

**Endpoint sözleşmesi değişti:**
```diff
- POST /api/v1/auth/login { "email": "...", "password": "..." }
+ POST /api/v1/auth/login { "identifier": "...", "password": "..." }
```

`identifier` alanı email / TCKN / telefon kabul eder; UI'da tek input "E-posta / TCKN / Telefon".

### Eklenen Test'ler
- `Domain.UnitTests/SharedKernel/Auth/LoginIdentifierTests.cs` — **22 test** (Theory): email/TCKN/telefon tespiti, TCKN algoritması, telefon formatları, geçersiz girdiler.
- `WebApi.IntegrationTests/Auth/LoginTests.cs` — `Taninmayan_identifier_400_donmeli` testi eklendi.

### Doğrulama
- ✓ `dotnet build` — 17 proje / 0 uyarı / 0 hata
- ✓ `dotnet test` — **98 test, hepsi başarılı** (64 Domain + 21 Infrastructure + 12 WebApi + 1 placeholder)
- ✓ Migration `AddUserTcknColumns` üretildi
- ✓ Önceki tüm login/refresh/logout test'leri yeni `identifier` field'ı ile geçti

### Açık Konular
- TCKN doğrulama akışı (TcknVerified'i true yapma) Faz 1 ManagementApp ekranlarıyla yapılacak.
- Telefon doğrulama (PhoneNumberConfirmed) v0.1.5.c (2FA SMS) ile birlikte gelecek.
- Frontend "E-posta / TCKN / Telefon" tek input — Faz 1 UI tasarımı.

---

## v0.1.5.a — 2026-05-17 — JWT + Refresh Token + Login + Redis Session

### Mimari Karar: Hibrit JWT + Redis Session Store

Saf JWT (stateless) yerine **Stripe/GitHub/Auth0 benzeri hibrit** yaklaşım benimsendi:
- JWT thin (~250 byte): yalnız `sub`, `sid`, `ctx`, `iat`, `exp`, `iss`, `aud`.
- Zengin claim'ler (roller, permission'lar, scope) **Redis session'da**.
- Server-side anlık revocation; yetki değişimi anında yansır.

### Eklenen Application Dosyaları (12)
- `Common/Auth/`: `JwtClaimNames`, `JwtSettings`, `SessionSettings`, `TokenPair`, `AuthSession`, `IJwtTokenService`, `IRefreshTokenService`, `IAuthSessionStore`, `ICurrentSessionAccessor`.
- `Features/Auth/Login/`: `LoginCommand`, `LoginCommandHandler` (plain handler; v0.1.6'da MediatR pipeline'a entegre olacak).
- `Features/Auth/Refresh/`: `RefreshTokenCommand`, `RefreshTokenCommandHandler`.
- `Features/Auth/Logout/`: `LogoutCommand`, `LogoutCommandHandler`.

### Eklenen Infrastructure.Caching Dosyaları (4) — Boş AssemblyAnchor yerine dolduruldu
- `RedisSettings.cs` — bağlantı config.
- `Sessions/SessionKeyBuilder.cs` — `ct:session:{sid}` ve `ct:user:{uid}:sessions` anahtarları.
- `Sessions/RedisAuthSessionStore.cs` — StackExchange.Redis ile session CRUD + user index + atomic transaction.
- `DependencyInjection.cs` — `AddRedisCache()`.

### Eklenen Infrastructure.Identity Dosyaları (8) — Boş AssemblyAnchor yerine dolduruldu
- `Jwt/JwtTokenService.cs` — HMAC SHA-256 sign; thin JWT üretimi.
- `Jwt/TokenValidationParametersFactory.cs` — JwtBearer middleware konfigürasyonu.
- `RefreshTokens/RefreshTokenService.cs` — kriptografik token, SHA-256 hash, rotation chain, **replay tespiti** (zincirin tümünü revoke).
- `Context/HttpUserContext.cs` — HTTP scope'unda `IUserContext` + `ICurrentSessionAccessor`; session'dan claim'leri okur.
- `Context/HttpTenantContext.cs` — HTTP scope'unda `ITenantContext`; session'dan scope bilgisini okur.
- `Middleware/SessionLookupMiddleware.cs` — JWT validate sonrası Redis lookup; session yoksa **401 Unauthorized** (revocation).
- `DependencyInjection.cs` — `AddIdentityServices()`; JwtBearer authentication + Identity ile birlikte.

### WebApi
- `Endpoints/AuthEndpoints.cs` — `/api/v1/auth/{login,refresh,logout}` minimal API.
- `Program.cs` — env var mapping (Jwt:*, Session:*), `AddCatalogPersistence` + `AddRedisCache` + `AddIdentityServices`, auth pipeline middleware sıralaması (`UseAuthentication → UseSessionLookup → UseAuthorization`).

### Eklenen Paketler
- `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.7`
- `Microsoft.AspNetCore.Mvc.Testing 10.0.7`
- `Microsoft.IdentityModel.JsonWebTokens 8.5.0`
- `StackExchange.Redis 2.8.16`
- `Microsoft.Extensions.Caching.StackExchangeRedis 10.0.7`
- `Testcontainers.Redis 4.7.0`

### Integration Test'leri (11 yeni)
- `Fixtures/WebApiFactoryFixture.cs` — Testcontainers PostgreSQL 17 + Redis 8; migration + seed.
- `Fixtures/WebApiCollection.cs` — xUnit collection; tüm WebApi test sınıfları tek fixture paylaşır (process env var çakışması engellendi).
- `Auth/LoginTests` (4) — geçerli/yanlış şifre/bilinmeyen user/boş email.
- `Auth/RefreshTokenTests` (4) — rotation, replay tespiti, bilinmeyen, boş.
- `Auth/LogoutTests` (3) — başarılı, revocation sonrası 401, Bearer'sız 401.

### Uçtan Uca Manuel Test (Dev Stack ile)
- ✓ `POST /api/v1/auth/login` → 200 (TokenPair)
- ✓ `redis-cli GET ct:session:{sid}` → JSON session görünür
- ✓ `POST /api/v1/auth/logout` (Bearer) → 200; Redis session silindi
- ✓ Aynı token ile tekrar istek → **HTTP 401** (revocation çalışıyor)
- ✓ `POST /api/v1/auth/refresh` → 200, **aynı sessionId** (session devam etti), yeni refresh token
- ✓ Eski (kullanılmış) refresh token ile tekrar → **HTTP 401** (replay tespit edildi)

### Doğrulama
- ✓ `dotnet build` — 17 proje / 0 uyarı / 0 hata
- ✓ `dotnet test` — **75 test başarılı** (42 Domain + 21 Infrastructure + 11 WebApi + 1 Application placeholder)

### Kararlar ve Trade-Off'lar
- **Plain handler'lar, MediatR v0.1.6'ya bırakıldı** — Command/Handler yapısı korundu, MediatR entegrasyonu pipeline behavior'larla birlikte gelecek.
- **HTTP header'a ASCII-safe makine kodu, Türkçe mesaj body'de** — Kestrel non-ASCII header reddediyor (`ş` 0x015F patlattı); `X-Auth-Failure-Code` header + body'de açıklama.
- **`ICollectionFixture` paralel test izolasyonu** — Test sınıfları process env var'ı paylaştığı için tek fixture instance kullanılıyor.
- **Refresh session devam ediyor (aynı sessionId)** — refresh sırasında session devam, sliding TTL ile yenilenir; yeni JWT eski session'ı referans alır.
- **Multi-scope login v0.1.5.b'de** — şu an ilk aktif rol ataması scope olarak seçiliyor.
- **2FA aktif kullanıcılar v0.1.5.a'da login yapamaz** — `Error.Failure("AUTH-2FA-REQUIRED")`; v0.1.5.c'de doğrulama akışı gelecek.

### Riskler & Açık Konular
- Redis down → tüm auth kilitlenir. Health check `/health/ready`'de Redis bağımlılığı v0.1.5.b/c'de eklenecek.
- `RefreshTokenCommandHandler.HandleAsync` mevcut session'ı bulmak için tüm user session'larını tarıyor (O(N)) — N büyürse refresh token kayıtlarına `SessionId` foreign key eklenebilir.
- `Microsoft.Extensions.Logging.Debug` 10.0.0 → 10.0.7 yükseltildi (Hosting transitive).

### Sonraki Adım
**v0.1.5.b — Multi-Scope + Context Switch + Support Mode:**
- Login response'unda multi-scope seçim (kullanıcı birden çok scope'ta atanmışsa)
- `POST /api/v1/auth/switch-context` — yeni scope ile yeni session
- `POST /api/v1/system/{enter-support-mode, exit-support-mode, elevate-to-write, impersonate-user}`
- `SupportSession` yaşam döngüsü yönetimi
- `IPermissionChecker` (rol değişiminde Redis session güncelleme)

---

## v0.1.4.b — 2026-05-17 — Davranış + Seed + Integration Test

### Eklenen / Güncellenen Dosyalar

**Application:**
- `Common/MultiTenancy/ITenantConnectionFactory.cs` — Hibrit multi-tenancy için connection lookup sözleşmesi.

**Infrastructure.Persistence:**
- `Context/SystemUserContext.cs` — `IUserContext` placeholder (UserId=null; v0.1.5'te HttpContext-bound versiyonla değişir).
- `Context/SystemTenantContext.cs` — `ITenantContext` placeholder (ScopeLevel=System; v0.1.5'te değişir).
- `Interceptors/AuditingInterceptor.cs` — `IEntity` (UUID v7), `IAuditable` (CreatedAt/By, UpdatedAt/By), `ISoftDeletable` (delete→soft delete) otomatik dolduran SaveChangesInterceptor.
- `Interceptors/UrlCodeGeneratingInterceptor.cs` — `IHasUrlCode` entity'lerde otomatik UrlCode üretim + `UrlCodeRegistry`'e satır ekleme + async yolda 5 deneme retry (in-memory çakışma kontrolü).
- `MultiTenancy/TenantConnectionFactory.cs` — Catalog'tan tenant lookup + IMemoryCache (5 dk TTL).
- `Seeding/PermissionCatalog.cs` — 45 permission kodu (modül bazlı kategorize).
- `Seeding/BuiltInRoleCatalog.cs` — 13 built-in rol (System 7 + Tenant 1 + Company 1 + Unit 4).
- `Seeding/CatalogSeeder.cs` — Permission + rol seed orchestrator (idempotent).
- `Seeding/DevSeedData.cs` — Yusuf Gülmez Developer hesabı + "Acme Sites Ltd." demo tenant.
- `Seeding/DemoSeedData.cs` — Demo seeder iskeleti (Faz 1'de zenginleşecek).

**Infrastructure.Persistence/DependencyInjection.cs (güncelleme):**
- `IClock` / `IUrlCodeGenerator` singleton.
- `IUserContext` / `ITenantContext` scoped (placeholder impl).
- Interceptor'lar scoped + DbContext'e bağlanır.
- ASP.NET Core Identity konfigürasyonu (şifre policy: min 8 + complexity).
- Lockout: 5 deneme → 15 dk.
- IMemoryCache + `ITenantConnectionFactory`.
- `CatalogSeeder`, `DevSeedData`, `DemoSeedData` scoped.

**tools/CleanTenant.MigrationRunner/ (YENİ console proje):**
- `Program.cs` — System.CommandLine root + alt komutlar.
- `Infrastructure/HostBuilderFactory.cs` — IHost kurulumu, env-bazlı config.
- `Commands/MigrateCommand.cs` — `migrate --env <Env>` → `dotnet ef database update`.
- `Commands/SeedCommand.cs` — `seed --env <Env>` → Core + ortama özel seed.
- `Commands/InitSystemAdminCommand.cs` — Production bootstrap (interaktif şifre prompt).

**Script güncellemeleri:**
- `scripts/env-seed.ps1` — Gerçek implementasyon: `.env`'i yükler, MigrationRunner `seed` komutunu çağırır.
- `.env.development(.example)` — `SEED_ADMIN_PASSWORD` değişkeni eklendi.

**SharedKernel düzeltmesi:**
- `BaseEntity.Id` artık property initializer ile `Guid.CreateVersion7()` default'a sahip — toplu Add'de IdentityMap çakışması engellendi.

### Eklenen Paketler
- `Microsoft.Extensions.Hosting 10.0.7`
- `Microsoft.Extensions.Configuration.EnvironmentVariables 10.0.7`
- `Microsoft.Extensions.Caching.Memory 10.0.7`
- `System.CommandLine 2.0.0-rc.1`
- `Testcontainers.PostgreSql 4.7.0`
- (`Microsoft.Extensions.Logging.Debug` 10.0.0 → 10.0.7 yükseltildi — Hosting paketi 10.0.7 isteyince çakıştı)

### Integration Test'leri (yeni: 6 sınıf, 20 test)
**`tests/CleanTenant.Infrastructure.IntegrationTests/`:**
- `Fixtures/PostgresFixture.cs` — Testcontainers PostgreSQL 17 container; extension'ları yükler, migration'ları uygular; `IClassFixture<>` ile paylaşılır.
- `Fixtures/NullLoggerProvider.cs` — Test gürültüsünü engelleyen no-op logger.
- `Catalog/AuditingInterceptorTests` (4 test) — Add CreatedAt, UUID v7 üretimi, Update UpdatedAt, soft delete davranışı.
- `Catalog/UrlCodeGeneratingInterceptorTests` (3 test) — Auto-gen UrlCode, UrlCodeRegistry kayıtları, toplu Add'de benzersiz kodlar.
- `Catalog/ConcurrencyTests` (1 test) — xmin → DbUpdateConcurrencyException.
- `Catalog/UserRoleAssignmentScopeTests` (4 test) — CHECK constraint: System/Tenant/Unit kapsam tutarlılığı + geçerli atama.
- `Catalog/SupportSessionReasonTests` (2 test) — Reason min 20 karakter CHECK ihlali ve geçerli durum.
- `Catalog/TurkishSearchAlignmentTests` (7 test, Theory) — PG `unaccent(lower(...))` ↔ .NET `TurkishStringNormalizer.Normalize` birebir hizalama.

### Doğrulama Sonuçları
- ✓ `dotnet build`: **17 proje / 0 uyarı / 0 hata** (MigrationRunner dahil).
- ✓ `dotnet test`: **65 test başarılı / 0 başarısız**:
  - Domain.UnitTests: 42 (SharedKernel + 3 placeholder)
  - Infrastructure.IntegrationTests: 21
  - Application.UnitTests: 1 placeholder
  - WebApi.IntegrationTests: 1 placeholder
- ✓ `env-up + env-migrate + env-seed -Env Development` uçtan uca:
  - 45 permission, 13 built-in rol seed'lendi
  - Yusuf admin (yusuf.gulmez.ai@gmail.com) oluşturuldu, Developer (System) rolü atandı
  - Demo tenant ("Acme Sites Ltd.") oluşturuldu, UrlCode otomatik üretildi
  - `url_code_registry` tablosunda her IHasUrlCode için satır var

### Kararlar ve Trade-Off'lar
- **`BaseEntity.Id = Guid.CreateVersion7()` property initializer** — Toplu Add'lerde EF IdentityMap'in `Guid.Empty` çakışmasını engelledi. Test ile ortaya çıktı, en temiz çözüm.
- **`SystemUserContext` + `SystemTenantContext`** v0.1.4.b'de placeholder — UserId/TenantId null. MigrationRunner ve seed'de uygun davranış (audit alanları null = "sistem yaptı"). v0.1.5'te HttpContext-bound versiyonla değiştirilir.
- **`UrlCodeGeneratingInterceptor` async'te 5 retry, sync'te yok** — Sync yolda DB constraint son güvence; pratik retry probability ~10⁻¹⁵.
- **`AddDefaultTokenProviders` v0.1.5'e ertelendi** — `Microsoft.AspNetCore.Identity` ana paketinde, şu an `IdentityCore` kullanıyoruz; v0.1.5'te 2FA wiring eklenirken devreye girecek.
- **`Microsoft.Extensions.Logging.Debug` 10.0.0 → 10.0.7** — Hosting 10.0.7 transitive dependency olarak istedi; CPM downgrade hatası bu güncellemeyle aşıldı.
- **Permission ile rol arasındaki haritalama** Faz 1'e ertelendi (kullanıcı onayı) — şimdi sadece katalog seed.

### Yeni Suppression'lar (Directory.Build.props NoWarn)
- **CA1848** — LoggerMessage delegate (mikro-performans optimizasyonu; okunabilirlik öncelikli).
- **CA1873** — Pahalı log argümanı uyarısı.
- **CA1862** — String karşılaştırma StringComparison ile (EF LINQ-to-SQL'de pratik değil).

### Riskler & Açık Konular
- AspNet*'ın Identity tabloları paralel duruyor; UserManager bu tablolarla çalışıyor, bizim UserRoleAssignments daha esnek scope-aware atamalarla. v0.1.5'te bu paralelliğin nasıl yönetileceği netleşecek.
- `TenantConnection.ConnectionStringEncrypted` plaintext (v0.1.5'te DataProtection encrypt).
- AddDefaultTokenProviders eksik — 2FA token üretimi v0.1.5'e bağlı.
- Production seed'inde admin oluşmaz; `init-system-admin` CLI ile manuel.

### Sonraki Adım
**v0.1.5 — Identity + JWT + Refresh Token + 2FA İskelet:**
- `IJwtTokenService` (context-bound JWT üretimi; System/Tenant/Company/Unit claim'leri)
- `IRefreshTokenService` (rotating, hashed)
- Auth endpoint'leri: `/auth/login`, `/refresh`, `/logout`, `/switch-context`, `/system/enter-support-mode`, `/system/exit-support-mode`
- 2FA enrollment + verify (TOTP / Email / SMS); `AddDefaultTokenProviders` wiring
- `MediatR AuthorizationBehavior` + `IPermissionChecker` + `[RequirePermission]`
- HttpContext-bound `IUserContext` / `ITenantContext` (SystemUserContext placeholder yerine)

---

## v0.1.4.a — 2026-05-17 — Catalog Şeması + EF Core + İlk Migration

### Eklenen Domain Entity'leri (12)
- **Identity/Tenants:** `Tenant` (aggregate root, IHasUrlCode), `TenantConnection`, `TenantStatus` enum, `BillingTier` enum.
- **Identity/Users:** `User` (IdentityUser<Guid>, IHasUrlCode), `RefreshToken`.
- **Identity/Authorization:** `Role` (IdentityRole<Guid>, IHasUrlCode), `Permission`, `RolePermission` (join), `UserRoleAssignment` (scope tutarlılık CHECK ile).
- **Identity/Support:** `SupportSession` (aggregate root, IHasUrlCode), `SupportSessionMode` enum.

### Eklenen Application Soyutlaması (1)
- `Common/Persistence/ICatalogDbContext.cs` — DbSet'leri expose eden interface; handler'lar concrete DbContext yerine bunu tüketir.

### Eklenen Infrastructure.Persistence Dosyaları (14)
- **Catalog:** `CatalogDbContext.cs` (IdentityDbContext<User, Role, Guid>, ICatalogDbContext), `CatalogDbContextDesignTimeFactory.cs` (EF CLI için).
- **Configurations (10):** Her entity için bir `IEntityTypeConfiguration<T>`.
- **Identifiers:** `UrlCodeRegistry.cs` (Infrastructure-only entity; URL kod çarpışma kontrolü için).
- **EfCoreExtensions.cs:** Tüm entity'lerde tekrar eden `UseXminAsConcurrencyToken()` helper'ı.
- **DependencyInjection.cs:** `AddCatalogPersistence(connectionString)` extension.

### İlk Migration
- `Catalog/Migrations/20260517073050_InitialCatalog.cs` — 16 tablo oluşturur:
  - **Bizim tablolar (snake_case):** `tenants`, `tenant_connections`, `permissions`, `role_permissions`, `user_role_assignments`, `refresh_tokens`, `support_sessions`, `url_code_registry` + `__EFMigrationsHistory`.
  - **Identity standart:** `AspNetUsers` (User entity'miz), `AspNetRoles` (Role entity'miz), `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserRoles`, `AspNetUserTokens`.
- Migration olarak tek seferde uygulandı; rollback ile temiz geri alınır.

### Eklenen Paketler (`Directory.Packages.props`)
- `Microsoft.EntityFrameworkCore 10.0.7`
- `Microsoft.EntityFrameworkCore.Design 10.0.7` (CLI'ya açık)
- `Microsoft.EntityFrameworkCore.Tools 10.0.7`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.7`
- `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0`
- `EFCore.NamingConventions 10.0.0`

### Güncellenen Script'ler
- `scripts/env-migrate.ps1` — gerçek implementasyon: `.env.<env>`'i okuyup `dotnet ef database update` çalıştırır. v0.1.4.a için sadece Catalog DbContext aktif; sonraki alt fazlarda Main/Log/Audit eklenecek.

### Silinen Yer Tutucular
- `Domain/AssemblyAnchor.cs`, `Application/AssemblyAnchor.cs`, `Infrastructure.Persistence/AssemblyAnchor.cs` — gerçek içerikle değiştirildi.

### Yapılan Kural Değişiklikleri (sorun → çözüm)
- `Directory.Build.props` genel `NoWarn`'a **CA1711** eklendi — "Permission", "RolePermission" tip adları sözlük kelimesiyle bitiyor; yanlış pozitif.
- `.editorconfig` Migration klasörü için `generated_code = true` + CA1861/CA1822/CS1591 severity none — EF auto-gen kodu analyzer kurallarından muaf.

### Kararlar ve Trade-Off'lar
- **`Domain` projesinin `Microsoft.AspNetCore.Identity.EntityFrameworkCore` bağımlılığı kabul** — `User : IdentityUser<Guid>` ve `Role : IdentityRole<Guid>` miras alabilmek için. Alternatif (10+ alan manuel yazımı) maliyetli. Saflık trade-off'u belgelendi.
- **`Application` projesinin `Microsoft.EntityFrameworkCore` bağımlılığı kabul** — `ICatalogDbContext` DbSet expose ediyor. Tamamen abstract repository pattern over-engineering olurdu.
- **Tek PG instance, 4 DB** mantıksal ayrım pratikte sorunsuz çalıştı; init script'leri tüm DB'lerde extension'ları kurdu.
- **xmin → uint RowVersion eşlemesi** Npgsql 10'da `UseXminAsConcurrencyToken()` extension method'u yok; manuel `Property("RowVersion").HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()` ile çözüldü, kendi helper'ımıza sarıldı.
- **Identity'nin standart tabloları (AspNet*)** bizim `UserRoleAssignments`'a paralel duruyor — şimdilik kabul; AspNet*'ları sıralı bir gelecek alt fazında ihtiyaç olursa disable edebiliriz.
- **`UserRoleAssignment.scope` tutarlılık** CHECK constraint ile DB seviyesinde dayatıldı; uygulama tek savunma değil.
- **`SupportSession.reason` min 20 karakter** CHECK constraint — operatörün açıklama yazmadan giriş yapması imkansız.

### Doğrulama Sonuçları
- ✓ `dotnet build`: 16 proje / 0 uyarı / 0 hata.
- ✓ `dotnet ef migrations add InitialCatalog` → 3 dosya üretildi (`InitialCatalog.cs`, `.Designer.cs`, `CatalogDbContextModelSnapshot.cs`).
- ✓ `dotnet ef database update` → Catalog DB'de 16 tablo oluştu.
- ✓ Tablo isimleri snake_case (`tenants`, `support_sessions`, `url_code_registry` vb.).
- ✓ `tenants.name` kolon tipi `citext` (case-insensitive); `url_code` `character(9)`.
- ✓ Foreign key'ler aktif (`support_sessions → tenants`, `tenant_connections → tenants` vb.).
- ✓ CHECK constraint: `ck_support_session_reason_minlength CHECK (char_length(reason) >= 20)` doğrulandı.
- ✓ Unique index'ler: `tenants.url_code`, `tenants.name`.
- ✓ `env-up`/`env-migrate`/`env-down` uçtan uca akış çalışıyor.

### Riskler & Açık Konular
- xmin sütunu PG sistem sütunu olduğu için `\d` ile görünmüyor; runtime concurrency test'i v0.1.4.b integration test'lerinde yapılacak.
- Identity'nin `ConcurrencyStamp` string token'ı ile bizim `RowVersion` xmin token'ı paralel duruyor (her ikisi de concurrency için); test'te davranış doğrulanacak.
- AspNet* tablolarının kullanım stratejisi v0.1.5'te netleşecek (Identity tooling'le yönetilir; bizim UserRoleAssignments paralel mekanizma).
- `TenantConnection.ConnectionStringEncrypted` v0.1.4'te plaintext; v0.1.5'te DataProtection API ile encrypt edilecek.

### Sonraki Adım
**v0.1.4.b — Interceptor + Factory + Seed + Test:** AuditingInterceptor (audit alanları otomatik), UrlCodeGeneratingInterceptor (IHasUrlCode → otomatik kod + UrlCodeRegistry'ye kayıt + retry), ITenantConnectionFactory, MigrationRunner console projesi, init-system-admin CLI komutu, PermissionCatalog + BuiltInRoleCatalog seed'leri, Dev/Demo seed verisi, Testcontainers ile integration test'ler.

---

## v0.1.3 — 2026-05-17 — SharedKernel

### Eklenen Üretim Dosyaları (21)

**Common / Errors:**
- `Common/Errors/ErrorType.cs` — enum (None, Validation, NotFound, Conflict, Unauthorized, Forbidden, Failure, Critical).
- `Common/Errors/Error.cs` — sealed record (Code, Message, Type) + factory metotları + `Error.None`.
- `Common/Errors/ErrorCodes.cs` — genel hata kodu kataloğu (GEN-, VAL-).

**Common / Results:**
- `Common/Results/Result.cs` — non-generic Result; invariant ctor (success→errors boş; failure→errors dolu).
- `Common/Results/ResultOfT.cs` — `Result<T>` + örtük dönüşüm operatörleri (value→Success, Error→Failure).
- `Common/Results/ResultExtensions.cs` — Map, Bind, Match (functional composition).

**Entities (interface'ler + base class):**
- `Entities/IEntity.cs` — marker (Guid Id).
- `Entities/IAggregateRoot.cs` — DDD aggregate root marker.
- `Entities/IAuditable.cs` — CreatedAt/By, UpdatedAt/By.
- `Entities/ISoftDeletable.cs` — IsDeleted, DeletedAt/By.
- `Entities/ITenantScoped.cs` — TenantId.
- `Entities/IHasUrlCode.cs` — **opt-in** marker; URL'de görünmesi gereken entity'ler implement eder.
- `Entities/BaseEntity.cs` — abstract; Id, audit alanları, soft delete, RowVersion (xmin). UrlCode ve TenantId taşımaz.

**Identifiers:**
- `Identifiers/IUrlCodeGenerator.cs` — sözleşme.
- `Identifiers/Base58UrlCodeGenerator.cs` — 9 karakter, Base58 alfabesi (0/O/I/l hariç), GUID'den modulo.

**Context:**
- `Context/ScopeLevel.cs` — enum (None, System, Tenant, Company, Unit).
- `Context/ITenantContext.cs` — TenantId/CompanyId/UnitId/CurrentScope.
- `Context/IUserContext.cs` — UserId, UserName, Email, IsAuthenticated, Roles, Permissions.

**Time:**
- `Time/IClock.cs` — DateTimeOffset UtcNow.
- `Time/SystemClock.cs` — production implementasyonu.

**Localization:**
- `Localization/TurkishStringNormalizer.cs` — static; Normalize (tr-TR lowercase + aksan kaldırma), TurkishLower (sadece lowercase). PG `unaccent(lower(...))` ile hizalı.

### Eklenen Test Dosyaları (6)
- `SharedKernel/Common/Errors/ErrorTests.cs` — 9 test (her factory + None + record equality).
- `SharedKernel/Common/Results/ResultTests.cs` — 4 test (Success/Failure/FirstError).
- `SharedKernel/Common/Results/ResultOfTTests.cs` — 8 test (Success/Failure/implicit/Map/Bind/Match).
- `SharedKernel/Identifiers/Base58UrlCodeGeneratorTests.cs` — 4 test (uzunluk, alfabe, yasaklı karakter, 1000 üretimde benzersizlik).
- `SharedKernel/Time/SystemClockTests.cs` — 2 test (gerçek UTC + NSubstitute mock).
- `SharedKernel/Localization/TurkishStringNormalizerTests.cs` — 15 test (TR + DE karakterler, idempotent, null/empty).

### Paket Eklemeleri
- `FluentAssertions 6.12.2` — son Apache-2.0 sürüm (v8+ ücretli).
- `NSubstitute 5.3.0` — mock.
- `Domain.UnitTests` csproj → SharedKernel project referansı + `Using FluentAssertions` + `Using NSubstitute`.

### Yapılan Kural Değişiklikleri (sorun → çözüm)
- `.editorconfig`: `dotnet_style_require_accessibility_modifiers = for_non_interface_members:warning` (interface üyeleri implicit public; explicit yazmak gereksiz).
- `Directory.Build.props` (genel `NoWarn`):
  - **CA1000** — generic'te static method yasak (Microsoft'un kendi `Task<T>.FromResult`'ı da bu deseni kullanıyor; Result pattern için zorunlu).
  - **CA1716** — VB.NET keyword'ü ile çakışma ("Error"); biz yalnız C# kullanıyoruz.
- `Directory.Build.props` (test projeleri `NoWarn`):
  - **CA1707** — alt çizgili method isimleri (test naming convention: `When_X_then_Y`).
  - **CA1515** — internal yerine private.

### Build & Test Doğrulaması
- ✓ `dotnet build`: **16 proje / 0 uyarı / 0 hata**.
- ✓ `dotnet test`: **42 SharedKernel testi + 3 placeholder = 45 test, hepsi başarılı**.
- ✓ Türkçe karakter normalize: `İSTANBUL → istanbul`, `Şişli → sisli`, `Çankaya → cankaya`, `Ğümüşhane → gumushane`, `Öztürk → ozturk`, `Üsküdar → uskudar`.
- ✓ Almanca aksan normalize: `Müller → muller`, `Bäcker → backer`.
- ✓ Base58UrlCodeGenerator 1000 üretimde benzersiz; sadece izinli alfabe.
- ✓ NSubstitute ile `IClock` mock'lanabiliyor.

### Kararlar ve Trade-Off'lar
- **`UrlCode` opt-in (IHasUrlCode marker)** — BaseEntity'den çıkarıldı. Yüksek hacimli tablolarda (audit, log, line item, refresh token) disk + indeks tasarrufu; DDD anlamı netleşti (yalnız URL'de adreslenebilir kaynaklar taşır).
- **`Result.Failure` invariant'ı ctor'da zorlanıyor** — başarısızlık en az bir hata içermeli, aksi InvalidOperationException. Sessiz hata-yutmayı engeller.
- **`Result<T>` implicit operatörleri** — `Result<User> r = user;` ve `Result<User> r = Error.NotFound(...);` desenleri kabul; handler kodunda kontrol akışı yalın.
- **`TurkishStringNormalizer.Normalize` deterministic + PG-aligned** — DB ve .NET tarafı arama sonuçlarının ayrışmasını engeller; gerçek PG hizalama doğrulaması v0.1.4'te tablolar oluşturulduğunda yapılacak.
- **`BaseEntity.RowVersion = uint`** — PostgreSQL `xmin` sistem sütununa birebir uyar (32-bit unsigned); EF Core mapping v0.1.4'te yapılacak.
- **FluentAssertions 6.12.2 pin** — v8 ücretli, v7 son ücretsiz değil (FluentValidation 11.x'e benzer durum).

### Riskler & Açık Konular
- `ITenantContext` ve `IUserContext` concrete implementasyonları **v0.1.5 Identity** alt-fazına bağlı; şimdilik interface'ler tanımlı, kullanılmıyor.
- `BaseEntity`'nin EF Core mapping'i (özellikle `RowVersion → xmin`, `UrlCode unique index`, `Id → uuid v7`) **v0.1.4 Catalog DB**'de yapılacak.
- TurkishStringNormalizer'ın PG `unaccent(lower(...))` ile birebir hizası **v0.1.4**'te gerçek DB'ye karşı doğrulanacak; sapma olursa AccentMap güncellenir.

### Sonraki Adım
**v0.1.4 — Catalog DB + Tenant Registry + Identity Şeması:** `CatalogDbContext`, ilk EF Core migration, `Tenants/Users/Roles/Permissions/RolePermissions/UserRoleAssignments/RefreshTokens/UrlCodes` tabloları, BaseEntity'nin EF Core mapping'i, `ITenantConnectionFactory` taslağı, dev/demo seed (System rol katalogu + ilk admin).

---

## v0.1.2 — 2026-05-17 — Docker Compose + 4 Ortam Yapısı

### Eklenen Dosyalar
- **Compose dosyaları (5):**
  - `compose/docker-compose.yml` — Base (postgres + redis).
  - `compose/docker-compose.development.yml` — Dev override (+ Seq, port'lar açık).
  - `compose/docker-compose.test.yml` — Test override (Seq yok, port 55xx).
  - `compose/docker-compose.demo.yml` — Demo override (+ Seq, resource limit).
  - `compose/docker-compose.production.yml` — Prod override (port expose yok, SSL connection string'leri, resource limit, logging driver).
- **PostgreSQL init scripts (2, LF EOL):**
  - `compose/postgres-init/01-init-databases.sh` — 4 DB oluşturur (catalog/main/log/audit).
  - `compose/postgres-init/02-extensions.sh` — Her DB'ye `citext`, `unaccent`, `pg_trgm`, `pgcrypto` extension'larını kurar.
- **Environment şablonları (4):**
  - `.env.development.example`, `.env.test.example`, `.env.demo.example`, `.env.production.example`
  - Postgres bağlantı string'leri, Redis, Seq, JWT placeholder'ları içerir; sırlar `__DEGISTIR__` ile işaretli.
  - `.env.production.example` Vault'a göç notu içerir; SSL Mode=Require zorunlu.
- **PowerShell + Bash script'leri (5 çift):**
  - `env-up`, `env-down`, `env-reset` — fonksiyonel.
  - `env-migrate`, `env-seed` — iskelet (Faz v0.1.4'te dolacak).
  - PowerShell varyantları `ValidateSet` ile Development/Test/Demo/Production doğrulamasına sahip.
  - `env-reset` PRODUCTION'da çift onay + ortam adı yazma şartı; `--Force` ile bypass.

### Servis Yapısı

| Servis | Image | Dev | Test | Demo | Prod |
|---|---|---|---|---|---|
| postgres | postgres:17-alpine | :5432 | :5532 | :5632 | internal |
| redis | redis:8-alpine | :6379 | :6479 | :6579 | internal |
| seq | datalust/seq:latest | :5341 | — | :5541 | — |

### Doğrulama Sonuçları (Development ortamında uçtan uca)
- ✓ `env-up.ps1 -Env Development` → 3 servis ayakta, health-check'ler yeşil.
- ✓ 4 veritabanı oluşturuldu: `cleantenant_catalog`, `cleantenant_main`, `cleantenant_log`, `cleantenant_audit`.
- ✓ Her DB'de 4 extension yüklü: `citext` 1.6, `pg_trgm` 1.6, `pgcrypto` 1.3, `unaccent` 1.1.
- ✓ Türkçe karakter normalize testi: `unaccent(lower('İSTANBUL'))` = `istanbul`, `Şişli` → `sisli`, `Çankaya` → `cankaya`.
- ✓ Redis PING → PONG.
- ✓ Seq `/health` → HTTP 200 `{"status":"healthy"}`.
- ✓ `env-down.ps1 -Env Development` → temiz iniş, volume'lar korundu.

### Kararlar ve Trade-Off'lar
- **Tek PostgreSQL container, 4 DB (logical separation).** Geliştirme makinesinde 4 PG instance yerine 1 instance + 4 DB; kaynak tasarrufu + bağlantı string'i bütünlüğü. Prod'da ops kararıyla ayrılabilir.
- **Compose dosya isimleri tam env adıyla** (`docker-compose.development.yml` vs `.dev.yml`). İlk denemede tutarsızlık çıktı, düzeltildi — `.env.<env>` ile aynı naming kullanıldı.
- **Postgres 17 + Redis 8 (en son major'lar).** Dev/prod arası sürüm sürprizi engelleme.
- **Port aralıkları her ortam için farklı** (Dev 54xx, Test 55xx, Demo 56xx, Prod 57xx). Aynı host'ta paralel çalışmaya olanak.
- **`.env.production.example` Vault göç notuyla.** İlk deploy `.env.production` köprü modu (chmod 600); Faz 2-3'te Vault entegrasyonu planlandı.
- **Seq hash STDIN'den okunur** (`echo password | docker run -i ... config hash`); .env.*.example dosyasında bu komut yorum satırı olarak yazıldı.
- **Bash script'leri LF EOL'a normalize edildi** (Windows CRLF Linux container'da `\r` token hatası verirdi). `.gitattributes` zaten `*.sh text eol=lf` kuralını içeriyor.

### Riskler & Açık Konular
- `env-migrate` ve `env-seed` iskelet; v0.1.4'te EF Core DbContext'leri tanımlandığında doldurulacak.
- Per-tenant dedicated DB (hibrit multi-tenancy) henüz yok; ihtiyaç doğdukça (Faz 1+) ek compose / runtime container'lar eklenecek.
- Bash script'leri Windows tarafında yalnız WSL veya Git Bash ile çalışır (kullanıcı kendi tercihiyle); PowerShell varyantları birinci sınıf.

### Sonraki Adım
**v0.1.3 — SharedKernel:** `Result`, `BaseEntity`, `IUrlCodeGenerator` (Base58), `ITenantContext`, `IClock`, `TurkishStringNormalizer` primitif'leri.

---

## v0.1.1 — 2026-05-17 — Solution & Proje İskeleti

### Eklenen Dosyalar / Yapı
- `CleanTenant.sln` — Solution Folder organizasyonlu (src/Core, src/Infrastructure, src/Presentation, tests).
- 16 proje oluşturuldu:
  - **Core (3):** SharedKernel, Domain, Application
  - **Infrastructure (5):** Persistence, Identity, Logging, Caching, BackgroundJobs
  - **Presentation (4):** WebApi (Minimal API), ManagementApp (Blazor Server), PortalApp (Blazor Server), MobilApp (MAUI Blazor Hybrid)
  - **Tests (4):** Domain.UnitTests, Application.UnitTests, Infrastructure.IntegrationTests, WebApi.IntegrationTests
- `global.json` — SDK 10.0.203 stable pin'lendi; `allowPrerelease: false`.
- `Directory.Build.props` — Ortak derleme ayarları (TargetFramework, Nullable, TreatWarningsAsErrors, GenerateDocumentationFile, AnalysisMode=Recommended).
- `Directory.Packages.props` — Central Package Management aktif; ASP.NET Core OpenAPI, xUnit test çatısı, MAUI paketleri merkezi sürümlendirildi.
- `.editorconfig` — Türkçe ortam için kapsamlı C# stil ve isimlendirme kuralları.
- `.gitignore`, `.gitattributes` — .NET / Visual Studio / Rider / VS Code uyumlu.
- `README.md` — Kök seviyede Türkçe proje tanıtım dokümanı.
- Her classlib'e Türkçe XML doc'lu `AssemblyAnchor.cs` placeholder eklendi.
- WebApi `Program.cs` — `WeatherForecast` şablon kalıntıları temizlendi; `/health` endpoint'i eklendi; `WebApplicationFactory<Program>` için `public partial class Program` bildirimi yapıldı.
- Tüm test projelerinde Türkçe XML doc'lu `PlaceholderTests.cs` (xUnit runner doğrulayıcı).

### Bağımlılık Grafı (kurulan referanslar)
```
SharedKernel  ←  Domain  ←  Application  ←  Infrastructure.*
                                         ←  WebApi
                                         ←  ManagementApp
                                         ←  PortalApp
                          ←  Domain.UnitTests
                                            ←  Application.UnitTests
                                            ←  Infrastructure.IntegrationTests (tüm Infra'lar)
                                            ←  WebApi.IntegrationTests
```
- MobilApp **hiçbir Core veya Infrastructure projesine referans vermez**. Yalnız HTTP üzerinden WebApi ile konuşacaktır.

### Kararlar ve Trade-Off'lar
- **SDK pin'leme (`global.json`):** Sistemde `10.0.300-preview.0` default'tu; stable `10.0.203` (feature band 2) pin'lendi. `rollForward: latestPatch` ile gelecek patch'ler otomatik kabul edilir, preview'ler reddedilir.
- **`var` kullanım kuralı gevşetildi:** .editorconfig'de başlangıçta `csharp_style_var_elsewhere = false:warning` idi; .NET 10 minimal API idiomatic olarak `var` kullandığından (template kodları zaten `var` ile geliyor) kural `true:silent` (zorlamasız izin) olarak güncellendi.
- **MAUI namespace çakışması:** `CleanTenant.Application` (Core katman) ile `Microsoft.Maui.Controls.Application` (MAUI base class) çakıştığı için MobilApp'in Application referansı kaldırıldı. Bu aslında **doğru mimari karar** — mobil istemci backend ile HTTP üzerinden konuşmalı; Application'a doğrudan bağlanmamalı. Paylaşılan DTO ihtiyacı Faz 1'de OpenAPI generate (NSwag/Refit) veya ayrı `CleanTenant.Contracts` projesi ile karşılanacak.
- **MAUI için XML doc + bazı strict kurallar gevşetildi:** MAUI şablonu çok sayıda platform-özel boilerplate dosya üretiyor (AppDelegate, MauiProgram, Program.cs per platform). MobilApp özelinde `GenerateDocumentationFile=false` ve `NoWarn=CA1711;IDE0040` eklendi. Faz 1'de iş kodu yazıldığında bu istisnalar yeniden değerlendirilecek.

### Build & Test Doğrulaması
- `dotnet --version`: `10.0.203` ✓
- `dotnet build`: **16 proje / 0 uyarı / 0 hata** ✓
- `dotnet test`: **4 test projesi / 4 test başarılı / 0 başarısız** ✓
- MAUI 4 hedef framework'te (android, iOS, MacCatalyst, Windows) build başarılı ✓

### Riskler & Açık Konular
- **MAUI workload kaynağı `SDK 10.0.300-preview.0`** olarak görünüyor. Build çalışıyor ama uzun vadede `dotnet workload update` ile stable kaynağa geçirilmesi önerilir. Sorun çıkarsa hemen müdahale edilecek.
- Test projeleri henüz `FluentAssertions`, `NSubstitute`, `Testcontainers` gibi gerçek test araçlarını içermiyor — Faz v0.1.10'da eklenecek.
- Solution dışı klasörler (`compose/`, `scripts/`, `.github/`) henüz boş — sonraki alt fazlarda dolacak.

### Sonraki Adım
**v0.1.2 — Docker Compose + 4 Ortam Yapısı:** PostgreSQL × 4 + Redis + Seq Docker stack'i, `env-up/down/reset` script'leri, `.env.<env>.example` dosyaları.
