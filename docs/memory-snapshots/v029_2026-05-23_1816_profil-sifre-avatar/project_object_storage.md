---
name: object-storage-altyap-s-minio-profil-sayfas
description: "v0.2.13 — IFileStorage/IImageProcessor soyutlaması + MinioFileStorage + /profile sayfası (foto, 2FA, dil)"
metadata: 
  node_type: memory
  type: project
  originSessionId: 23639f84-632d-43fd-acca-ba3a39cb2b76
---

`feature/system-context-and-company-users` branch'inde v0.2.13 olarak eklendi (henüz commit edilmedi). [[project_current_state]] FAZ 6 Tahakkuk hattından ayrı bir iş parçası.

## Object Storage (yeniden kullanılabilir)
- Yeni proje: `src/Infrastructure/CleanTenant.Infrastructure.Storage` (slnx + WebApi + ManagementApp referansları).
- Soyutlamalar `Application/Common/Storage`: `IFileStorage` (Upload/Get/Delete/Exists), `IImageProcessor` (`ToSquarePngAsync`), `ProfilePhotoPolicy` (100px, 4MB, izinli MIME, `avatars/{userId:N}.png`).
- Implementasyonlar: `MinioFileStorage` (Minio 7.0.0 SDK), `InMemoryFileStorage` (endpoint yoksa + Production değilse fallback → integration testleri MinIO'suz boot eder), `SkiaImageProcessor`.
- **Görsel kütüphanesi kararı:** SixLabors.ImageSharp DEĞİL → SkiaSharp 3.119.2 (MIT). ImageSharp 3.x ücretli "Split License", 2.1.x ise NU1902/NU1903 güvenlik açıklı (TreatWarningsAsErrors build'i kırar). İleride fatura/ek dosyaları da bu altyapıyı kullanır.
- Config: `ObjectStorage:*` (Endpoint/AccessKey/SecretKey/UseSsl/Bucket/CreateBucketIfMissing). `.env.development`'a eklendi; bucket `cleantenant-files`. Docker: `compose/docker-compose.yml`'e `minio` servisi + dev override 9000/9001 portları. `MinioBucketInitializer` startup'ta bucket'ı best-effort oluşturur.
- DI: `AddObjectStorage(configuration, environment)` — WebApi `ServiceCollectionExtensions` + ManagementApp `Program.cs`.

## /profile sayfası (ManagementApp, Blazor)
- Tek `/profile` + MudTabs 3 sekme: Genel & Foto, Güvenlik (2FA), Dil & Tercihler. `ProfileSecurityPanel.razor` ayrı bileşen.
- Eski `/settings/language` ve `/settings/2fa/enroll` → `/profile`'a redirect. AppBar/NavMenu linkleri güncellendi.
- Foto: `UploadProfilePhotoCommand`/`RemoveProfilePhotoCommand`/`GetProfilePhotoQuery` (base64 data-uri render), `User.ProfilePhotoKey`+`ProfilePhotoUpdatedAt` (Catalog migration `AddUserProfilePhoto`).
- 2FA: `SetTwoFactorEnabledCommand` (≥1 doğrulanmış yöntem şart; System scope kapatamaz; **System dışı kullanıcı pasife alırken hesap şifresi `CheckPasswordAsync` ile doğrulanır** — `PasswordPromptDialog`), Email/Phone self-servis doğrulama (`EmailMethod`/`PhoneMethod` klasörleri), `RemoveTwoFactorMethodCommand`. **Recovery kodu 10→12** (`TwoFactorDefaults.RecoveryCodeCount`). `GetTwoFactorMethods` sonucu UI durumlarıyla genişletildi.
- AppBar avatar: `ProfileAvatarState` (Scoped/circuit) — foto yüklenince/silinince MainLayout sağ üst avatar'ı anında güncellenir; `MudMenu ActivatorContent` ile MudAvatar (foto veya AccountCircle ikonu).
- Dil: yeni komut YOK — mevcut `/auth/change-culture` form-post + `cleantenant.submitCultureChange` JS yeniden kullanıldı (cookie + PreferredCulture persist + reload).

**Why:** Object storage projede ilk kez geldi; mimari bir genişleme. Görsel kütüphanesi seçimi lisans+güvenlik kapısıyla zorlandı (SkiaSharp).

**How to apply:** Çalıştırmadan önce `env-up` ile MinIO container'ı kalkmalı + `env-migrate` ile `AddUserProfilePhoto` uygulanmalı. Dosya saklama gerektiren yeni iş (fatura PDF/ek) için `IFileStorage` kullan, yeni soyutlama yazma. [[rules_environments]] [[feedback_build_kill_first]]

## Bilinen pre-existing kırık (benden değil)
WebApi integration suite bu branch'te kırık: WebApi DI `AddCleanTenantExport()` çağırmıyor (`IBuildingSchemaExcelService` çözülemiyor, commit `58ef0be`) + fixture Audit/Main/Log connection ayarlamıyor → `ValidateOnBuild` patlıyor. Ayrıca 2 VKN validator unit testi (`UpdateTenant`/`CreateTenant`) sıfır-prefix kural değişikliğiyle senkron değil. Profil işiyle ilgisiz.
