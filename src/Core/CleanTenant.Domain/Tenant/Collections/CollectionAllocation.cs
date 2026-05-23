using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Collections;

/// <summary>
/// <para>
/// Tahsilat dağıtım satırı — bir <see cref="Collection"/>'ın belirli bir
/// <c>AccrualDetail</c>'e uygulanan kısmı. TBK m.101 uyarınca ödeme en eski
/// vadeli açık borçtan başlayarak dağıtılır.
/// </para>
/// <para>
/// Bir tahakkuk detayının kalan borcu = AccrualDetail.Amount − SUM(bu detaya ait
/// CollectionAllocation.AllocatedAmount).
/// </para>
/// </summary>
public sealed class CollectionAllocation : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Bağlı olduğu tahsilat.</summary>
    public Guid CollectionId { get; set; }

    /// <summary>Ödemenin uygulandığı tahakkuk detayı (BB borç satırı).</summary>
    public Guid AccrualDetailId { get; set; }

    /// <summary>Bu tahakkuk detayına uygulanan tutar.</summary>
    public decimal AllocatedAmount { get; set; }
}
