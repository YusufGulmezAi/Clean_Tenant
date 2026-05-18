namespace CleanTenant.Domain.Tenant.BuildingSchema;

/// <summary>
/// Bağımsız bölümün oda ve salon kombinasyonu (Türkiye'deki oda+salon gösterimi).
/// </summary>
public enum ApartmentLayout
{
    /// <summary>Stüdyo daire — ayrı yatak odası yok.</summary>
    Studio = 0,

    /// <summary>1 oda, salon yok (1+0).</summary>
    OneRoom = 1,

    /// <summary>1 yatak odası + 1 salon (1+1).</summary>
    OneBedroom = 2,

    /// <summary>2 yatak odası + 1 salon (2+1).</summary>
    TwoBedroom = 3,

    /// <summary>3 yatak odası + 1 salon (3+1).</summary>
    ThreeBedroom = 4,

    /// <summary>4 yatak odası + 1 salon (4+1).</summary>
    FourBedroom = 5,

    /// <summary>5 yatak odası + 1 salon (5+1).</summary>
    FiveBedroom = 6,

    /// <summary>Belirtilen tiplere uymayan kombinasyon.</summary>
    Other = 99,
}
