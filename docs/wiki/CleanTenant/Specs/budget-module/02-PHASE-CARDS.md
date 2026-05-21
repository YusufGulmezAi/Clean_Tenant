# 02 — Bu Sprint'in İş Paketleri (Faz Kartları)

> **Bu sprint'in scope'u:** Yapı Şeması + Bütçe MVP + Tahakkuk MVP.
> **Diğer her şey out-of-scope.** `00-CONTEXT.md` Bölüm 7'deki yasak listeye uy.

Her faz kartı: **Hedef → Acceptance Criteria → Out-of-Scope → Çıktılar → Effort → Bağımlılıklar.**

---

## FAZ 1 — Yapı Şeması Domain Modeli

**Hedef:** Ada/Parsel/Yapı/Blok/BB hiyerarşisini Domain katmanında eksiksiz modelle. Türk tapu sistemine uygun.

**Acceptance Criteria:**
- [ ] `Ada`, `Parsel`, `Yapi`, `Blok`, `BagimsizBolum`, `BagimsizBolumTipi`, `YapiTipi` entity'leri Domain katmanında tanımlanmış.
- [ ] `Site → Ada → Parsel → Yapi → (opsiyonel) Blok → BB` ilişkisi navigation property ile kuruluyor.
- [ ] Her entity'de `Id (Guid)`, `TenantId (Guid)`, `ShortCode (string 8)`, `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` standart alanları var.
- [ ] BB'nin **opsiyonel** Blok ilişkisi var: bir BB ya bir Bloka bağlı (apartman) ya da doğrudan Yapı'ya bağlı (villa tipi).
- [ ] `YapiTipi` enum veya lookup tablo: `Apartman, Villa, Dukkan, Ofis, AVM, IsMerkezi, SosyalTesis, Marina, Karma`.
- [ ] `BagimsizBolumTipi` enum veya lookup tablo: `Konut, Dukkan, Ofis, Depo, Otopark, Diger`.
- [ ] Standart BB öznitelikleri: `BrutMetrekare (decimal 10,2)`, `NetMetrekare (decimal 10,2)`, `ArsaPayi (decimal 10,4)`, `OdaSayisi (int)`, `KapiNo (string 20)`.
- [ ] Aggregate root: `Site` (Company). Ada/Parsel/Yapı/Blok/BB onun altında.
- [ ] Domain validation: BB'nin Blok'u varsa, Blok'un Yapı'sı BB'nin tahmini Yapı'sı ile aynı olmak zorunda.
- [ ] Domain events tanımlandı: `BagimsizBolumOlusturuldu`, `YapiOlusturuldu`, `BagimsizBolumGuncellendi`, `BagimsizBolumSilindi`.

**Out-of-Scope:**
- ❌ EAV custom attribute (Faz 2'de)
- ❌ Katılım grupları (Faz 4'te)
- ❌ Excel import (Faz 3'te)

**Çıktılar:**
```
src/[Project].Domain/
├── Entities/YapiSemasi/
│   ├── Ada.cs
│   ├── Parsel.cs
│   ├── Yapi.cs
│   ├── Blok.cs
│   ├── BagimsizBolum.cs
│   └── BagimsizBolumTipi.cs (eğer entity ise)
├── Enums/
│   ├── YapiTipi.cs
│   └── BagimsizBolumTipiEnum.cs (eğer enum ise)
├── Events/YapiSemasi/
│   ├── BagimsizBolumOlusturuldu.cs
│   ├── YapiOlusturuldu.cs
│   └── ...
└── Common/ (varsa)
    └── EntityBase.cs (eğer henüz yoksa — Id, TenantId, ShortCode, audit)
```

**Effort:** 1-1.5 gün

**Bağımlılıklar:**
- `03-DECISIONS-OPEN.md`'deki Karar #1, #4 onaylanmış olmalı.
- Mevcut `Site` entity'sinin yapısı bilinmeli (kod taranmalı).

---

## FAZ 2 — Yapı Şeması Infrastructure (EF Core + Migration)

**Hedef:** Domain modelini PostgreSQL'e yansıt. Migration üret, tenant filter çalışsın.

**Acceptance Criteria:**
- [ ] Her entity için ayrı `EntityConfiguration` sınıfı (`Infrastructure/Persistence/Configurations/YapiSemasi/`).
- [ ] PK, FK, index, constraint'ler fluent API ile tanımlı.
- [ ] `TenantId` her tabloda; EF Core global query filter onu otomatik filtreliyor.
- [ ] `ShortCode` her tabloda unique constraint (tenant-scope unique: `(TenantId, ShortCode)`).
- [ ] EF Core migration üretildi (`dotnet ef migrations add AddYapiSemasi`).
- [ ] Migration **up + down** test edildi (geri alınabiliyor).
- [ ] Mevcut `Site` entity'si ile `Ada` arasında FK düzgün kurulmuş.
- [ ] `OnModelCreating`'de hepsi register edildi (veya assembly scan kullanılıyorsa otomatik).
- [ ] Index önerileri uygulandı: `(TenantId, ShortCode)` unique, `(SiteId)` FK index, `(YapiId, BlokId)` composite.

**Out-of-Scope:**
- ❌ Partition (BB tablosu büyüyene kadar gerekmiyor)
- ❌ Read model / materialized view (Faz 6'da rapor için)

**Çıktılar:**
```
src/[Project].Infrastructure/
├── Persistence/Configurations/YapiSemasi/
│   ├── AdaConfiguration.cs
│   ├── ParselConfiguration.cs
│   ├── YapiConfiguration.cs
│   ├── BlokConfiguration.cs
│   └── BagimsizBolumConfiguration.cs
└── Persistence/Migrations/
    └── [Timestamp]_AddYapiSemasi.cs
```

**Effort:** 0.5-1 gün

**Bağımlılıklar:** FAZ 1 tamamlandı.

---

## FAZ 3 — Yapı Şeması Application Layer (CQRS)

**Hedef:** Kullanıcının Yapı Şeması üzerinde yapabileceği işlemleri (CRUD + listele + Excel import) MediatR command/query olarak yaz.

**Acceptance Criteria:**
- [ ] Her entity için CRUD command'ları:
  - `Create[Entity]Command` + Handler + Validator
  - `Update[Entity]Command` + Handler + Validator
  - `Delete[Entity]Command` + Handler + Validator
- [ ] List query'leri:
  - `GetAdalarBySiteQuery` (pagination + filter)
  - `GetParsellerByAdaQuery`
  - `GetYapilarBySiteQuery`
  - `GetBaglmsizBolumlerBySiteQuery` (pagination, çok kullanılacak)
  - `GetBagimsizBolumByIdQuery`
  - `GetYapiSemasiAgaciQuery` (Site'ın tüm hiyerarşisini ağaç olarak — UI'da Tree view için)
- [ ] **BB Excel Import:**
  - `ImportBagimsizBolumlerCommand` (IFormFile alır)
  - ClosedXML veya EPPlus ile parse
  - Hatalı satırlar `ImportResult` DTO'sunda raporlanır
  - Idempotent: aynı dosya tekrar import edilirse mevcut kayıtlar ShortCode bazında update edilir
- [ ] FluentValidation rules her command için:
  - BrutMetrekare > 0
  - ArsaPayi >= 0 ve <= 1
  - OdaSayisi >= 0
  - KapiNo unique (Yapı veya Blok scope'unda)
  - ShortCode (Tenant scope unique, validator'da contains check)
- [ ] Her command/query AuthorizeAttribute ile yetki kontrolüne tabi.
- [ ] DTO'lar `Application/Features/YapiSemasi/Dtos/` altında.

**Out-of-Scope:**
- ❌ BB öznitelikleri (EAV) için import — sadece standart kolonlar
- ❌ Toplu güncelleme (bulk update) — Wave 3+

**Çıktılar:**
```
src/[Project].Application/Features/YapiSemasi/
├── Commands/
│   ├── CreateAda/
│   │   ├── CreateAdaCommand.cs
│   │   ├── CreateAdaCommandHandler.cs
│   │   └── CreateAdaCommandValidator.cs
│   ├── CreateParsel/...
│   ├── CreateYapi/...
│   ├── CreateBlok/...
│   ├── CreateBagimsizBolum/...
│   ├── UpdateBagimsizBolum/...
│   ├── DeleteBagimsizBolum/...
│   └── ImportBagimsizBolumler/...
├── Queries/
│   ├── GetAdalarBySite/...
│   ├── GetBagimsizBolumlerBySite/...
│   └── GetYapiSemasiAgaci/...
└── Dtos/
    ├── AdaDto.cs
    ├── BagimsizBolumDto.cs
    └── ImportResultDto.cs
```

**Effort:** 1.5-2 gün

**Bağımlılıklar:** FAZ 2 tamamlandı.

---

## FAZ 4 — Yapı Şeması API + Blazor UI

**Hedef:** Yapı Şeması'nın UI'sı (Tree view ile hiyerarşi gösterimi + CRUD form'ları + Excel import).

**Acceptance Criteria:**
- [ ] API Controller veya Minimal API endpoint'leri her command/query için tanımlı.
- [ ] Blazor sayfaları:
  - `YapiSemasi.razor` — Site bazlı tree view (MudTreeView veya MudList ile)
  - `BagimsizBolumler.razor` — Liste sayfası (MudDataGrid, server-side pagination + filter)
  - `BagimsizBolumDetay.razor` — Detay/düzenleme sayfası
  - `BagimsizBolumImport.razor` — Excel import sayfası (drag-drop, sonuç raporu)
- [ ] Form validation: FluentValidation client + server tarafında uyumlu.
- [ ] Hata mesajları **Türkçe**.
- [ ] Tenant context her sayfada doğru çalışıyor (kullanıcı sadece kendi tenantının verilerini görüyor).
- [ ] Tree view: Site → Ada → Parsel → Yapi → Blok → BB. Her node'a sağ tık ile "Yeni Ekle / Düzenle / Sil" menüsü.
- [ ] BB listesi: en az şu kolonlar — KapiNo, Yapı, Blok, Tip, Brüt m², Arsa Payı, ShortCode.
- [ ] BB import sayfasında örnek Excel şablonu indirilebilir.
- [ ] Audit: her CRUD işlem `IAuditService` üzerinden audit log'a yazılır.

**Out-of-Scope:**
- ❌ BB özniteliklerini UI'dan tanımlama (EAV ekranı) — Wave 3+
- ❌ Toplu seçim ve işlem — Wave 3+

**Çıktılar:**
```
src/[Project].WebUI/
├── Pages/YapiSemasi/
│   ├── YapiSemasi.razor (+.razor.cs)
│   ├── BagimsizBolumler.razor (+.razor.cs)
│   ├── BagimsizBolumDetay.razor (+.razor.cs)
│   └── BagimsizBolumImport.razor (+.razor.cs)
├── Components/YapiSemasi/
│   ├── YapiSemasiTreeView.razor
│   ├── BagimsizBolumForm.razor
│   └── ImportSonucDialog.razor
└── (API controllers veya Endpoints)
    └── YapiSemasiEndpoints.cs (Minimal API ise)
```

**Effort:** 2-2.5 gün

**Bağımlılıklar:** FAZ 3 tamamlandı.

---

## FAZ 5 — Bütçe MVP Domain + Application

**Hedef:** Yıllık bütçe oluştur, kalemleri tanımla, versiyon yönetimi yap (geçmiş bozulmadan).

**Acceptance Criteria:**

### Domain
- [ ] Entity'ler: `Butce`, `ButceVersiyonu`, `ButceKalemi`, `ButceKalemiVersiyonu`, `GiderKategorisi` (basit hiyerarşi, tek seviye), `OdemePlani`, `DonemKaydi`.
- [ ] **Sadece 2 dağıtım modeli** (MVP):
  - `Esit` (her BB eşit pay)
  - `BrutMetrekareyeGore` (m² oranlı pay)
  - `DagitimModeli` enum (arsa payı, oda sayısı sonra)
- [ ] `Butce` aggregate root.
- [ ] Versiyonlama:
  - `ButceVersiyonu.GecerlilikBaslangic` + `.GecerlilikBitis`
  - `ButceVersiyonu.OnceVersiyonId` (zincir)
  - Aktif versiyon: `GecerlilikBitis is NULL veya > NOW()`.
- [ ] `OdemePlani` enum: `AylikEsit, YillikTekSeferde, FaturaBazli`. (Taksitli, Sezonluk sonra.)
- [ ] Domain rule: aynı `(ButceId, GecerlilikBaslangic)` için iki versiyon olamaz.
- [ ] Domain rule: yayınlanmış bir bütçe versiyonu silinemez (sadece yeni versiyon ile geçersiz kılınır).
- [ ] Domain events: `ButceOlusturuldu`, `ButceYayinlandi`, `ButceRevizeEdildi`.

### Application
- [ ] Commands:
  - `CreateButceCommand` (yeni bütçe başlat, taslak)
  - `AddButceKalemiCommand` (taslağa kalem ekle)
  - `UpdateButceKalemiCommand` (taslakta kalem güncelle)
  - `RemoveButceKalemiCommand` (taslakta kalem sil)
  - `PublishButceCommand` (taslağı yayına al — ilk versiyon V1)
  - `ReviseButceCommand` (yayınlanmış bütçeyi revize et — yeni versiyon üret, geçmişi koru)
- [ ] Queries:
  - `GetButcelerBySiteQuery`
  - `GetButceDetayQuery` (kalemleriyle)
  - `GetButceVersiyonlariQuery` (versiyon zinciri)
  - `GetButceKarsilastirmaQuery` (iki versiyonu yan yana karşılaştır)
- [ ] Validation:
  - Bütçe tarih aralığı geçerli (BitisTarihi > BaslangicTarihi)
  - Bütçe yıllık (12 ay) veya ara dönem
  - Bütçe kalemi tutarı >= 0
- [ ] **Revize iş kuralı (kritik):**
  - Yayınlanmış versiyon V1'in `GecerlilikBitis` yeni revize tarihine set edilir.
  - Yeni versiyon V2 oluşur, `GecerlilikBaslangic = revize tarihi`.
  - V2 sadece değişen kalemlerin yeni hallerini içerir; değişmeyen kalemler V1'den kopyalanır.
  - Eğer V1'e bağlı **tahakkuk varsa onlar dokunulmaz** (önemli — yıl ortası revizyonda geçmiş ay tahakkukları korunur).

**Out-of-Scope:**
- ❌ Kullanıcı tanımlı formül (Wave 3+)
- ❌ Gelir kalemleri (Wave 3+)
- ❌ Yedek akçe (Wave 3+)
- ❌ Senaryo (Wave 4+)
- ❌ Onay süreci entegrasyonu (Wave 2 sonu — basit "publish" yeterli)

**Çıktılar:**
```
src/[Project].Domain/Entities/Butce/
├── Butce.cs
├── ButceVersiyonu.cs
├── ButceKalemi.cs
├── ButceKalemiVersiyonu.cs
├── GiderKategorisi.cs
├── OdemePlani.cs (entity veya value object)
└── DonemKaydi.cs

src/[Project].Domain/Enums/
├── DagitimModeli.cs
├── OdemePlaniTipi.cs
└── ButceDurum.cs

src/[Project].Application/Features/Butce/
├── Commands/...
└── Queries/...
```

**Effort:** 2 gün

**Bağımlılıklar:** FAZ 4 tamamlandı.

---

## FAZ 6 — Tahakkuk Motoru MVP

**Hedef:** Aylık tahakkuk üretimi — verilen bir dönem için aktif bütçe versiyonunu okuyup BB'lere dağıt.

**Acceptance Criteria:**

### Domain
- [ ] Entity'ler: `Tahakkuk`, `TahakkukDetayi`.
- [ ] `Tahakkuk` bir bütçe kalemi versiyonu + dönem (DonemKaydi) için tek (UNIQUE constraint).
- [ ] `TahakkukDetayi` her BB için: TahakkukTutari, VadeTarihi, DagitimPayi.
- [ ] Domain rule: yayınlanmış tahakkuk **değiştirilemez** (iptal edilebilir, ama düzenlenemez — `Durum` enum).

### Application
- [ ] **Command:** `UretTahakkukCommand(DonemKaydiId)` veya `UretTahakkukCommand(SiteId, Yil, Ay)`.
- [ ] Command Handler işlemi:
  1. Dönem aktif mi kontrol et (kapatılmamış olmalı).
  2. Site için aktif bütçe versiyonunu bul (`GecerlilikBaslangic <= DonemBaslangic AND (GecerlilikBitis IS NULL OR GecerlilikBitis >= DonemBitis)`).
  3. Her `ButceKalemiVersiyonu` için:
     a. `OdemePlani`'ye göre o dönemde tahakkuk üretilmeli mi kontrol et.
     b. Tutarı hesapla (örn: yıllık → /12, tek seferde → tek Ocak'ta).
     c. `DagitimModeli`'ne göre BB'lere dağıt:
        - `Esit`: tutar / BBSayisi (yuvarlama LRM)
        - `BrutMetrekareyeGore`: BB.Brut / TopBrut * tutar (yuvarlama LRM)
     d. Vade tarihi: bir sonraki ayın `VadeGunu`'sü (varsayılan ayın 15'i, parametrik).
  4. `TahakkukDetayi` kayıtları yaz, `TahakkukUretildi` event'i fırlat.
- [ ] **Idempotency:** Aynı `(ButceKalemiVersiyonuId, DonemKaydiId)` çiftine ikinci kez üretim çağrılırsa:
  - Eğer henüz tahsilat yapılmamışsa: mevcut tahakkuğu sil, yeniden üret (audit kaydı bırak).
  - Eğer tahsilat varsa: hata fırlat, kullanıcı önce tahsilatı geri almak zorunda.
- [ ] **Yuvarlama (LRM — Largest Remainder Method):**
  - Tutar / pay_oranı tam sayıya bölünmezse, artık kuruş en büyük ondalık kalanlı BB'ye eklenir.
  - Toplam = girdi tutarı (toplama hata kabul edilmez).
- [ ] **Sıfır-koruma:** BB.BrutMetrekare = 0 ise m² dağıtımında **dahil edilmez** (audit not düşülür).
- [ ] **Hangfire Job:** `TahakkukUretmeJob` — her ayın 1'inde otomatik tetiklenir (CRON), o ayın aktif tüm sitelerine tahakkuk üretir.
- [ ] **Manuel tetikleme:** UI'dan da `UretTahakkukCommand` çağrılabilir.

### Queries
- [ ] `GetTahakkuklarByDonemQuery`
- [ ] `GetTahakkukDetaylarByBBQuery` (BB sahibinin kendi tahakkuk listesi)
- [ ] `GetTahakkukOzetByButceQuery` (bütçe karşılaştırma raporu için temel)

**Out-of-Scope:**
- ❌ Tahsilat (Faz 7'de)
- ❌ Gecikme cezası (Faz 7'de)
- ❌ Tahakkuk iptal (Faz 7'de — basit cancel ile)
- ❌ Manuel override (Wave 3+)
- ❌ Formül-bazlı tutar (Wave 3+)

**Çıktılar:**
```
src/[Project].Domain/Entities/Tahakkuk/
├── Tahakkuk.cs
└── TahakkukDetayi.cs

src/[Project].Application/Features/Tahakkuk/
├── Commands/UretTahakkuk/
│   ├── UretTahakkukCommand.cs
│   └── UretTahakkukCommandHandler.cs
├── Queries/...
└── Services/
    ├── IDagitimMotoru.cs           (interface)
    ├── EsitDagitimMotoru.cs
    └── M2DagitimMotoru.cs

src/[Project].Infrastructure/
└── BackgroundJobs/
    └── TahakkukUretmeJob.cs (Hangfire)
```

**Effort:** 2-2.5 gün

**Bağımlılıklar:** FAZ 5 tamamlandı.

---

## FAZ 7 — Tahsilat MVP + Gecikme Cezası + Bütçe/Tahakkuk UI

**Hedef:** Manuel tahsilat girişi + basit gecikme hesabı + bütçe ve tahakkuk ekranları.

**Acceptance Criteria:**

### Tahsilat (Domain + Application)
- [ ] Entity: `Tahsilat` (TahakkukDetayiId, TahsilatTarihi, OdenenTutar, OdemeYontemi, ReferansNo, Aciklama).
- [ ] Bir `TahakkukDetayi`'na birden çok `Tahsilat` kaydı (kısmi ödeme).
- [ ] `BorcDurumu` view veya hesap servisi: BB başına kalan borç (Tahakkuk - Tahsilat + İşlemiş Gecikme).
- [ ] Command: `KaydetTahsilatCommand`.
- [ ] Validation: OdenenTutar > 0, OdenenTutar <= KalanBorc + tolerans (fazla ödeme şimdilik avans olarak değil, hata).
- [ ] Domain event: `TahsilatKaydedildi`.

### Gecikme Cezası (Basit MVP)
- [ ] Entity: `GecikmeHesabi` (TahakkukDetayiId, HesaplamaTarihi, HesaplananTutar).
- [ ] **Kural:** Vadesi geçmiş `TahakkukDetayi` için, vadeden bugüne geçen tam aylar × OranAylik × Anapara. (Bileşik DEĞİL — MVP basit.)
- [ ] **Tavan kontrolü:** Aylık oran %5'i geçemez (KMK m.20). Sistem-genelinde sabit, kullanıcı 5'ten yüksek girerse reddedilir.
- [ ] Hangfire Job: `GecikmeHesaplamaJob` — günlük çalışır, vadesi dolmuş tahakkukları gözden geçirir.
- [ ] Query: `GetBorcDurumuByBBQuery` (BB başına anapara + işlemiş gecikme).

### UI (Blazor)
- [ ] `Butceler.razor` — Site bazlı bütçe listesi.
- [ ] `ButceDetay.razor` — Bütçe kalemleri, versiyon karşılaştırma, "Yeni Versiyon Üret" butonu.
- [ ] `Tahakkuklar.razor` — Dönem bazlı tahakkuk listesi, "Tahakkuk Üret" butonu.
- [ ] `TahakkukDetay.razor` — Bir tahakkuğun BB başına dağılımı.
- [ ] `BorcDurumu.razor` — BB bazlı borç durumu (kullanıcı kendi BB'sini görür; yönetici tüm BB'leri).
- [ ] `TahsilatKayit.razor` — Manuel tahsilat girişi (BB seç → tahakkuk seç → tutar gir).

**Out-of-Scope:**
- ❌ Banka ekstresi import (Wave 4+)
- ❌ Otomatik eşleştirme (Wave 4+)
- ❌ Bileşik gecikme (Wave 3+)
- ❌ Kısmi ödeme tahsis politikası seçimi (TBK m.101 default — sonra konfigüre edilebilir)

**Çıktılar:** Yukarıda listelendi.

**Effort:** 2-3 gün

**Bağımlılıklar:** FAZ 6 tamamlandı.

---

## FAZ 8 — Test + Referans Senaryo Doğrulama

**Hedef:** İlk üç referans senaryonun (küçük apartman, orta site, revizyon) end-to-end testlerle çalıştığını kanıtla.

**Acceptance Criteria:**
- [ ] Unit tests:
  - Domain rules (BB validation, ButceVersiyonu zinciri, dağıtım matematiği)
  - Dağıtım motorları (Esit ve M2 — yuvarlama dahil)
  - LRM yuvarlama (toplam = girdi)
- [ ] Integration tests (TestContainers + PostgreSQL):
  - Senaryo A (12 BB apartman): bütçe yarat → tahakkuk üret → tahsilat → borç durumu doğru
  - Senaryo B (120 BB site): aynısı + 1 muafiyet senaryosu **(NOT: Muafiyet henüz scope-dışı; bu senaryoyu Wave 3'e ertele veya basit halde kur — sadece "havuz kullanıcıları katılım grubu" placeholder olarak)**
  - Senaryo C (Senaryo B'nin Temmuz revizesi): Ocak-Haziran tahakkukları değişmedi, Temmuz-Aralık yeni tutarlarla
- [ ] Tenant izolasyon testi: 2 ayrı tenant'a aynı veri girilir, biri diğerini göremiyor.
- [ ] Performance smoke: 200 BB'lik tahakkuk üretimi < 30 sn.

**Çıktılar:**
```
tests/
├── [Project].Domain.Tests/
├── [Project].Application.Tests/
├── [Project].Integration.Tests/
└── fixtures/
    ├── senaryo-a-kucuk-apartman/
    │   ├── input-bb-envanteri.csv
    │   ├── input-butce-kalemleri.json
    │   └── expected-tahakkuk-2026.json
    ├── senaryo-b-orta-site/...
    └── senaryo-c-revize/...
```

**Effort:** 1.5-2 gün

**Bağımlılıklar:** FAZ 7 tamamlandı.

---

## Toplam Effort Tahmini

| Faz | Effort |
|-----|--------|
| 1 — Yapı Şeması Domain | 1-1.5 gün |
| 2 — Yapı Şeması Infrastructure | 0.5-1 gün |
| 3 — Yapı Şeması Application | 1.5-2 gün |
| 4 — Yapı Şeması UI | 2-2.5 gün |
| 5 — Bütçe MVP Domain+App | 2 gün |
| 6 — Tahakkuk Motoru | 2-2.5 gün |
| 7 — Tahsilat + Gecikme + UI | 2-3 gün |
| 8 — Test + Referans Senaryolar | 1.5-2 gün |
| **Toplam** | **13-16 gün** (~3 hafta) |

## Faz Geçiş Kuralı

> **Bir faz tamamlanmadan diğerine geçme.** Faz tamamlandı sayılması için:
> - Tüm Acceptance Criteria check'lendi ✅
> - Kod build oluyor, testler geçiyor ✅
> - Kullanıcı demo'da onayladı ✅
> - PR merge edildi ✅

## Out-of-Scope Hatırlatma

Her faz kartında "Out-of-Scope" listesi var. **Bu listeye eklenmemiş bir şey scope dışı sayılır.** "Bunu da hızlıca ekleyeyim" diyerek scope büyütme — sprint planı bozulur. Eklemek istediğin bir şey varsa kullanıcıya sor, yeni faz kartı oluştur.
