namespace CleanTenant.Domain.Tenant.Accounting.Enums;

/// <summary>
/// Gider/maliyet dağıtım yöntemi; ortak giderlerin bağımsız bölümlere
/// veya maliyet merkezlerine nasıl yayılacağını belirler.
/// </summary>
public enum AllocationMethod
{
    /// <summary>Kullanım alanına (m²) göre orantılı dağıtım.</summary>
    SquareMeters,

    /// <summary>Tapudaki arsa payına göre orantılı dağıtım.</summary>
    LandShare,

    /// <summary>Eşit pay — her birime aynı tutar.</summary>
    EqualShare,

    /// <summary>Manuel — kullanıcı tarafından belirlenen özel oranlar.</summary>
    Manual
}
