using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Catalog.Readers;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Readers;

internal sealed class LookUpCatalogReader : ILookUpCatalogReader
{
    private readonly ICatalogDbContext _db;

    public LookUpCatalogReader(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProvinceListItem>> GetProvincesAsync(CancellationToken ct = default)
    {
        var result = await _db.Provinces
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new ProvinceListItem(x.Id, x.UrlCode, x.Name, x.PlateCode))
            .ToListAsync(ct);
        return result;
    }

    public async Task<IReadOnlyList<DistrictListItem>> GetDistrictsByProvinceAsync(Guid provinceId, CancellationToken ct = default)
    {
        var result = await _db.Districts
            .AsNoTracking()
            .Where(x => x.ProvinceId == provinceId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new DistrictListItem(x.Id, x.UrlCode, x.Name, x.ProvinceId))
            .ToListAsync(ct);
        return result;
    }

    public async Task<IReadOnlyList<NeighborhoodListItem>> GetNeighborhoodsByDistrictAsync(Guid districtId, CancellationToken ct = default)
    {
        var result = await _db.Neighborhoods
            .AsNoTracking()
            .Where(x => x.DistrictId == districtId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new NeighborhoodListItem(x.Id, x.UrlCode, x.Name, x.DistrictId))
            .ToListAsync(ct);
        return result;
    }

    public async Task<IReadOnlyList<ResidentialTypeListItem>> GetResidentialTypesAsync(CancellationToken ct = default)
    {
        var result = await _db.ResidentialTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new ResidentialTypeListItem(x.Id, x.UrlCode, x.Name, x.Description))
            .ToListAsync(ct);
        return result;
    }

    public async Task<IReadOnlyList<BuildingTypeListItem>> GetBuildingTypesAsync(CancellationToken ct = default)
    {
        var result = await _db.BuildingTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new BuildingTypeListItem(x.Id, x.UrlCode, x.Name, x.Description))
            .ToListAsync(ct);
        return result;
    }

    public async Task<IReadOnlyList<BankListItem>> GetBanksAsync(CancellationToken ct = default)
    {
        var result = await _db.Banks
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.FullName)
            .Select(x => new BankListItem(x.Id, x.UrlCode, x.FullName, x.ShortName))
            .ToListAsync(ct);
        return result;
    }

    public async Task<BankDetail?> GetBankDetailAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _db.Banks
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new BankDetail(
                x.Id,
                x.UrlCode,
                x.FullName,
                x.ShortName,
                x.EftCode,
                x.HasVirtualPosIntegration,
                x.HasCorporateCollectionIntegration))
            .FirstOrDefaultAsync(ct);
        return result;
    }
}
