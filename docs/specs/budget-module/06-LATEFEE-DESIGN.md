# FAZ 7B — Gecikme Faizi (Late Fee) Tasarımı

> Karar tarihi: 2026-05-24 · Kullanıcı onaylı · Mevzuat: KMK m.20, TBK m.101
> Kaynak spec: `01-SDD-v1.0.md` FR-18, FR-19, Bölüm 8.4–8.5, 13.2–13.3

## 1. Kullanıcı Kararları

| # | Konu | Karar |
|---|------|-------|
| D1 | Oran kaynağı | **Hem Company hem Bütçe seviyesinde** ayarlanabilir → hiyerarşik `LateFeePolicy` (Bütçe override → Company varsayılan) |
| D2 | Faiz tipi | **Basit faiz** (MVP). `IsCompound` flag entity'de var, kapalı; bileşik ileride |
| D3 | Tahakkuk temsili | **`AccrualSource.LateFee`** — mevcut Accrual/AccrualDetail + AccrualJournalPoster yeniden kullanılır |
| D4 | Tahsilat tahsis sırası | **Önce gecikme, sonra anapara** (TBK m.101), en eski vadeden başlayarak |

## 2. Mevzuat Sabitleri

- **KMK m.20:** Gecikme tazminatı tavanı **aylık %5**. Sistem-geneli kilitli sabit
  (`RegulatoryLimits.KmkM20MonthlyCapPercent = 5m`). Politika oranı bu tavanla
  sınırlanır (`min(policyRate, cap)`). MVP'de domain sabiti; ileride SystemAdmin
  yönetimli `MevzuatTavanlari` kataloğuna taşınır.
- **TBK m.101:** Varsayılan tahsis önce-faiz-sonra-anapara. UI'dan değiştirilebilirlik
  Wave 2 (şimdilik sabit varsayılan).

## 3. Veri Modeli

### LateFeePolicy (`Tenant/LateFees/LateFeePolicy.cs`) — IAggregateRoot
| Alan | Açıklama |
|------|----------|
| TenantId, CompanyId | Zorunlu, scope |
| BudgetId (nullable) | `null` = şirket-geneli varsayılan; dolu = o bütçeye özel override |
| MonthlyRatePercent | Aylık oran (örn. 3.00 = %3). DB CHECK: > 0 |
| IsCompound | Bileşik mi (MVP: false) |
| GraceDays | Vade sonrası ödemesiz gün (DB CHECK: ≥ 0) |
| IncomeAccountCodeId | Gecikme geliri hesabı (yaprak; örn. 642/649) |
| IsActive | Aktif mi |

**Partial unique index'ler (PostgreSQL):**
- Şirket-geneli: `UNIQUE (company_id) WHERE budget_id IS NULL AND is_deleted = false`
- Bütçe-override: `UNIQUE (budget_id) WHERE budget_id IS NOT NULL AND is_deleted = false`

### AccrualSource enum
`LateFee = 3` eklenir. İdempotency partial index `source = 0` filtreli olduğundan
LateFee tahakkukları kısıt dışıdır (bir dönemde birden çok run serbest).

### Gecikme tahakkuğu (LateFee Accrual)
- Receivable = **gecikilen anapara detayının 120.0X.NNN hesabı** (yeniden kullanılır;
  ayrı alacak hesabı açılmaz).
- Income = **policy.IncomeAccountCodeId**.
- AccrualJournalPoster olduğu gibi çalışır: Borç 120 (toplam) / Alacak gecikme-geliri.

## 4. Hesaplama Motoru — `GenerateLateFeeChargesCommand(CompanyId, AsOfDate)`

1. Şirketin **açık + vadesi geçmiş** anapara detaylarını yükle
   (`Source ∈ {Budget, Invoice, DirectCharge}`, `Remaining = Amount − Σtahsis > 0`,
   `DueDate + grace < AsOfDate`).
2. **BB başına** (unit):
   - `earliest` = en eski vadeli açık borç. `policy = resolve(earliest.BudgetId, CompanyId)`
     (bütçe override → şirket varsayılan). Politika yoksa BB atlanır.
   - `rate = min(policy.MonthlyRatePercent, KMK tavan)`.
   - `computedOwed = Σ_d remaining_d × rate/100 × overdueDays_d/30` (basit faiz, kuruşa yuvarla).
   - `alreadyCharged = Σ mevcut LateFee detay tutarı (bu BB)`.
   - `delta = computedOwed − alreadyCharged`; `delta ≤ 0` ise atla (idempotent/incremental).
3. BB'leri **(receivable, income) hesap çiftine** göre grupla; grup başına 1 LateFee Accrual,
   detaylar BB-bazlı `delta`. `AccrualDetail.DueDate = BB'nin en eski açık vadesi`.
4. Her accrual için AccrualJournalPoster ile Posted yevmiye.

**MVP basitleştirmeleri (dokümante):**
- BB'nin tüm borçlarına **tek politika** (en eski borcun politikası) uygulanır.
  Farklı bütçelerde farklı oran karması Wave 2.
- Basit faiz, cari kalan anapara üzerinden; gün-bazlı bakiye entegrasyonu (path-dependent
  tam doğruluk) Hangfire günlük job ile (FAZ 6.8) gelir.
- Hangfire ertelendiği için tetikleme **manuel komut** (FAZ 6'daki `GenerateBudgetAccrual`
  deseniyle aynı). İzin: `tenant.accrual.generate`.

## 5. Tahsilat Güncellemesi (TBK m.101)

`RecordCollectionCommandHandler` açık detay sıralaması:
`OrderBy(DueDate) → ThenByDescending(Source == LateFee) → ThenBy(Id)`
→ en eski vade içinde önce gecikme, sonra anapara. (Projeksiyona `a.Source` eklenir.)

## 6. Slice Planı

| Slice | İçerik | Migration |
|-------|--------|-----------|
| 7B.1 | Domain: `AccrualSource.LateFee`, `LateFeePolicy`, `RegulatoryLimits`, `LateFeesGenerated` event | — |
| 7B.2 | EF config + DbSet + migration `AddLateFeePolicy` | ✅ |
| 7B.3 | `SetLateFeePolicyCommand` + validator + `GetLateFeePoliciesQuery` | — |
| 7B.4 | `GenerateLateFeeChargesCommand` + `ILateFeeCalculator` + `ILateFeePolicyResolver` | — |
| 7B.5 | `RecordCollection` TBK m.101 sıralama güncellemesi | — |
| 7B.6 | Faz kapanış testleri (calculator + resolver + validator + constraint + tahsis sırası) | — |

Hata kodları: `LF-001..` (hesaplama/policy), `SetLateFeePolicy` için `LFP-001..`.
