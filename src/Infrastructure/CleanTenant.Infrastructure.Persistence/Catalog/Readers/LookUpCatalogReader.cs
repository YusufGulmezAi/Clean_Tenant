using CleanTenant.Application.Features.Catalog.Readers;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Readers;

/// <summary>
/// <para>
/// <see cref="ILookUpCatalogReader"/> implementasyonu. v0.2.11.b'de
/// <c>IDbContextFactory&lt;CatalogDbContext&gt;</c>'ye geçildi — Blazor Server'da
/// prerender + interactive double-render senaryosunda scoped DbContext concurrency
/// hatası veriyordu (TenantCatalogReader ile aynı sebep, v0.2.9 fix).
/// </para>
/// </summary>
internal sealed class LookUpCatalogReader : ILookUpCatalogReader
{
    private readonly IDbContextFactory<CatalogDbContext> _dbFactory;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public LookUpCatalogReader(IDbContextFactory<CatalogDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProvinceListItem>> GetProvincesAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        // Plaka koduna göre sırala (Türkiye konvansiyonu: 01 Adana → 81 Düzce).
        // Postgres default collation Türkçe-aware değil; Name OrderBy'ı Ç/Ğ/İ/Ö/Ş/Ü
        // gibi karakterleri beklenmedik yere atar. PlateCode null ise sona düşer.
        var result = await db.Provinces
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.PlateCode == null)
            .ThenBy(x => x.PlateCode)
            .ThenBy(x => x.Name)
            .Select(x => new ProvinceListItem(x.Id, x.UrlCode, x.Name, x.PlateCode))
            .ToListAsync(ct);
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DistrictListItem>> GetDistrictsByProvinceAsync(Guid provinceId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var result = await db.Districts
            .AsNoTracking()
            .Where(x => x.ProvinceId == provinceId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new DistrictListItem(x.Id, x.UrlCode, x.Name, x.ProvinceId))
            .ToListAsync(ct);
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NeighborhoodListItem>> GetNeighborhoodsByDistrictAsync(Guid districtId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var result = await db.Neighborhoods
            .AsNoTracking()
            .Where(x => x.DistrictId == districtId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new NeighborhoodListItem(x.Id, x.UrlCode, x.Name, x.DistrictId))
            .ToListAsync(ct);
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ResidentialTypeListItem>> GetResidentialTypesAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var result = await db.ResidentialTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new ResidentialTypeListItem(x.Id, x.UrlCode, x.Name, x.Description))
            .ToListAsync(ct);
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BuildingTypeListItem>> GetBuildingTypesAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var result = await db.BuildingTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new BuildingTypeListItem(x.Id, x.UrlCode, x.Name, x.Description))
            .ToListAsync(ct);
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BankListItem>> GetBanksAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var result = await db.Banks
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.FullName)
            .Select(x => new BankListItem(
                x.Id,
                x.UrlCode,
                x.FullName,
                x.ShortName,
                x.EftCode,
                x.HasVirtualPosIntegration,
                x.HasCorporateCollectionIntegration,
                x.IsActive))
            .ToListAsync(ct);
        return result;
    }

    /// <inheritdoc />
    public async Task<BankDetail?> GetBankDetailAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var result = await db.Banks
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new BankDetail(
                x.Id,
                x.UrlCode,
                x.FullName,
                x.ShortName,
                x.EftCode,
                x.HasVirtualPosIntegration,
                x.HasCorporateCollectionIntegration,
                x.IsActive))
            .FirstOrDefaultAsync(ct);
        return result;
    }
}
