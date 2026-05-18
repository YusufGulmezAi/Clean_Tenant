using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.Domain.Tenant.Companies;
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

    /// <summary>Ada (Block) kayıtları — yapı şeması 1. seviye.</summary>
    DbSet<Block> Blocks { get; }

    /// <summary>Parsel kayıtları — yapı şeması 2. seviye.</summary>
    DbSet<Parcel> Parcels { get; }

    /// <summary>Yapı (Building) kayıtları — yapı şeması 3. seviye.</summary>
    DbSet<Building> Buildings { get; }

    /// <summary>Bağımsız bölüm (Unit) kayıtları — yapı şeması 4. seviye.</summary>
    DbSet<Unit> Units { get; }

    /// <summary>Bekleyen değişiklikleri persist eder.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
