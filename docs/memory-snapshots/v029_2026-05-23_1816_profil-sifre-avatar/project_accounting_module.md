---
name: TDHP Muhasebe Modülü Planı
description: Company bazlı Tek Düze Hesap Planı muhasebe modülü mimari kararları ve faz planı
type: project
originSessionId: 3cdb2d7b-655e-4ae3-afeb-c6e2b3816486
---
Muhasebe modülü tam mimari tasarımı tamamlandı (Rev 2: 2026-05-21). Plan dosyası:
`C:\Users\yusuf\.claude\plans\net-10-da-clean-noble-teacup.md`

**Why:** Her Tenant altındaki Company'lerin (Site/AVM/Marina) muhasebesini TDHP + VUK + KDV + e-Defter kanuni standartlarına göre yönetmek için.

**Kritik Kararlar:**
- Hibrit THP şablon: Catalog DB'de ChartOfAccountsTemplate (global) + Company aktive edilince Main DB'ye kopyalama
- Ayrı Accounting DB yok — MainDbContext'e ekleniyor
- Takvim dışı hesap dönemi: FiscalYear.Year kaldırıldı → Label + StartDate + EndDate (01.05.2026-30.04.2027 gibi)
- AccountCode.IsDetail: true → yalnızca yaprak hesaplara fiş girilebilir (TDHP zorunluluğu)
- Dual-control onay akışı: AccountingSettings.RequireApproval ile şirket bazlı
- VUK fiş iptali: Soft-delete değil, ters fiş (contra entry) — VoidReason zorunlu
- Enflasyon Muhasebesi (TMS 29): 2024+ zorunlu, IsMonetary + InflationIndex altyapısı
- e-Defter stub: EDefterXml alanı, GİB gönderimi v2.0
- IAccountingService: diğer modüller için public sözleşme
- 21 granüler permission (company.accounting.* prefix)
- Dapper: 9 rapor sorgusu (KDV + Defteri Kebir + Bütçe-Gerçekleşen dahil)

**11 Yeni Entity (Main DB):** AccountCode, CostCenter, FiscalYear, AccountingPeriod, JournalEntry, JournalLine, EntrySequence, BankAccount, Invoice, Budget, AccountingSettings
**2 Yeni Entity (Catalog DB):** ChartOfAccountsTemplate, InflationIndex

**Faz Planı:** 10 faz. MVP = Faz 1-5.

**How to apply:** Implementasyon başlamadan Bölüm 14'teki 8 tasarım kararını kullanıcıyla netleştir. Özellikle dual-control default, KDV periyodu ve enflasyon muhasebesi etkinleştirme kritik.
