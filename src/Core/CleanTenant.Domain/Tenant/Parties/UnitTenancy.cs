using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Parties;

/// <summary>
/// <para>
/// Kiracı tenure kaydı — bir <see cref="Party"/>'nin bir Bağımsız Bölümdeki kira
/// dönemini (<see cref="StartDate"/>–<see cref="EndDate"/>) tutar. Bir BB'de aynı
/// anda tek aktif kiracı varsayılır (uygulama seviyesi kontrol).
/// </para>
/// <para>
/// Sorumluluk modu <c>TenantThenOwner</c> olan bütçelerde, tahakkuk döneminde aktif
/// kiracı varsa borç kiracıya yansır (bkz. IResponsibilityResolver).
/// </para>
/// </summary>
public sealed class UnitTenancy : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Kiralanan Bağımsız Bölüm.</summary>
    public Guid UnitId { get; set; }

    /// <summary>Kiracı (cari kişi).</summary>
    public Guid PartyId { get; set; }

    /// <summary>Kira başlangıcı / giriş tarihi (dahil).</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Kira bitişi / çıkış tarihi (dahil). Hâlâ oturuyorsa null.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Açıklama (örn. kira sözleşme no). Opsiyonel.</summary>
    public string? Notes { get; set; }
}
