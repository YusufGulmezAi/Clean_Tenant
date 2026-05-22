using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Budgeting;

/// <summary>
/// <para>
/// <see cref="ParticipationGroup"/> ↔ Bağımsız Bölüm junction kaydı.
/// Many-to-many ilişki; (ParticipationGroupId, UnitId) çifti benzersizdir
/// (kısmi unique index, IsDeleted = false).
/// </para>
/// <para>
/// Üyeliğin tarih penceresi vardır: <see cref="ValidFrom"/> dahil,
/// <see cref="ValidTo"/> dahil (null ise açık uçlu). Tahakkuk üretiminde
/// dönem tarihine düşen aktif üyelikler kullanılır.
/// </para>
/// </summary>
public sealed class UnitParticipationGroup : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Katılım grubunun id'si.</summary>
    public Guid ParticipationGroupId { get; set; }

    /// <summary>Üye Bağımsız Bölüm id'si.</summary>
    public Guid UnitId { get; set; }

    /// <summary>Üyelik başlangıcı (dahil). Tahakkuk dönemi bu tarihten itibaren etkilenir.</summary>
    public DateOnly ValidFrom { get; set; }

    /// <summary>Üyelik bitişi (dahil). Açık uçluysa null.</summary>
    public DateOnly? ValidTo { get; set; }

    /// <summary>Üyelik açıklaması / sebebi. Opsiyonel.</summary>
    public string? Notes { get; set; }
}
