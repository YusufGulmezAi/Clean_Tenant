using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Parties;

/// <summary>
/// <para>
/// Malik tenure kaydı — bir <see cref="Party"/>'nin bir Bağımsız Bölüm üzerindeki
/// mülkiyetini, tarih aralığı (<see cref="StartDate"/>–<see cref="EndDate"/>) ve
/// pay oranı (<see cref="SharePercent"/>) ile tutar. Çok-malikli (Hissedar) BB'de
/// aynı anda birden çok aktif kayıt olur; paylar toplamı ~100 beklenir (esnek).
/// </para>
/// <para>
/// Borç çözümlemesinde (bkz. IResponsibilityResolver) malik tarafı bu kayıtlardan
/// belirlenir; çok-malikli durumda borç tek kalır (en yüksek paylı = birincil),
/// <see cref="IsJointAndSeveral"/> ise tahsilat herhangi bir hissedardan alınabilir.
/// </para>
/// </summary>
public sealed class UnitOwnership : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Malik olunan Bağımsız Bölüm.</summary>
    public Guid UnitId { get; set; }

    /// <summary>Malik (cari kişi).</summary>
    public Guid PartyId { get; set; }

    /// <summary>Mülkiyet başlangıcı (dahil).</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Mülkiyet bitişi (dahil). Hâlâ malikse null.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Pay oranı (0–100). Tek malikte 100.</summary>
    public decimal SharePercent { get; set; } = 100m;

    /// <summary>KMK müteselsil sorumluluk bayrağı (BB başına tutarlı olmalı).</summary>
    public bool IsJointAndSeveral { get; set; }

    /// <summary>Açıklama (örn. tapu sicil no, edinme şekli). Opsiyonel.</summary>
    public string? Notes { get; set; }
}
