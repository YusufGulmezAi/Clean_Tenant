namespace CleanTenant.Domain.Tenant.BuildingSchema;

/// <summary>
/// Bağımsız bölümün tapu kayıtlarındaki nitelik (kullanım) tipi.
/// </summary>
public enum UnitType
{
    /// <summary>Konut amaçlı daire.</summary>
    Apartment = 1,

    /// <summary>Büro / ofis bağımsız bölümü.</summary>
    Office = 2,

    /// <summary>Perakende satış için dükkan.</summary>
    Shop = 3,

    /// <summary>Büyük ölçekli perakende veya toptan satış için mağaza.</summary>
    Store = 4,

    /// <summary>Eşya/araç depolama birimi.</summary>
    Storage = 5,

    /// <summary>Araç park yeri (kapalı / açık).</summary>
    Parking = 6,

    /// <summary>Deprem / yangın sığınağı.</summary>
    Shelter = 7,

    /// <summary>Yukarıdaki kategorilere girmeyen bağımsız bölüm.</summary>
    Other = 99,
}
