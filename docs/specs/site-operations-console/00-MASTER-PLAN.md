# Site Operasyon Konsolu — Bağımsız Bölüm / Sakin / Tahsilat (Master Plan)

> Durum: **TASLAK PLAN** (kod yok). Karar tarihi: 2026-05-24.
> Kullanıcı yönü: "Önce büyük konsolu planla; backend'te olmayan bölümler sonra hazırlanacak."
> Geliştirme sırası tercihi: **Önce Gecikme**, sonra Tahsilat.

## 1. Vizyon (kullanıcı tanımı)

Site bağlamına geçilir → sol menüde **Bağımsız Bölümler** → body'de tüm BB'ler arasından **ara-bul-seç** →
seçili BB için:

- **Üstte:** Bağımsız Bölümün toplam borcu. **Sağda:** Tahsilat butonu.
- **Solda (aynı div içinde, ağaç):** **Malikler** ve **Kiracılar** ayrı düğümler.
  - Malikler: tarih aralığına göre **aktif malik en üstte → ilk malik en altta**.
  - Kiracılar: **son kiracı en üstte → ilk kiracı en altta**.
- **Sağda (aynı div, sekmeler):** seçili Malik/Kiracı için **Bilgiler, İletişim, Dosyalar, İcralar,
  Hissedarlar, Gönderilen SMS'ler, Gönderilen E-mailler, Şikayetler**.
- **Tahsilat butonu → modal:** sekmeler **Nakit, POS, Çek, Senet, Banka (EFT & Havale)**.

> ⏳ Layout birebir, kullanıcının paylaşacağı **örnek HTML** ile kesinleşecek (bkz. §7 Açık Sorular).

## 2. Mevcut Backend Envanteri (bugün VAR)

| Yetenek | Komut/Sorgu | İzin |
|---|---|---|
| BB (fiziksel birim) | `Unit` entity, `GetBuildingSchemaQuery` (ağaç) | BuildingSchema.Read |
| BB borç durumu | `GetUnitDebtStatusQuery` (tek BB) | tenant.collection.view |
| Tahsilat kaydı | `RecordCollectionCommand` (TBK m.101 dağıtım + yevmiye) | tenant.collection.record |
| Tahsilat geçmişi | `GetUnitCollectionHistoryQuery`, `GetCollectionsByPeriodQuery` | tenant.collection.view |
| Ödeme yöntemleri | enum `PaymentMethod`: Cash, BankTransfer, CreditCard(POS), Check, Other | — |
| Gecikme politikası | `SetLateFeePolicyCommand` (şirket default + bütçe override), `GetLateFeePoliciesQuery` | tenant.latefee.configure |
| Gecikme faizi üretme | `GenerateLateFeeChargesCommand` (KMK m.20 tavanlı, idempotent) | tenant.accrual.generate |
| Hesap kodu / dönem | `GetAccountCodesQuery`, `GetPeriodsQuery` | company.accounting.account-plan.read |
| Dosya saklama altyapısı | `IFileStorage` / `IImageProcessor` (MinIO + SkiaSharp) | — |

## 3. Domain Boşlukları (bugün YOK — sonra hazırlanacak)

| Eksik | Açıklama | Etki |
|---|---|---|
| **Owner (Malik)** | BB ↔ Malik ilişkisi + tapu/tenure tarih aralığı (aktif/geçmiş), hisse | Sol ağaç "Malikler" düğümü |
| **Resident / Tenant (Kiracı)** | BB ↔ Kiracı kira sözleşmesi tarih aralığı (aktif/geçmiş) | Sol ağaç "Kiracılar" düğümü |
| **Borç sorumluluğu kuralı** | Aidat/giderin Malik mi Kiracı mı borcu olduğu | Borç görünümü + tahsilat muhatabı |
| **Hissedarlar** | Bir BB'de çok-malikli pay yapısı (co-ownership) | "Hissedarlar" sekmesi |
| **İcralar** | Yasal takip/icra dosyası takibi | "İcralar" sekmesi |
| **Şikayetler** | Şikayet/talep kaydı (ticketing) | "Şikayetler" sekmesi |
| **SMS gönderim + log** | SMS sağlayıcı entegrasyonu + gönderim kaydı | "Gönderilen SMS'ler" sekmesi |
| **E-posta gönderim + log** | E-posta gönderim + log (altyapı kısmen var olabilir) | "Gönderilen E-mailler" sekmesi |
| **Dosyalar (kişi/BB bazlı)** | IFileStorage var; ama kişi/BB'ye bağlı dosya kayıt domaini yok | "Dosyalar" sekmesi |
| **Çek/Senet portföyü** | Çek/senet giriş, vade, tahsil/karşılıksız; `PaymentMethod.PromissoryNote` (Senet) enum'da yok | Modal "Çek"/"Senet" sekmeleri (tam) |
| **Toplu BB borç özeti** | Tüm BB'lerin kalan/vadesi geçen borcu (3000 satır) | BB listesi borç sütunu |

## 4. Mimari Kararlar (önerilen — kullanıcı onayı bekler, bkz §7)

- **Konum/scope:** Konsol **Company (Site) scope** — kullanıcı "site bağlamına geç" dedi. Sol menüye
  "Bağımsız Bölümler" (bugün Faz 1.7 placeholder) gerçek sayfa olur.
- **İzin modeli:** Mevcut tahsilat/gecikme izinleri `tenant.*`. Company scope konsol için
  ya bunlar Company bağlamında da geçerli sayılır ya da yeni `company.collection.*` izinleri eklenir → **karar gerek**.
- **Veri erişimi:** Yazma EF/CQRS (MediatR + Result + RequirePermission); okuma ağır listelerde Dapper.
- **Çoklu kiracılık:** Tüm yeni entity'ler Main DB'de `ITenantScoped` + `IHasUrlCode` (mevcut desen).
- **Uygulama:** İlk etap ManagementApp (Company scope). (İleride saha/portal app ayrı tartışılır.)

## 5. Faz Planı

> Kural: her faz kapanışında build 0/0 + unit+integration testleri + memory snapshot
> (mevcut çalışma disiplini). "Önce Gecikme" tercihine uygun sıralandı.

### FAZ 1 — Gecikme (Late Fee) UI  ✅ backend HAZIR
Site-seçimli (Bütçe deseni). Üç işlev:
1. **Politika listesi + form** (`GetLateFeePolicies` + `SetLateFeePolicy`): şirket-geneli varsayılan +
   bütçe override; oran (KMK %5 tavan), basit/bileşik, grace gün, gelir hesabı seçimi.
2. **Gecikme faizi üretme** (`GenerateLateFeeCharges`): AsOfDate seç → çalıştır → sonuç özeti
   (kaç BB, kaç tahakkuk, toplam faiz). İdempotent uyarısı.
3. Form Açıklama Bloğu + L10n + yetki gating.
→ Bağımsız, küçük, hemen yapılabilir. **Tek başına teslim edilebilir.**

### FAZ 2 — BB Operasyon İskeleti  ⚠️ 1 yeni read sorgu
- Company scope "Bağımsız Bölümler" sayfası (sol menü gerçek link).
- Body'de tüm BB **ara-bul-seç** — 3000 BB için **server-side sayfalama + virtualize + arama** (§6).
- **YENİ** `GetUnitsWithDebtSummaryQuery` (Dapper, toplu): her BB için kalan + vadesi geçen.
- BB seçilince master-detail iskelet: üstte toplam borç, sağda **Tahsilat** butonu (Faz 3'e bağlanır).
- Bu fazda sol ağaç (Malik/Kiracı) ve sağ sekmeler **placeholder** (Faz 4-5'te dolar).

### FAZ 3 — Tahsilat Modalı  ⚠️ Senet hariç backed
- Modal sekmeleri: **Nakit / POS / Çek / Banka (EFT & Havale)** → `RecordCollection`
  (kasa/banka hesabı seçimi, dönem, tutar, referans). Tahsilat sonrası borç güncellenir + makbuz/yevmiye linki.
- **Senet** sekmesi Faz 6'ya kadar devre dışı veya "Diğer"e map (karar gerek).
- BB borç durumu + tahsilat geçmişi tablosu (mevcut sorgular).

### FAZ 4 — Malik / Kiracı Domain + Sol Ağaç  ❌ YENİ backend
- `Owner` (Malik) + `Resident` (Kiracı) entity'leri: BB ilişkisi + tenure (StartDate/EndDate) + aktiflik.
- **Borç sorumluluğu kuralı** (Malik/Kiracı) — karar gerek (§7).
- CRUD komutları + listeleme sorguları.
- Sol ağaç: **Malikler** (aktif→ilk) / **Kiracılar** (son→ilk) düğümleri; seçim → sağ panel.

### FAZ 5 — Sağ Panel Sekmeleri  ❌ YENİ backend (alt-fazlara bölünür)
- 5a **Bilgiler** (kimlik/künye) · 5b **İletişim** (tel/e-posta/adres) — Malik/Kiracı domaininden.
- 5c **Dosyalar** — IFileStorage üzerine kişi/BB dosya kayıt domaini.
- 5d **Hissedarlar** — çok-malikli pay (co-ownership).
- 5e **İcralar** — yasal takip dosyaları.
- 5f **Şikayetler** — şikayet/talep ticketing.
- 5g **Gönderilen SMS'ler** — SMS sağlayıcı + gönderim log.
- 5h **Gönderilen E-mailler** — e-posta gönderim + log.
→ Her sekme ayrı slice/alt-faz; öncelik kullanıcıyla belirlenecek.

### FAZ 6 — Çek/Senet Portföyü  ❌ YENİ backend
- Çek/senet giriş, vade takvimi, tahsil/karşılıksız/ciro; `PaymentMethod.PromissoryNote` eklenir.
- Tahsilat modalındaki Çek/Senet sekmeleri tam fonksiyon kazanır.

## 6. Performans Stratejisi (3000 BB) — sorun olmaz

- "Tüm BB borç özeti" = **tek Dapper sorgusu** (accrual_detail ⨝ collection_allocation, unit'e göre
  GROUP BY, company filtresi). 3000 satır SQL için önemsiz; uygun index'lerle (unit_id, company_id) hızlı.
- UI tablosu: **server-side sayfalama (MudTable ServerData) veya Virtualize + debounce arama** —
  tüm satırlar DOM'a basılmaz.
- BB **detayı** (borç kırılımı, ağaç, sekmeler) yalnız seçimde **lazy** yüklenir; liste hafif kalır.
- Borç özeti gerekirse kısa-TTL cache (Caching katmanı mevcut).

## 7. Açık Sorular / Kullanıcı Kararları

1. **Örnek HTML** — layout/sekme/modal birebir için bekliyorum (dosya yolu veya içerik).
2. **İzin/scope** — Company scope yeni `company.collection.*` mı, mevcut `tenant.*` mı?
3. **Borç sorumluluğu** — aidat/gider Malik mi Kiracı mı borcu? (gider tipine göre bölünür mü?)
4. **Senet** — Faz 6'ya kadar modalde gizli mi, "Diğer"e map mi?
5. **Sağ panel sekme önceliği** — 8 sekmeden hangileri önce (örn. Bilgiler+İletişim+Dosyalar önce)?
6. **Uygulama** — ManagementApp Company scope yeterli mi, yoksa ayrı portal/saha app düşünülüyor mu?

---

## Özet Sıralama (öneri)

`FAZ 1 (Gecikme, backed)` → `FAZ 2 (BB iskelet + toplu borç)` → `FAZ 3 (Tahsilat modalı)` →
`FAZ 4 (Malik/Kiracı + ağaç)` → `FAZ 5 (sekmeler, alt-fazlar)` → `FAZ 6 (Çek/Senet)`.

Faz 1-3 büyük ölçüde mevcut backend ile çalışır; Faz 4-6 yeni domain gerektirir ("sonra hazırlanacak").
