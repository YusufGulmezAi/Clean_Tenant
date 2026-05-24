using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accruals;
using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.Domain.Tenant.Collections;
using CleanTenant.Domain.Tenant.Companies;
using CleanTenant.Domain.Tenant.LateFees;
using CleanTenant.Domain.Tenant.Parties;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Common.Persistence;

/// <summary>
/// <para>
/// Main veri tabanı için Application katmanının okuduğu soyutlama.
/// Concrete <c>MainDbContext</c> Infrastructure.Persistence projesindedir.
/// </para>
/// <para>
/// Main DB tenant iş varlıklarını taşır (Company, BuildingSchema hiyerarşisi).
/// Hibrit multi-tenancy: shared-mode'da tüm tenant'lar paylaşır,
/// <c>HasDedicatedDatabase=true</c> tenant'lar için ayrı DB. Global query filter
/// <see cref="CleanTenant.SharedKernel.Entities.ITenantScoped"/> entity'lerinde
/// otomatik <c>tenant_id</c> filtresi uygular.
/// </para>
/// </summary>
public interface IMainDbContext
{
    /// <summary>Şirket (Site) kayıtları (tenant-scoped).</summary>
    DbSet<Company> Companies { get; }

    /// <summary>Ada (Land) kayıtları — yapı şeması 1. seviye.</summary>
    DbSet<Land> Lands { get; }

    /// <summary>Yapı blok/kule kayıtları — yapı şeması 4. seviye (opsiyonel).</summary>
    DbSet<Block> Blocks { get; }

    /// <summary>Parsel kayıtları — yapı şeması 2. seviye.</summary>
    DbSet<Parcel> Parcels { get; }

    /// <summary>Yapı (Building) kayıtları — yapı şeması 3. seviye.</summary>
    DbSet<Building> Buildings { get; }

    /// <summary>Bağımsız bölüm (Unit) kayıtları — yapı şeması 4. seviye.</summary>
    DbSet<Unit> Units { get; }

    // ── Muhasebe Modülü ──────────────────────────────────────────────────────
    /// <summary>Hesap kodları (TDHP 3-kademeli hiyerarşi).</summary>
    DbSet<AccountCode> AccountCodes { get; }

    /// <summary>Maliyet merkezleri.</summary>
    DbSet<CostCenter> CostCenters { get; }

    /// <summary>Mali yıllar.</summary>
    DbSet<FiscalYear> FiscalYears { get; }

    /// <summary>Muhasebe dönemleri (aylık).</summary>
    DbSet<AccountingPeriod> AccountingPeriods { get; }

    /// <summary>Yevmiye fişleri.</summary>
    DbSet<JournalEntry> JournalEntries { get; }

    /// <summary>Yevmiye satırları.</summary>
    DbSet<JournalLine> JournalLines { get; }

    /// <summary>Fiş sıra numarası sayaçları.</summary>
    DbSet<EntrySequence> EntrySequences { get; }

    /// <summary>Muhasebe banka hesapları (LookUp.BankAccount'tan bağımsız).</summary>
    DbSet<BankAccount> AccountingBankAccounts { get; }

    /// <summary>Fatura kayıtları (gelen/giden).</summary>
    DbSet<Invoice> Invoices { get; }

    /// <summary>
    /// Legacy bütçe kayıtları (dönem + hesap kodu granülaritesi). FAZ 5 Slice 4d'de
    /// yeni <c>Budget</c> aggregate ile değiştirilecek; o zaman bu DbSet kaldırılır.
    /// </summary>
    DbSet<BudgetEntry> BudgetEntries { get; }

    /// <summary>Şirket muhasebe yapılandırmaları.</summary>
    DbSet<AccountingSettings> AccountingSettings { get; }

    // ── Bütçe Modülü (FAZ 5+) ───────────────────────────────────────────────
    /// <summary>Yıllık bütçe aggregate'leri.</summary>
    DbSet<Budget> Budgets { get; }

    /// <summary>Yayınlanmış bütçe versiyonları (V1, V2, …).</summary>
    DbSet<BudgetVersion> BudgetVersions { get; }

    /// <summary>Gider kategorileri (hiyerarşik).</summary>
    DbSet<ExpenseCategory> ExpenseCategories { get; }

    /// <summary>Bütçe kalemleri (line tanımları).</summary>
    DbSet<BudgetLine> BudgetLines { get; }

    /// <summary>Bütçe kalemi versiyon snapshot'ları (planlanan tutar + dağıtım).</summary>
    DbSet<BudgetLineVersion> BudgetLineVersions { get; }

    /// <summary>Taksit planı satırları (Installment kalemler için).</summary>
    DbSet<BudgetLineInstallment> BudgetLineInstallments { get; }

    /// <summary>Katılım grupları (Havuz, Ticari, …).</summary>
    DbSet<ParticipationGroup> ParticipationGroups { get; }

    /// <summary>Bağımsız Bölüm ↔ Katılım Grubu üyelik kayıtları.</summary>
    DbSet<UnitParticipationGroup> UnitParticipationGroups { get; }

    /// <summary>Muafiyet kuralları.</summary>
    DbSet<ExemptionRule> ExemptionRules { get; }

    // ── Tahakkuk Modülü (FAZ 6+) ─────────────────────────────────────────────
    /// <summary>Tahakkuk başlıkları (Bütçe/Fatura/Doğrudan).</summary>
    DbSet<Accrual> Accruals { get; }

    /// <summary>Tahakkuk detayları (BB-bazlı yardımcı defter).</summary>
    DbSet<AccrualDetail> AccrualDetails { get; }

    // ── Tahsilat Modülü (FAZ 7+) ─────────────────────────────────────────────
    /// <summary>Tahsilat başlıkları (BB ödemeleri).</summary>
    DbSet<Collection> Collections { get; }

    /// <summary>Tahsilat dağıtım satırları (AccrualDetail'lere).</summary>
    DbSet<CollectionAllocation> CollectionAllocations { get; }

    // ── Gecikme Faizi Modülü (FAZ 7B+) ───────────────────────────────────────
    /// <summary>Gecikme faizi politikaları (şirket varsayılanı + bütçe override).</summary>
    DbSet<LateFeePolicy> LateFeePolicies { get; }

    // ── Cari (Party) Modülü (F0+) ────────────────────────────────────────────
    /// <summary>Cari kişiler (malik/kiracı/iletişim).</summary>
    DbSet<Party> Parties { get; }

    /// <summary>Malik tenure kayıtları (pay% + müteselsil).</summary>
    DbSet<UnitOwnership> UnitOwnerships { get; }

    /// <summary>Kiracı tenure kayıtları.</summary>
    DbSet<UnitTenancy> UnitTenancies { get; }

    /// <summary>İletişim kişisi tenure kayıtları.</summary>
    DbSet<UnitContact> UnitContacts { get; }

    /// <summary>Tahakkuk detayı gün-bazlı sorumluluk parçaları.</summary>
    DbSet<AccrualResponsibilitySplit> AccrualResponsibilitySplits { get; }

    /// <summary>Bekleyen değişiklikleri persist eder.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
