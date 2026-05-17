namespace CleanTenant.Domain.Tenant.Companies;

/// <summary>
/// <see cref="Company"/>'in yaşam döngüsü durumu. Soft-delete ayrı
/// (<c>BaseEntity.IsDeleted</c>); status sıralı durum geçişlerini taşır.
/// </summary>
public enum CompanyStatus
{
    /// <summary>Aktif şirket — tüm operasyonlar açık.</summary>
    Active = 1,

    /// <summary>Geçici askıya alınmış — Tenant Admin kararı (örn. ödeme bekliyor).</summary>
    Suspended = 2,

    /// <summary>Kapanmış — yeni operasyon yok, salt okunur tarihçe.</summary>
    Closed = 3,
}
