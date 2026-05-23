---
name: Proje Mevcut Durumu — v0.2.13.a Bütçe Adım 0
description: Bütçe modülü Tenant scope'a taşındı + 7 yeni permission + brifing dokümanı. Muhasebe TDHP tamamı bitti. BuildingSchema FAZ 3 ve Bütçe FAZ 5 sırada.
type: project
originSessionId: 92894288-df01-4cff-89f3-6fae0e457383
---
Son commit: **`bd76a26`** (2026-05-22) — `feat(budget): Adim 0 — erişim refactor (Site → Tenant scope)`

---

## Bütçe Adım 0 — TAMAMLANDI ✅ (2026-05-22)

| Değişiklik | Detay |
|------------|-------|
| Yeni sayfa | `TenantArea/Budget/BudgetPage.razor` — route `/tenant/budget` (site seçimi) + `/tenant/budget/{CompanyId:guid}` (planlama) |
| Yeni sayfa | `TenantArea/Budget/BudgetVsActualPage.razor` — route `/tenant/budget/{CompanyId:guid}/vs-actual` |
| Silindi | Eski `CompanyArea/Accounting/BudgetPage.razor` + `Reports/BudgetVsActualPage.razor` |
| Yeni permissions | `tenant.budget.view/edit/publish`, `tenant.accrual.generate`, `tenant.collection.view/record`, `tenant.latefee.configure` (7 adet, ScopeLevel.Tenant) |
| Silinen permissions | `company.accounting.budget.read/write` |
| NavMenu | Muhasebe alt grubundan Bütçe çıkarıldı; Tenant scope'a permission-gated link eklendi |
| LocalizationCatalog | `NavMenu.Budget` (TR "Bütçe" / EN "Budget") |
| Doküman | `docs/specs/budget-module/05-WORKFLOW-BRIEFING.md` (9 bölüm; Tenant kararı, aktör haritası, iş akışı, otomatik sistem, yetkiler, yol haritası) |

Build: 0 hata 0 uyarı.

---

## TDHP Muhasebe Modülü — TAMAMEN TAMAMLANDI ✅

| Katman | Durum |
|--------|-------|
| Domain (13 entity + 12 enum) | ✅ |
| EF Core Config (13 tablo) | ✅ |
| Application — AccountCodes/CostCenters/FiscalYears/Periods/JournalEntries/Invoices/BankAccounts/Reports | ✅ |
| Infrastructure — DbSet, Migration, DI, Seed (~200 TDHP hesap) | ✅ |
| Blazor — 20 sayfa (Bütçe sayfaları artık Tenant scope) | ✅ |
| NavMenu — Muhasebe grubu (Bütçe çıkarıldı) | ✅ |
| REST API — 21 endpoint (AccountingEndpoints.cs) | ✅ |
| AccountingReader — 9 Dapper sorgusu (`Infrastructure.Caching/Readers/`) | ✅ |

---

## BuildingSchema Refactoru — KISMİ DEVAM

| Faz | İçerik | Durum |
|-----|--------|-------|
| FAZ 1 | Land/Block domain refactor | ✅ v0.2.13 |
| FAZ 2 | EF migration (`AddBuildingSchemaRefactor`) | ✅ |
| FAZ 3 | **Block (kule) CRUD commands** — application layer | ❌ SIRADA |
| FAZ 4 | **Blazor UI** — Tree view, kule CRUD formları | ❌ SIRADA |

**Mevcut BuildingSchema application:**
- Buildings: Create/Update/Delete/Reorder ✅
- Units: Create/Update/Delete/Reorder ✅
- Parcels: Update/Delete ✅
- Lands: Query ✅
- **Blocks: HİÇ YOK** ❌ (FAZ 3 hedefi)

---

## Bütçe Modülü — FAZ 5-8 SIRADA

Adım 0 sonrası FAZ 5 başlamak için hazır. Brifing'e (`05-WORKFLOW-BRIEFING.md`) göre:

- **FAZ 5 — Bütçe Domain (~2 gün)** — Butce, ButceVersiyonu, ButceKalemi, KalemVersiyonu, OdemePlani, DagitimModeli (enum), GiderPaylasimGrubu, KatilimGrubu, KapsamKurali, MuafiyetKurali + Create/Add/Publish/Revise komutları + migration
- **FAZ 6 — Tahakkuk Motoru (~2,5 gün)** — Tahakkuk + TahakkukDetayi + UretTahakkukCommand + EsitDagitim/M²Dagitim motorları + LRM + Hangfire `TahakkukUretmeJob`
- **FAZ 7A — Tahsilat (~1,5 gün)** — Tahsilat + KaydetTahsilatCommand + TBK m.101 partial payment + otomatik yevmiye fişi
- **FAZ 7B — Gecikme + UI (~1,5 gün)** — GecikmeHesabi + Hangfire `GecikmeHesaplamaJob` + KMK m.20 tavanı + ManagementApp Tenant Bütçe sayfaları + PortalApp 3 malik sayfası (Borç, Tahakkuk Geçmişi, Ödeme Geçmişi)
- **FAZ 8 — Test (~2 gün)** — 3 referans senaryo (12-BB apartman, 120-BB site, mid-year revizyon) + tenant isolation testleri

**Toplam:** ~10-11 iş günü.

---

## Önceki Faz Geçmişi

- **v0.1.x** — Backend: Auth + 2FA + Multi-scope + 146 test
- **v0.2.1–v0.2.7** — ManagementApp Shell, Auth UI, Company/Role CRUD, Audit, PortalApp
- **v0.2.10** — Lokalizasyon (TR/EN/AR/RU/DE + admin sayfası)
- **v0.2.11** — PermissionPicker + CompanyForm + Tab'lı Tenant form
- **v0.2.12** — Kullanıcı yönetimi revizyonu
- **v0.2.13** — BuildingSchema FAZ 1-2 + TDHP Muhasebe altyapı + API + Reader
- **v0.2.13.a** — Bütçe Adım 0 (erişim refactor + brifing)

**Why:** Bütçe modülünün üst seviyesi (UI/route/yetki) Tenant scope'a alındı. Veri modeli FAZ 5'te yeniden tasarlanacak; mevcut basit `Budget` entity'si geçici kullanılıyor.

**How to apply:** `git log --oneline -5` ile gerçek durumu doğrula. Sıradaki adım kullanıcı kararına bağlı: (1) BuildingSchema FAZ 3 (Block CRUD), (2) Bütçe FAZ 5 (yeni domain modeli) ya da (3) başka öncelik.
