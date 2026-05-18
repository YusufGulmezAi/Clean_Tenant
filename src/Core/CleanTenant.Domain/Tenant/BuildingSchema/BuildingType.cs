namespace CleanTenant.Domain.Tenant.BuildingSchema;

/// <summary>
/// Yapının kullanım amacına göre imar/tapu sınıflandırması.
/// </summary>
public enum BuildingType
{
    /// <summary>Yalnızca konut bağımsız bölümleri barındıran yapı.</summary>
    Residential = 1,

    /// <summary>Konut ve işyeri bağımsız bölümlerini birlikte barındıran yapı.</summary>
    ResidentialCommercial = 2,

    /// <summary>Alışveriş merkezi — ticari birim ağırlıklı.</summary>
    ShoppingMall = 3,

    /// <summary>Tamamı ofis/büro bağımsız bölümlerinden oluşan yapı.</summary>
    Office = 4,

    /// <summary>Depolama amaçlı yapı.</summary>
    Warehouse = 5,

    /// <summary>Yukarıdaki kategorilere girmeyen yapı türü.</summary>
    Other = 99,
}
