using CleanTenant.Domain.Tenant.BuildingSchema;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Excel;

/// <summary>
/// Import Excel'inden parse edilmiş tek bir BB satırı; hiyerarşi bilgisini
/// (Land → Parcel → Building) ve BB alanlarını birlikte taşır.
/// </summary>
public sealed class BuildingSchemaImportRow
{
    /// <summary>Ada adı.</summary>
    public string LandName { get; init; } = "";

    /// <summary>Parsel adı.</summary>
    public string ParcelName { get; init; } = "";

    /// <summary>Yapı adı.</summary>
    public string BuildingName { get; init; } = "";

    /// <summary>Yapı tipi.</summary>
    public BuildingType BuildingType { get; init; }

    /// <summary>Yapının belediye/kapı no'su (opsiyonel).</summary>
    public string? MunicipalNo { get; init; }

    /// <summary>Blok/Kule adı (opsiyonel). Boşsa BB doğrudan Yapı altına bağlanır.</summary>
    public string? BlockName { get; init; }

    /// <summary>Bağımsız bölüm numarası.</summary>
    public string UnitNumber { get; init; } = "";

    /// <summary>BB tipi.</summary>
    public UnitType UnitType { get; init; }

    /// <summary>Brüt metrekare.</summary>
    public decimal SquareMeters { get; init; }

    /// <summary>Arsa payı.</summary>
    public int LandShare { get; init; }

    /// <summary>Tahsis alanı (opsiyonel).</summary>
    public decimal? AllocatedArea { get; init; }

    /// <summary>Kat.</summary>
    public int Floor { get; init; }

    /// <summary>Cephe yönü.</summary>
    public Orientation Orientation { get; init; }

    /// <summary>Oda/salon düzeni.</summary>
    public ApartmentLayout Layout { get; init; }
}
