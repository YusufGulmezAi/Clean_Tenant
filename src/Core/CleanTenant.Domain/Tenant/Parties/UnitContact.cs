using CleanTenant.Domain.Tenant.Parties.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Parties;

/// <summary>
/// İletişim kişisi tenure kaydı — bir BB için acil durum / vekâlet iletişim kişisi.
/// Bu kişiler <b>borçlu olmaz</b> ve <b>tebligat almaz</b>; yalnız iletişim içindir.
/// </summary>
public sealed class UnitContact : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>İlgili Bağımsız Bölüm.</summary>
    public Guid UnitId { get; set; }

    /// <summary>İletişim kişisi (cari kişi).</summary>
    public Guid PartyId { get; set; }

    /// <summary>İlişki rolü (vekil / aile / avukat / mirasçı / diğer).</summary>
    public ContactRole ContactRole { get; set; }

    /// <summary>İlişki başlangıcı (dahil).</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>İlişki bitişi (dahil). Açık uçluysa null.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Açıklama / yetki kapsamı. Opsiyonel.</summary>
    public string? Notes { get; set; }
}
