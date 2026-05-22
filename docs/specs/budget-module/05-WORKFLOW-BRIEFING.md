# 05 — Bütçe Modülü İş Akışı Brifingi

> v0.2.13 sonrası referans dokümanı. Bütçe modülünün rol/scope ayrımını, muhasebe ve malik/sakin taraf işlemlerini, otomatik sistem süreçlerini, mevcut implementasyon durumunu ve önerilen yol haritasını özetler.

---

## 0. Erişim Modeli Kararı — Tenant Scope Only

**Karar (2026-05-22):** Bütçe modülü artık **Site (Company) scope'unda görünmez**. Sadece **Yönetim (Tenant) scope'unda** ve **`Tenant.Budget.*` permission'ları ile** erişilebilir.

### Sebep
- Muhasebeci/operatör, bir Yönetim'in (firma) tüm sitelerini tek bağlamda izlemek ister
- Bütçe disiplini Yönetim seviyesinde kurulur; site personeli (kapıcı, alan yöneticisi) yalnız operasyonel veriyi görür
- Yetki granülaritesi: `Tenant.Budget.View`, `Tenant.Budget.Edit`, `Tenant.Budget.Publish`, `Tenant.Accrual.Generate`, `Tenant.Collection.Record`
- Site scope'ta gelecekte yalnızca **okunabilir özet** (kendi sitesinin bütçe ÖZETİ) ileride yetki dahilinde sunulabilir; bu MVP dışı

### Veri Modeli vs Erişim Modeli
| Boyut | Karar |
|-------|-------|
| **Veri**: Her Site (Company) için 1 Bütçe (Karar #2, değişmedi) | Company-scoped data |
| **Erişim**: UI / NavMenu / endpoint hepsi Tenant scope | Tenant-scoped access |
| **Filtre**: Tenant scope'taki bir kullanıcı, kendisine atanmış Yönetim'in tüm sitelerini görür ve istediği sitenin bütçesinde çalışır | Site listesinden seçim |

### Refactor Etkisi (Mevcut Kodda)
- `BudgetPage` ve `BudgetVsActualPage` `CompanyArea/Accounting` altından `TenantArea/Budget` altına taşınır
- Route'lar: `/tenant/budget/...` (önceki `/company/accounting/budget` kalkar)
- NavMenu Muhasebe alt grubundan **Bütçe** linki çıkarılır; Yönetim grubuna **Bütçe** sekmesi eklenir
- Permission rejimi: System bypass + `Tenant.Budget.*` + opsiyonel `CompanyId` parametresi (Tenant kullanıcı hangi siteye baktığını seçer)
- Mevcut `SetBudgetCommand` (eski model) FAZ 5'te kaldırılacak; bu nedenle taşıma + yeniden tasarım birlikte düşünülmeli

---

## 1. Aktör Haritası

| Aktör | Scope | Erişim Aracı | Ana Sorumluluk |
|-------|-------|--------------|----------------|
| **Sistem Operatörü** | System | ManagementApp | Yönetim oluşturma, mevzuat parametreleri (KMK m.20 tavanı), denetim |
| **Yönetim Yöneticisi** | Tenant | ManagementApp | Bütçe planlama, onaylama, tahakkuk üretimi, tahsilat denetimi |
| **Muhasebeci** | Tenant (`Tenant.Budget.*` izinli) | ManagementApp | Bütçe satırlarını oluşturma, banka eşleme, tahsilat kaydı |
| **Site Yöneticisi** (kapıcı/yönetici) | Company | ManagementApp | Sadece operasyonel veri (birim/sakin yönetimi). Bütçe verisini görmez |
| **Malik** | (Self) | PortalApp | Kendi BB'sinin tahakkuk + borç + ödeme geçmişi |
| **Sakin / Kiracı** | (Self) | PortalApp | Aynı malik gibi; kira ilişkisine göre değişebilir |

---

## 2. Muhasebe Tarafı — İşlem Sırası (Tenant Scope, ManagementApp)

### A. Hazırlık (One-time / Her Şirket İçin Bir Defa)
1. **TDHP Hesap Planı Aktivasyonu** ✅ Mevcut
   - `InitializeChartOfAccountsCommand` → standart TDHP şablonu seed'lenir
   - Şirket-spesifik hesaplar `CreateAccountCodeCommand` ile eklenir
2. **Maliyet Merkezleri Tanımı** ✅ Mevcut
   - Bloklar, ortak alanlar, sosyal tesisler için `CostCenter` kaydı
3. **Mali Yıl + Dönem Açma** ✅ Mevcut
   - `CreateFiscalYearCommand` → 12 aylık `AccountingPeriod` otomatik oluşur
4. **Birim (BB) Listesi Hazır** ✅ FAZ 1-2 tamamlandı
   - Excel import veya manuel CRUD ile

### B. Bütçe Kurulumu (FAZ 5 — Henüz Yok)
5. **Katılım Grubu Tanımlama** ❌
   - Örnek: "Havuz Kullanıcıları", "Asansör Kullanıcıları", "Ticari Birimler"
   - Hangi BB'lerin grupta olduğu işaretlenir (`BagimsizBolumKatilimGrubu`)
6. **Gider Kalemi Tanımlama** ❌
   - "Elektrik", "Asansör Bakım", "Havuz Kimyasalı" gibi 30-150 satır
   - Her kalem → katılım grubu + dağıtım modeli + ödeme planı bağlantısı (`GiderPaylasimGrubu`)
7. **Bütçe (Butce) Oluşturma** ❌
   - Yıllık konteyner: `CreateButceCommand(CompanyId, FiscalYearId)`
   - Durum: Taslak
8. **Bütçe Kalemi Versiyonu Atama** ❌
   - Her kalem için: Planlanan Tutar (yıllık veya aylık)
   - Dağıtım modeli: Eşit / m² oranlı / arsa payı / formül
   - Ödeme planı: Aylık Eşit / Yıllık Tek Seferde / Fatura Bazlı / Mevsimsel
9. **Bütçe Yayınlama** ❌
   - `PublishButceCommand` → V1 oluşur, immutable
   - `Tenant.Budget.Publish` permission gerekli
   - Olay: `ButceYayinlandi` (Outbox)

### C. Tahakkuk Üretimi (FAZ 6 — Henüz Yok)
10. **Aylık Tahakkuk Üretimi** ❌
    - Manuel: `UretTahakkukCommand(FiscalYearId, Month)` → buton (yetki: `Tenant.Accrual.Generate`)
    - Otomatik: Hangfire `TahakkukUretmeJob` — her ayın 1'inde 00:00 (Europe/Istanbul)
    - Akış:
      1. Aktif `ButceVersiyonu` bulunur (geçerlilik tarihi penceresinde)
      2. Her `ButceKalemiVersiyonu` için:
         - Ödeme planına göre o ayda tahakkuk edilecek mi?
         - Tutarı dağıtım motoruna (`EsitDagitimMotoru` / `M2DagitimMotoru`) gönder
         - LRM (Largest Remainder Method) ile yuvarla — kuruş kaybı yok
         - Her BB için `TahakkukDetayi` oluştur (Tutar, Vade Tarihi)
      3. Olay: `TahakkukUretildi` (Outbox)
    - **İdempotency:** Aynı `(ButceKalemiVersiyonuId, DonemKaydiId)` çiftine ikinci üretim engellenir; tahsilat yoksa yenilenebilir
11. **Tahakkuk İptali / Düzeltme** ❌
    - Tahsilat yoksa: silinebilir
    - Tahsilat varsa: ters fiş (`DuzeltmeIslemi`) gerekir

### D. Tahsilat (FAZ 7 — Henüz Yok)
12. **Banka Eşleme** ❌
    - Banka ekstre CSV'si yüklenir → Otomatik eşleme: BB kodu + tutar
    - Hedef: ~%80 oto-eşleşme, kalanı manuel
13. **Tahsilat Kaydı** ❌
    - `KaydetTahsilatCommand(TahakkukDetayiId, OdenenTutar, OdemeYontemi)`
    - Kısmi ödeme: aynı tahakkuk için N adet tahsilat satırı olabilir
    - TBK m.101: Önce en eski + faiz uygulanır (otomatik dağıtım)
    - Olay: `BorcOdendi` (Outbox)
14. **Yevmiye Fişi Otomatik Üretimi** ❌ (entegrasyon)
    - Tahsilat kaydedildiğinde otomatik muhasebe fişi (Kasa/Banka borç, Aidat hesabı alacaklı)
    - `JournalEntry` katmanı zaten mevcut → handler eklemek yeterli

### E. Gecikme ve İzleme (FAZ 7)
15. **Gecikme Hesaplama** ❌
    - Hangfire `GecikmeHesaplamaJob` — her gün 02:00 (Europe/Istanbul)
    - Her gecikmiş `TahakkukDetayi` için: GünSayısı / 30 × MonthlyRate% × Anapara
    - KMK m.20: Aylık %5 tavanı; sistem genelinde `MevzuatTavanlari` kataloğu zorunlu engelliyor
    - Olay: `GecikmeHesaplandi` (Outbox)
16. **Borç Durumu Raporu** ❌
    - `GetBorcDurumuByCompanyQuery`: BB bazlı borç + faiz tablosu
    - Renk kodlama: Vadesinde / Yaklaşan / Gecikmiş / Kritik (≥ 90 gün)
    - Filtre: Site, Blok, Borç eşiği, Süre eşiği

### F. Revizyon (FAZ 5)
17. **Bütçe Revize** ❌
    - Yıl ortasında: `ReviseButceCommand(VersiyonId, RevizeTarihi, GerekceMesaji, YeniKalemler)`
    - Eski `V1.GecerlilikBitis` = `RevizeTarihi - 1`
    - Yeni `V2.GecerlilikBaslangic` = `RevizeTarihi`
    - Eski tahakkuklar dokunulmaz; yeni dönemde V2 kullanılır
    - Olay: `ButceRevizeEdildi` (Outbox)

### G. Raporlama
18. **Mizan / Bilanço / Gelir Tablosu** ✅ UI + sorgu hazır
    - Yevmiye verisinden çalışıyor, bütçeden bağımsız
19. **Bütçe-Gerçekleşme Karşılaştırma** ⚠️ UI hazır, veri için FAZ 6 gerekli
    - `GetBudgetVsActualQuery` mevcut ama eski `Budget` modeline bağlı; FAZ 5 sonrası yeniden bağlanacak

---

## 3. Malik / Sakin Tarafı — PortalApp İşlemleri

> PortalApp ayrı bir Blazor Server uygulaması; her malik/sakin sadece kendi BB'sinin verisini görür. Domain veri sahipliği aynı: `TenantId` + `BB.Id` filtresi.

### Görüntüleme (Read-Only — FAZ 6-7 ile Hayata Geçer)
1. **Borç Durumum** ❌
   - "Toplam Borç: 1.247,30 TL (Anapara: 1.180,00 + Gecikme: 67,30)"
   - Vadesinde / gecikmiş ayrımı
   - Query: `GetBorcDurumuByUnitQuery(BBId)`
2. **Tahakkuk Geçmişim** ❌
   - Her aya ait aidatım: tutar, vade, durum (ödendi/kısmi/açık)
   - Kırılım: "Bu ayın 765 TL'sinin 230 TL'si elektrik (m² oranınla), 180 TL asansör bakım..."
   - Query: `GetTahakkukDetaylarByUnitQuery(BBId, From, To)`
3. **Ödeme Geçmişim** ❌
   - Tüm tahsilatlarım: tarih, tutar, yöntem, hangi tahakkuğa karşılık
   - Query: `GetTahsilatlarByUnitQuery(BBId, From, To)`
4. **Detay Kırılım (Şeffaflık)** ❌
   - "Asansör Bakım 250 TL → 24 BB ortak / m² oranlı dağılım → benim payım 23,42 TL"
   - KMK m.18 paylaşım kurallarına uyumlu; itiraz için zemin
5. **Online Ödeme** ❌ Wave 3+
   - iyzico / sanal POS entegrasyonu — şu an spec dışı

### Aksiyon (Wave 2+)
6. **Sakin → Yönetim İletişimi**
   - "Bu kalemin dağıtımı yanlış" → İtiraz formu (`OdemeItiraz`)
   - Yönetim tarafından inceleme + cevap
7. **Profil & Bildirim Tercihleri**
   - E-posta / SMS / uygulama bildirimleri (yeni tahakkuk, ödeme hatırlatma, gecikme uyarısı)

---

## 4. Otomatik Sistem İşlemleri

### Hangfire Background Jobs (FAZ 6-7)
| Job | Sıklık | Görev |
|-----|--------|-------|
| `TahakkukUretmeJob` | Aylık — her ayın 1'i, 00:00 (Europe/Istanbul) | Aktif Yönetimlerin yayınlı `ButceVersiyonu`'larından tahakkuk üretir |
| `GecikmeHesaplamaJob` | Günlük — 02:00 (Europe/Istanbul) | Vadesi geçmiş `TahakkukDetayi`'ler için gecikme faizi hesaplar |
| `OutboxDispatcherJob` | Sürekli (5 dk poll) | Outbox tablosundaki event'leri SMS/Email/Push/Audit'e yollar |

### Domain Event'ler (Outbox Pattern)
| Event | Tetikleyici | Tüketici(ler) |
|-------|-------------|---------------|
| `ButceYayinlandi` | `PublishButceCommand` | Audit |
| `ButceRevizeEdildi` | `ReviseButceCommand` | Audit, Bildirim ("bütçe revize edildi" — opsiyonel) |
| `TahakkukUretildi` | `UretTahakkukCommand` veya Hangfire | Audit, **Bildirim → Maliklere "yeni aidat tahakkuk etti"** |
| `BorcOdendi` | `KaydetTahsilatCommand` | Audit, Muhasebe (otomatik yevmiye fişi), Bildirim ("ödemeniz alındı") |
| `GecikmeHesaplandi` | `GecikmeHesaplamaJob` | Audit, opsiyonel "vade hatırlatma" bildirimi |

---

## 5. Yetki / Permission Matrisi (Önerilen)

### Tenant Scope Permissions
| Permission | İçeriği |
|-----------|---------|
| `Tenant.Budget.View` | Bütçe + tahakkuk + tahsilat listelerini görür |
| `Tenant.Budget.Edit` | Taslak bütçeyi düzenler (kalem ekle / sil / güncelle) |
| `Tenant.Budget.Publish` | Bütçe yayınlama, revize başlatma (Onaylayıcı rolü) |
| `Tenant.Accrual.Generate` | Manuel tahakkuk üretme; Hangfire kullanıcısı ayrı sistem hesabı |
| `Tenant.Collection.View` | Tahsilat / borç raporlarını görür |
| `Tenant.Collection.Record` | Manuel tahsilat girer, banka ekstresi eşler |
| `Tenant.LateFee.Configure` | Şirket özelinde gecikme oranı + tavan ayarlar (mevzuat tavanı altında) |

### Company Scope (Site Yöneticisi)
| Permission | İçeriği |
|-----------|---------|
| `Company.UnitOwner.View` | BB sakinlerinin temel bilgileri (ödeme detayı **YOK**) |
| (Bütçe okuyucu yok) | Bütçe verisi Site scope'a sızmıyor |

### Self (Malik / Sakin)
- Otomatik olarak kendi BB'sinin verisini görür; ek permission yok
- Yetkiler `UserUnitAssignment` üzerinden (kullanıcı ↔ BB ilişkisi)

---

## 6. Mevcut Durum vs Hedef

| Kategori | Mevcut | Hedef (Spec) | Eksik (FAZ) |
|----------|--------|--------------|-------------|
| Hesap Planı | ✅ TDHP | ✅ | — |
| Mali Yıl / Dönem | ✅ | ✅ | — |
| Yevmiye / Fatura | ✅ | ✅ | — |
| Bütçe Domain (basit) | ⚠️ Çok yalın (`Budget` = Period × AccountCode × Tutar) | ❌ Yok (Butce / ButceVersiyonu / ButceKalemi / KalemVersiyonu / OdemePlani / DagitimModeli / GiderPaylasimGrubu / KatilimGrubu / KapsamKurali / MuafiyetKurali) | **FAZ 5** |
| Bütçe Komutları | ⚠️ `SetBudgetCommand` (eski model) | CreateButce, AddKalem, PublishButce, ReviseButce | **FAZ 5** |
| Tahakkuk | ❌ Yok | Tahakkuk + TahakkukDetayi + UretTahakkukCommand + DagitimMotoru + LRM | **FAZ 6** |
| Tahsilat | ❌ Yok | Tahsilat + KaydetTahsilatCommand + BorcDurumu | **FAZ 7** |
| Gecikme | ❌ Yok | GecikmeHesabi + GecikmeHesaplamaJob + KMK m.20 tavanı | **FAZ 7** |
| UI — Muhasebeci | ⚠️ BudgetPage (yanlış scope + model) | Tenant scope sayfaları + tablo + chart + revizyon dialogu | **FAZ 5-7** |
| UI — Malik | ❌ PortalApp'ta yok | Borç + Tahakkuk + Tahsilat sayfaları | **FAZ 7** |
| Hangfire Jobs | ❌ Yok | TahakkukUretmeJob + GecikmeHesaplamaJob | **FAZ 6-7** |
| Permissions | ⚠️ Sadece `System.*` + `Tenant.Users.Manage` | Tenant.Budget.* + Tenant.Accrual.* + Tenant.Collection.* | **FAZ 5-7** |

---

## 7. Önerilen Yol Haritası

### Adım 0 — Erişim Refactor'u (Hemen — ~30 dk)
- `BudgetPage`'ı `CompanyArea/Accounting` → `TenantArea/Budget` altına taşı
- NavMenu'da Muhasebe alt grubundan kaldır; **Yönetim** grubuna **Bütçe** sekmesi ekle
- Route: `/tenant/budget` (BudgetPage geçici; FAZ 5'te yeniden tasarlanacak)
- Permission koruması: `Tenant.Budget.View` zorunlu, yoksa `_unauthorized = true`
- `BudgetVsActualPage` raporu da Tenant scope'a alınır
- Permission catalog'a 7 yeni izin eklenir

### Adım 1 — FAZ 5: Bütçe Domain (~2 gün)
- Butce, ButceVersiyonu, ButceKalemi, ButceKalemiVersiyonu, OdemePlani, DagitimModeli, GiderPaylasimGrubu, KatilimGrubu, KapsamKurali, MuafiyetKurali
- Create / Add / Publish / Revise komutları + validatörler
- Eski `Budget` entity drop (migration ile veri taşıma)
- Migration: `AddBudgetingDomain`

### Adım 2 — FAZ 6: Tahakkuk Motoru (~2,5 gün)
- Tahakkuk + TahakkukDetayi
- `UretTahakkukCommand` + 2 Dağıtım motoru (Esit, M²) + LRM
- Hangfire `TahakkukUretmeJob`
- Migration: `AddAccrualEngine`

### Adım 3 — FAZ 7A: Tahsilat (~1,5 gün)
- Tahsilat + `KaydetTahsilatCommand`
- TBK m.101 partial payment allocation
- Otomatik yevmiye fişi handler
- Migration: `AddCollections`

### Adım 4 — FAZ 7B: Gecikme + UI (~1,5 gün)
- GecikmeHesabi + Hangfire `GecikmeHesaplamaJob`
- KMK m.20 tavan validation
- BorcDurumu query / view
- ManagementApp: Tenant Bütçe sayfaları (yeniden tasarım) — Bütçe listesi, detay, tahakkuk listesi, tahsilat girişi, borç durumu
- PortalApp: Malik için 3 sayfa (Borç / Tahakkuk Geçmişi / Ödeme Geçmişi)
- Migration: `AddLateFees`

### Adım 5 — FAZ 8: Test (~2 gün)
- 3 referans senaryo (12-BB apartman, 120-BB site, mid-year revizyon)
- Unit + integration + tenant isolation testleri
- TestContainers + PostgreSQL

**Toplam:** ~10-11 iş günü (Adım 0 hariç FAZ 5-8)

---

## 8. Açık Sorular (Karar Bekleyen)

1. **Onay akışı:** MVP için `IApprovalService` + `AutoApproveApprovalService` (Karar #6 ile uyumlu). 2-kişilik onay zinciri (taslak → onay → yayın) Wave 3'e mi bırakılacak?
2. **Banka entegrasyonu:** Hangi bankaların CSV / OFX formatları desteklenecek? İlk MVP'de sadece manuel eşleme + tek CSV formatı mı?
3. **Tenant Bütçe sayfası akışı:** Sayfa açıldığında varsayılan Site otomatik seçili mi gelsin, yoksa Site listesi zorunlu seçim ekranı mı çıksın? (Bir Yönetim'in 50 sitesi olabileceği için ikincisi daha makul olabilir)
4. **PortalApp veri köprüsü:** PortalApp şu anda hangi domain modeline bağlı? Tahakkuk / borç sayfaları için yeni reader gerekecek mi yoksa ManagementApp ile aynı reader paylaşılabilir mi?
5. **Mevzuat Tavanı UI:** Sistem operatörü `MevzuatTavanlari`'nı UI'dan mı yönetecek (System scope sayfası), yoksa appsettings / migration seed'den mi sabit tutulacak?
6. **Mid-year revizyon kullanıcı UX'i:** Revizyon ile aynı kalemin yeni tutarı verildiğinde, sistem otomatik "etkilenmemiş aylara yansıtma" mı yapsın yoksa kullanıcı her dönem için ayrı tutar girişi mi yapsın?

---

## 9. İlgili Dosyalar ve Referanslar

### Spec Dokümanları
- [00-CONTEXT.md](00-CONTEXT.md) — Proje bağlamı
- [01-SDD-v1.0.md](01-SDD-v1.0.md) — Master tasarım dokümanı (~1960 satır)
- [02-PHASE-CARDS.md](02-PHASE-CARDS.md) — 8 fazlık plan
- [03-DECISIONS-OPEN.md](03-DECISIONS-OPEN.md) — Kararlar (6 madde kapandı 2026-05-22)
- [04-PLAYBOOK.md](04-PLAYBOOK.md) — Claude Code çalışma protokolü

### Mevcut Kod (Refactor / Replace Edilecek)
- `src/Core/CleanTenant.Domain/Tenant/Accounting/Budget.cs` — Eski / yalın model (FAZ 5'te kaldırılacak)
- `src/Core/CleanTenant.Application/Features/Main/Accounting/Budgets/` — Eski komutlar
- `src/Presentation/CleanTenant.ManagementApp/Components/Pages/CompanyArea/Accounting/BudgetPage.razor` — Adım 0'da TenantArea'ya taşınacak
- `src/Presentation/CleanTenant.ManagementApp/Components/Pages/CompanyArea/Accounting/Reports/BudgetVsActualPage.razor` — Adım 0'da TenantArea'ya taşınacak

### Yeni Eklenecek (FAZ 5-7)
- `src/Core/CleanTenant.Domain/Tenant/Budgeting/` — Yeni klasör (Butce, ButceVersiyonu, ButceKalemi, KalemVersiyonu, ...)
- `src/Core/CleanTenant.Domain/Tenant/Accrual/` — Tahakkuk, TahakkukDetayi
- `src/Core/CleanTenant.Domain/Tenant/Collection/` — Tahsilat, GecikmeHesabi
- `src/Core/CleanTenant.Application/Features/Main/Budgeting/` — Komutlar
- `src/Core/CleanTenant.Application/Features/Main/Accrual/`
- `src/Core/CleanTenant.Application/Features/Main/Collection/`
- `src/Infrastructure/CleanTenant.Infrastructure.Persistence/Main/Configurations/Budgeting/`
- `src/Infrastructure/CleanTenant.Infrastructure.BackgroundJobs/Jobs/Accounting/` — Hangfire job'ları
- `src/Presentation/CleanTenant.ManagementApp/Components/Pages/TenantArea/Budgeting/` — UI sayfaları
- `src/Presentation/CleanTenant.PortalApp/Components/Pages/MyAccount/` — Malik sayfaları

---

> **Sonuç:** Bütçe modülü mevcut durumda **iskelet**: hesap planı + yevmiye + temel raporlama katmanı çalışıyor, ancak **bütçe planlama, tahakkuk üretimi ve tahsilat akışı henüz yok**. Tenant-scope erişim kararı bu eksikliklerin doldurulması sürecini iki yönden destekler: (1) UI / route yeniden organize edilirken FAZ 5 entity'leri doğal sahibine yerleşir; (2) yetkiler Tenant matrisine eklendiğinde Site scope'a sızıntı olmaz; (3) Muhasebeci/Yönetici ayrımı doğal akışına oturur.
>
> İlk önerilen adım: **Adım 0 erişim refactor'u** (NavMenu + route taşıma + Permission koruması). Sonra FAZ 5 başlangıcı için domain entity tasarımına geçilebilir.
