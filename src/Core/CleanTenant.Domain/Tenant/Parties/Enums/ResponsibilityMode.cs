namespace CleanTenant.Domain.Tenant.Parties.Enums;

/// <summary>
/// Bir bütçenin/tahakkuğun borç sorumluluğunun hangi tarafa yansıyacağını
/// belirler. Çözümleme tahakkuk dönemine göre yapılır (bkz. IResponsibilityResolver).
/// </summary>
public enum ResponsibilityMode
{
    /// <summary>
    /// Tahakkuk döneminde aktif kiracı varsa kiracıya, yoksa aktif malike yansır.
    /// (Aidat/işletme giderleri için varsayılan.)
    /// </summary>
    TenantThenOwner = 0,

    /// <summary>
    /// Her zaman tahakkuk dönemindeki aktif malik(ler)e yansır; kiracı dikkate
    /// alınmaz. (Demirbaş / büyük onarım gibi malik yükümlülükleri için.)
    /// </summary>
    OwnerOnly = 1
}
