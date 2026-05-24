using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accruals;
using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.Domain.Tenant.Collections;
using CleanTenant.Domain.Tenant.Companies;
using CleanTenant.Domain.Tenant.LateFees;
using CleanTenant.Domain.Tenant.Parties;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Persistence.Main;

/// <summary>
/// <para>
/// Tenant iş varlıkları için Main DB'nin EF Core context'i. Hibrit
/// multi-tenancy: shared-mode'da tüm tenant'lar paylaşır,
/// <see cref="ITenantScoped"/> entity'leri için global query filter ile
/// <c>tenant_id</c> izolasyonu sağlanır.
/// </para>
/// <para>
/// <b>Audit:</b> Catalog DbContext gibi <c>AuditingInterceptor</c> +
/// <c>FullAuditInterceptor</c> + <c>UrlCodeGeneratingInterceptor</c> bağlanır
/// (Persistence DependencyInjection üzerinden). Tüm yazımlar tek
/// <c>audit_entries</c> tablosuna gider.
/// </para>
/// </summary>
public sealed class MainDbContext : DbContext, IMainDbContext
{
    private readonly ITenantContext _tenantContext;

    /// <summary>EF DI ctor.</summary>
    public MainDbContext(DbContextOptions<MainDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    /// <inheritdoc />
    public DbSet<Company> Companies => Set<Company>();

    /// <inheritdoc />
    public DbSet<Land> Lands => Set<Land>();

    /// <inheritdoc />
    public DbSet<Block> Blocks => Set<Block>();

    /// <inheritdoc />
    public DbSet<Parcel> Parcels => Set<Parcel>();

    /// <inheritdoc />
    public DbSet<Building> Buildings => Set<Building>();

    /// <inheritdoc />
    public DbSet<Unit> Units => Set<Unit>();

    // ── Muhasebe Modülü ──────────────────────────────────────────────────────
    /// <summary>Hesap kodları (TDHP 3-kademeli hiyerarşi).</summary>
    public DbSet<AccountCode> AccountCodes => Set<AccountCode>();

    /// <summary>Maliyet merkezleri.</summary>
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();

    /// <summary>Mali yıllar.</summary>
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();

    /// <summary>Muhasebe dönemleri (aylık).</summary>
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();

    /// <summary>Yevmiye fişleri.</summary>
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

    /// <summary>Yevmiye satırları.</summary>
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();

    /// <summary>Fiş sıra numarası sayaçları.</summary>
    public DbSet<EntrySequence> EntrySequences => Set<EntrySequence>();

    /// <summary>Muhasebe banka hesapları (LookUp.BankAccount'tan bağımsız).</summary>
    public DbSet<BankAccount> AccountingBankAccounts => Set<BankAccount>();

    /// <summary>Fatura kayıtları (gelen/giden).</summary>
    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <summary>Legacy bütçe kayıtları (FAZ 5 Slice 4d'de yeni model ile değiştirilecek).</summary>
    public DbSet<BudgetEntry> BudgetEntries => Set<BudgetEntry>();

    /// <summary>Şirket muhasebe yapılandırmaları.</summary>
    public DbSet<AccountingSettings> AccountingSettings => Set<AccountingSettings>();

    // ── Bütçe Modülü (FAZ 5+) ───────────────────────────────────────────────
    /// <summary>Yıllık bütçe aggregate'leri.</summary>
    public DbSet<Budget> Budgets => Set<Budget>();

    /// <summary>Yayınlanmış bütçe versiyonları.</summary>
    public DbSet<BudgetVersion> BudgetVersions => Set<BudgetVersion>();

    /// <summary>Gider kategorileri.</summary>
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

    /// <summary>Bütçe kalemi tanımları.</summary>
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();

    /// <summary>Bütçe kalemi versiyon snapshot'ları.</summary>
    public DbSet<BudgetLineVersion> BudgetLineVersions => Set<BudgetLineVersion>();

    /// <summary>Taksit planı satırları.</summary>
    public DbSet<BudgetLineInstallment> BudgetLineInstallments => Set<BudgetLineInstallment>();

    /// <summary>Katılım grupları.</summary>
    public DbSet<ParticipationGroup> ParticipationGroups => Set<ParticipationGroup>();

    /// <summary>BB ↔ katılım grubu junction.</summary>
    public DbSet<UnitParticipationGroup> UnitParticipationGroups => Set<UnitParticipationGroup>();

    /// <summary>Muafiyet kuralları.</summary>
    public DbSet<ExemptionRule> ExemptionRules => Set<ExemptionRule>();

    // ── Tahakkuk Modülü (FAZ 6+) ─────────────────────────────────────────────
    /// <summary>Tahakkuk başlıkları.</summary>
    public DbSet<Accrual> Accruals => Set<Accrual>();

    /// <summary>Tahakkuk detayları (BB-bazlı).</summary>
    public DbSet<AccrualDetail> AccrualDetails => Set<AccrualDetail>();

    // ── Tahsilat Modülü (FAZ 7+) ─────────────────────────────────────────────
    /// <summary>Tahsilat başlıkları.</summary>
    public DbSet<Collection> Collections => Set<Collection>();

    /// <summary>Tahsilat dağıtım satırları.</summary>
    public DbSet<CollectionAllocation> CollectionAllocations => Set<CollectionAllocation>();

    // ── Gecikme Faizi Modülü (FAZ 7B+) ───────────────────────────────────────
    /// <summary>Gecikme faizi politikaları (şirket varsayılanı + bütçe override).</summary>
    public DbSet<LateFeePolicy> LateFeePolicies => Set<LateFeePolicy>();

    // ── Cari (Party) Modülü (F0+) ────────────────────────────────────────────
    /// <summary>Cari kişiler (malik/kiracı/iletişim).</summary>
    public DbSet<Party> Parties => Set<Party>();

    /// <summary>Malik tenure kayıtları.</summary>
    public DbSet<UnitOwnership> UnitOwnerships => Set<UnitOwnership>();

    /// <summary>Kiracı tenure kayıtları.</summary>
    public DbSet<UnitTenancy> UnitTenancies => Set<UnitTenancy>();

    /// <summary>İletişim kişisi tenure kayıtları.</summary>
    public DbSet<UnitContact> UnitContacts => Set<UnitContact>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(MainDbContext).Assembly,
            type => type.Namespace?.StartsWith(typeof(MainDbContext).Namespace + ".Configurations", StringComparison.Ordinal) == true);

        // Global query filter: tüm ITenantScoped entity'lerde
        // - soft-delete (IsDeleted=false)
        // - aktif tenant context'i (TenantId == _tenantContext.TenantId)
        // System scope (TenantId=null) için filter pass-through (null match yok)
        // — System operatör cross-tenant erişim için IgnoreQueryFilters() kullanır.
        ApplyTenantGlobalQueryFilters(modelBuilder);
    }

    private void ApplyTenantGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                ApplyFilterForEntity(modelBuilder, entityType.ClrType);
            }
        }
    }

    private void ApplyFilterForEntity(ModelBuilder modelBuilder, Type clrType)
    {
        var method = typeof(MainDbContext)
            .GetMethod(nameof(SetGlobalQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(clrType);
        method.Invoke(this, [modelBuilder]);
    }

    private void SetGlobalQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantScoped
    {
        // (e.TenantId == _tenantContext.TenantId) — null vs null match etmemesi için
        // bilinçli olarak '==' kullanılır (PostgreSQL'de null == null false döner).
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
    }
}
