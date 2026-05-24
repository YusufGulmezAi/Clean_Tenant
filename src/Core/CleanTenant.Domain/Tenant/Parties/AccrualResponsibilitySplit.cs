using CleanTenant.Domain.Tenant.Parties.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Parties;

/// <summary>
/// <para>
/// Bir tahakkuk detayının (BB borç satırı) gün-bazlı sorumluluk parçası. Bir BB'nin
/// bir dönemlik borcu, tahakkuk ayı boyunca aktif tenure'a göre taraflar arasında
/// gün oranıyla bölünür. <c>Σ Amount = AccrualDetail.Amount</c>.
/// </para>
/// <para>
/// Tek-taraf (yaygın) durumda tek satır (%100). Tenure düzeltmesinde (reattribution)
/// satırlar silinip yeniden üretilir; borç toplamı ve tahsilat değişmez.
/// </para>
/// </summary>
public sealed class AccrualResponsibilitySplit : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Bağlı olduğu tahakkuk detayı (BB borç satırı).</summary>
    public Guid AccrualDetailId { get; set; }

    /// <summary>Sorumlu cari kişi.</summary>
    public Guid PartyId { get; set; }

    /// <summary>Taraf türü (Malik / Kiracı).</summary>
    public ResponsibilityKind Kind { get; set; }

    /// <summary>Parçanın kapsadığı ilk gün (dahil).</summary>
    public DateOnly FromDate { get; set; }

    /// <summary>Parçanın kapsadığı son gün (dahil).</summary>
    public DateOnly ToDate { get; set; }

    /// <summary>Parçanın gün sayısı (FromDate–ToDate dahil).</summary>
    public int DayCount { get; set; }

    /// <summary>Bu parçaya düşen tutar (gün oranı × toplam).</summary>
    public decimal Amount { get; set; }
}
