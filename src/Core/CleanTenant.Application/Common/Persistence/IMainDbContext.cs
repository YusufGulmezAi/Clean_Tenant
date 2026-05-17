using CleanTenant.Domain.Tenant.Companies;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Common.Persistence;

/// <summary>
/// <para>
/// Main veri tabanı için Application katmanının okuduğu soyutlama.
/// Concrete <c>MainDbContext</c> Infrastructure.Persistence projesindedir.
/// </para>
/// <para>
/// Main DB tenant iş varlıklarını taşır (Company, ileride Building/Unit/Invoice).
/// Hibrit multi-tenancy: shared-mode'da tüm tenant'lar paylaşır,
/// <c>HasDedicatedDatabase=true</c> tenant'lar için ayrı DB. Global query filter
/// <see cref="CleanTenant.SharedKernel.Entities.ITenantScoped"/> entity'lerinde
/// otomatik <c>tenant_id</c> filtresi uygular.
/// </para>
/// </summary>
public interface IMainDbContext
{
    /// <summary>Şirket kayıtları (tenant-scoped).</summary>
    DbSet<Company> Companies { get; }

    /// <summary>Bekleyen değişiklikleri persist eder.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
