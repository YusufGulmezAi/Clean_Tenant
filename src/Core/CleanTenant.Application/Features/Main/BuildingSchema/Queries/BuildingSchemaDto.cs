using CleanTenant.Domain.Tenant.BuildingSchema;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Queries;

/// <summary>Tek bir Unit'in düz (flat) gösterimi; tablo ve raporlama için.</summary>
public sealed record UnitDto(
    Guid Id,
    string UrlCode,
    string Number,
    string? NationalAddressCode,
    UnitType Type,
    decimal SquareMeters,
    decimal GrossSquareMeters,
    int LandShare,
    decimal? AllocatedArea,
    int Floor,
    Orientation Orientation,
    ApartmentLayout Layout,
    int RoomCount,
    Guid? BlockId,
    int SortOrder);

/// <summary>Bir Block/kule ve altındaki Unit listesi.</summary>
public sealed record BlockDto(
    Guid Id,
    string UrlCode,
    string Name,
    int SortOrder,
    IReadOnlyList<UnitDto> Units);

/// <summary>Bir Building ve altındaki Unit + Block listesi.</summary>
public sealed record BuildingDto(
    Guid Id,
    string UrlCode,
    string Name,
    string? MunicipalNo,
    BuildingType Type,
    int SortOrder,
    IReadOnlyList<BlockDto> Blocks,
    IReadOnlyList<UnitDto> Units);

/// <summary>Bir Parcel ve altındaki Building listesi.</summary>
public sealed record ParcelDto(
    Guid Id,
    string UrlCode,
    string Name,
    int SortOrder,
    IReadOnlyList<BuildingDto> Buildings);

/// <summary>Bir Land (Ada) ve altındaki Parcel listesi.</summary>
public sealed record LandDto(
    Guid Id,
    string UrlCode,
    string Name,
    int SortOrder,
    IReadOnlyList<ParcelDto> Parcels);

/// <summary>Bir Site'nin tüm yapı şeması; Ada → Parsel → Yapı → (Blok →) BB hiyerarşisi.</summary>
public sealed record BuildingSchemaDto(
    Guid CompanyId,
    IReadOnlyList<LandDto> Lands);
