---
name: Proje Mevcut Durumu — v0.2.14 FAZ 5 Tamamlandı
description: Bütçe Domain (FAZ 5) full bitti: 8 entity + 9 komut + 7 query + 19 unit test + 6 integration test. FAZ 6 (Tahakkuk Motoru) sırada.
type: project
originSessionId: 92894288-df01-4cff-89f3-6fae0e457383
---
Son commit: **`3fe5446`** (2026-05-22) — `test(budget): FAZ 5 Slice 4e — faz kapanış testleri`

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
