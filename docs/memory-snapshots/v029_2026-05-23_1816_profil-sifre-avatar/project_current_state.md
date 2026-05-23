---
name: Proje Mevcut Durumu — v0.2.14 FAZ 6 Devam (Slice 1-5)
description: Bütçe FAZ 5 + revizyon bitti. FAZ 6 Tahakkuk Motoru — Slice 1-5 tamamlandı (domain, EF, allocator, LRM dağıtım, GenerateBudgetAccrual). Kalan: 6.5b yevmiye, 6.6 fatura, 6.7 direkt, 6.8 Hangfire, 6.9 query, 6.10 test.
type: project
originSessionId: 92894288-df01-4cff-89f3-6fae0e457383
---
Son commit: **`75d7bd1`** (2026-05-23) — `feat(accrual): FAZ 6 Slice 5 — GenerateBudgetAccrualCommand`

---

## FAZ 6 — Tahakkuk Motoru (DEVAM EDİYOR)

| Slice | Commit | Durum | İçerik |
|-------|--------|-------|--------|
| 6.1 | `5d35142` | ✅ | `Accrual` + `AccrualDetail` + `AccrualSource` enum + `AccrualGenerated` event (Tenant/Accruals) |
| 6.2 | `84534d3` | ✅ | EF config (idempotency partial unique) + DbSet + migration `AddAccrualDomain` |
| 6.3 | `4620f47` | ✅ | `IAccountCodeAllocator` — ilk tahakkukta 120.0X.NNN/600.0X.NNN otomatik üretimi (Catalog+Main) |
| 6.4 | `8722c8a` | ✅ | `IDistributionService` (Equal + BySquareMeter) + LRM yuvarlama + 8 test |
| 6.5 | `75d7bd1` | ✅ | `GenerateBudgetAccrualCommand` — çekirdek tahakkuk motoru. Error: ACR-001..009 |
| 6.5b | — | ❌ SIRADA | Yevmiye fişi postingi (Accrual.JournalEntryId şu an null) |
| 6.6 | — | ❌ | `DistributeInvoiceAmongUnitsCommand` (sıcak su/doğalgaz dağıtımı) |
| 6.7 | — | ❌ | `CreateDirectUnitChargeCommand` (depo kira/ürün satış — tek BB) |
| 6.8 | — | ❌ | Hangfire `TahakkukUretmeJob` (aylık otomatik) |
| 6.9 | — | ❌ | Queries (dönem tahakkukları, BB borç durumu) |
| 6.10 | — | ❌ | Faz kapanış testleri |

**Tahakkuk akışı (6.5):** Bütçe yayınlı + dönem penceresi → muhasebe dönemi → idempotency (Force) → aktif versiyon → kalem versiyonları + taksitler → şirket BB'leri (Unit→Building→Parcel→Land→Company) → katılım/muafiyet filtresi → her kalem dağıtım (LRM) → BB başına topla + LineBreakdownJson → AccrualDetail (vade=ertesi ay min DueDay) → ilk tahakkukta hesap kodu tahsisi → Accrual kaydet.

---

## FAZ 5 Revizyonu — TAMAMLANDI ✅ (commit `be499ce`) — Bütçe Tasarım Kararları

Kullanıcı ile tartışma sonrası alınan kararlar (2026-05-22):

1. **BudgetType enum** (Aidat / Yatırım / Kömür / Kuruluş, genişletilebilir) — `Domain/Tenant/Budgeting/Enums/BudgetType.cs`
2. **Unique kısıt** `(CompanyId, FiscalYearId, Type, Title)` — yıl içinde aynı tipte çoklu bütçe (ek aidat, çoklu yatırım)
3. **BudgetTypeMetadata** Catalog DB kataloğu (System scope yönetir) — `Domain/Budgeting/BudgetTypeMetadata.cs`; base hesap kodları (Aidat→120.01/600.01, Yatırım→120.02/600.02, Kömür→120.03/600.03, Kuruluş→120.04/600.04); CatalogSeeder ile 4 tip seed'lendi
4. **Hesap kodu otomasyonu:** İlk tahakkuk anında base kodun altına şirkete özel alt hesap (120.01.001, 120.01.002…). `Budget.ReceivableAccountCodeId` + `IncomeAccountCodeId` (nullable, ilk tahakkukta dolar)
5. **Yevmiye granülaritesi:** Bütçe × Dönem = 1 fiş (kalem-bazlı DEĞİL). Borç 120.0X.NNN / Alacak 600.0X.NNN, toplam tutar. BB kırılımı yardımcı defterde (`AccrualDetail.LineBreakdownJson`)
6. **PaymentSchedule:** Seasonal kaldırıldı, **Installment** eklendi
7. **BudgetLineInstallment** tablosu — taksit planı (Year, Month, Amount=toplam, Label, IsManuallyEdited). Equal modda manuel düzenlenebilir; m²/Arsa modda otomatik eşit bölünür
8. **BudgetLineVersion** Installment konfig: InstallmentStart/End Year/Month + IntervalMonths (1-12)
9. **Budget esnek dönem:** `PeriodStartYear/Month` + `PeriodEndYear/Month` (takvim yılına bağlı değil; MonthlyEqual dönem ay sayısına böler). Verilmezse FiscalYear'dan dolar, mali yıl içi olmalı

**FAZ 6 ek kapsam (kararlaştırıldı):**
- `AccrualSource` enum: Budget / Invoice / DirectCharge
- `DistributeInvoiceAmongUnitsCommand` (sıcak su / doğalgaz faturası dağıtımı) — FAZ 6
- `CreateDirectUnitChargeCommand` (depo kira, ürün satış — tek BB) — FAZ 6
- `BudgetLine.AccountCodeId` → FAZ 9+ Gerçekleşme Modülü'ne ertelendi (gider hesabı anlamı)

**Migrations:** AddBudgetTypeMetadata (Catalog), BudgetTypeAndInstallmentAndPeriod (Main)

---

## FAZ 5 — Bütçe Domain TAMAMLANDI ✅ (Bütçe Adım 0 + 5 Slice)

| Slice | Commit | İçerik |
|-------|--------|--------|
| Adım 0 | `bd76a26` | BudgetPage TenantArea'ya taşındı, 7 permission, brifing |
| Slice 1 | `d304ef7` | `Budget` + `BudgetVersion` aggregate + `BudgetStatus` enum + 3 event |
| Slice 2 | `a2a7fd0` | `ExpenseCategory` + `BudgetLine` + `BudgetLineVersion` + `PaymentSchedule` + `DistributionModel` |
| Slice 3 | `ec0a3c8` | `ParticipationGroup` + `UnitParticipationGroup` + `ExemptionRule` |
| Slice 4a | `d6616d5` | 8 EF Configuration + 8 DbSet + `AddBudgetingDomain` migration + eski `Budget`→`BudgetEntry` rename |
| Slice 4b | `335be9d` | `CreateBudgetCommand` + `PublishBudgetCommand` + `AllowDraftBudgetVersions` migration |
| Slice 4c | `b605db6` | 7 destek komutu: `CreateExpenseCategoryCommand`, `CreateBudgetLineCommand`, `AddBudgetLineToVersionCommand`, `CreateParticipationGroupCommand`, `AddUnitToGroupCommand`, `CreateExemptionRuleCommand`, `ReviseBudgetCommand` |
| Slice 4d | `7848ae9` | 7 query + DTO: budget list/detail/version lines + lines/categories/groups/exemptions |
| Slice 4e | `3fe5446` | 19 validator unit test + 6 DB constraint integration test |

**Domain entity'leri (Tenant/Budgeting):**
- `Budget` (aggregate) — yıllık konteyner; (CompanyId, FiscalYearId) unique; `BudgetStatus` (Draft/Published/Cancelled); `CurrentVersionId`
- `BudgetVersion` — immutable versiyon zinciri; `PublishedAt` + `ValidFrom` nullable (Draft) / dolu (Published); `PreviousVersionId` linked-list
- `ExpenseCategory` — kategori hiyerarşisi
- `BudgetLine` (aggregate) — versiyondan bağımsız tanım; opsiyonel `AccountCodeId` TDHP bağlantısı
- `BudgetLineVersion` — versiyon × kalem snapshot; `PlannedAmount`, `PaymentSchedule`, `DistributionModel`, `ParticipationGroupId`, `DistributionConfig` (JSONB), `IsManualOverride`, `OverrideReason`, `DueDayOfMonth`
- `ParticipationGroup` (aggregate) — katılım grubu
- `UnitParticipationGroup` — junction (Group × Unit, tarih penceresi)
- `ExemptionRule` — muafiyet (Unit × BudgetLine, tarih penceresi, KMK m.18 gerekçe)

**Enum'lar:** `BudgetStatus`, `PaymentSchedule` (MonthlyEqual/AnnualLumpSum/InvoiceBased/Seasonal), `DistributionModel` (Equal/BySquareMeter MVP + Wave 2-3 placeholders)

**Komutlar:** 9 (Create/Publish/Revise budget + 6 destek)

**Queries:** 7 (budget list/detail/version lines + 4 destek listesi)

**Migrations:** 2 (AddBudgetingDomain + AllowDraftBudgetVersions)

**Testler:** 19 unit test (geçti) + 6 integration test (TestContainers'da koşulur)

---

## Önceki TAMAMLANAN Çalışmalar

### Bütçe Adım 0 (Site → Tenant erişim refactor) ✅
- `TenantArea/Budget/{BudgetPage, BudgetVsActualPage}.razor` (route `/tenant/budget/{CompanyId}`)
- 7 yeni `tenant.budget.*` / `tenant.accrual.*` / `tenant.collection.*` permission
- NavMenu Tenant scope'a Bütçe linki (permission-gated)
- `docs/specs/budget-module/05-WORKFLOW-BRIEFING.md` (9 bölüm)

### TDHP Muhasebe Modülü ✅
20 sayfa + 9 raporlama sorgusu (AccountingReader Caching/Readers/) + 21 REST endpoint + DI.

### BuildingSchema FAZ 1-2 ✅
Block→Land rename + yeni Block (kule) + EF migration `AddBuildingSchemaRefactor`.

---

## Sıradaki FAZ'lar (Bütçe Modülü Tamamlanması)

| Faz | İçerik | Süre |
|-----|--------|------|
| **FAZ 6** | Tahakkuk Motoru — `Accrual` + `AccrualDetail` + `UretTahakkukCommand` + 2 dağıtım motoru (Equal, BySquareMeter) + LRM + Hangfire `TahakkukUretmeJob` | ~2,5 gün |
| **FAZ 7A** | Tahsilat — `Collection` + `RecordCollectionCommand` + TBK m.101 partial allocation + otomatik yevmiye fişi | ~1,5 gün |
| **FAZ 7B** | Gecikme + UI — `LateFee` + Hangfire `LateFeeCalculationJob` + KMK m.20 tavanı + ManagementApp/PortalApp sayfaları | ~1,5 gün |
| **FAZ 8** | Test — 3 referans senaryo (12-BB / 120-BB / mid-year revizyon) + tenant izolasyon | ~2 gün |

## Sıradaki BuildingSchema Çalışmaları (FAZ 5-8'den bağımsız)

- **FAZ 3 (BuildingSchema):** Block (kule) CRUD commands + application layer
- **FAZ 4 (BuildingSchema):** Blazor UI — Tree view kule formları + Excel import

---

**Why:** FAZ 5 Bütçe Domain biten ilk büyük "yeni özellik" fazı. Veri modeli + komut/query katmanı + EF + migration + testler tamamlandı. Sırada FAZ 6: bu domain üzerinden aylık tahakkuk üretmek için Accrual motoru.

**How to apply:** `git log --oneline -15` ile gerçek durumu doğrula. Yeni iş başlarken FAZ 6 mimari haritasını çıkar (Tahakkuk + AccrualDetail + LRM + Hangfire) veya BuildingSchema FAZ 3'e geç.
