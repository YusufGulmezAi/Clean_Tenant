namespace CleanTenant.Application.Features.Catalog.Readers;

public interface ILookUpCatalogReader
{
    Task<IReadOnlyList<ProvinceListItem>> GetProvincesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DistrictListItem>> GetDistrictsByProvinceAsync(Guid provinceId, CancellationToken ct = default);
    Task<IReadOnlyList<NeighborhoodListItem>> GetNeighborhoodsByDistrictAsync(Guid districtId, CancellationToken ct = default);
    Task<IReadOnlyList<ResidentialTypeListItem>> GetResidentialTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BuildingTypeListItem>> GetBuildingTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BankListItem>> GetBanksAsync(CancellationToken ct = default);
    Task<BankDetail?> GetBankDetailAsync(Guid id, CancellationToken ct = default);
}

public sealed record ProvinceListItem(Guid Id, string UrlCode, string Name, int? PlateCode);
public sealed record DistrictListItem(Guid Id, string UrlCode, string Name, Guid ProvinceId);
public sealed record NeighborhoodListItem(Guid Id, string UrlCode, string Name, Guid DistrictId);
public sealed record ResidentialTypeListItem(Guid Id, string UrlCode, string Name, string? Description);
public sealed record BuildingTypeListItem(Guid Id, string UrlCode, string Name, string? Description);
public sealed record BankListItem(
    Guid Id,
    string UrlCode,
    string FullName,
    string ShortName,
    string? EftCode,
    bool HasVirtualPosIntegration,
    bool HasCorporateCollectionIntegration,
    bool IsActive);

public sealed record BankDetail(
    Guid Id,
    string UrlCode,
    string FullName,
    string ShortName,
    string? EftCode,
    bool HasVirtualPosIntegration,
    bool HasCorporateCollectionIntegration,
    bool IsActive);
