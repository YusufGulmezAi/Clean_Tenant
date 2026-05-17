using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Companies;
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
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MainDbContext).Assembly);

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
