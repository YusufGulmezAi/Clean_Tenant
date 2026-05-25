using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainUnit = CleanTenant.Domain.Tenant.BuildingSchema.Unit;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Excel;

/// <summary>
/// <see cref="ImportBuildingSchemaCommand"/> handler.
/// Parse + validasyon servisini çağırır, hata yoksa hiyerarşiyi upsert eder.
/// </summary>
public sealed class ImportBuildingSchemaCommandHandler
    : IRequestHandler<ImportBuildingSchemaCommand, Result<ImportBuildingSchemaResult>>
{
    private readonly IMainDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IBuildingSchemaExcelService _excelService;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ImportBuildingSchemaCommandHandler(
        IMainDbContext db,
        ITenantContext tenantContext,
        IBuildingSchemaExcelService excelService)
    {
        _db = db;
        _tenantContext = tenantContext;
        _excelService = excelService;
    }

    /// <inheritdoc />
    public async Task<Result<ImportBuildingSchemaResult>> Handle(
        ImportBuildingSchemaCommand command,
        CancellationToken cancellationToken)
    {
        var companyExists = await _db.Companies
            .AnyAsync(c => c.Id == command.CompanyId, cancellationToken);
        if (!companyExists)
            return Result<ImportBuildingSchemaResult>.Failure(
                Error.NotFound("COMPANY-NOT-FOUND", "Site bulunamadı."));

        var parseResult = _excelService.ParseAndValidate(command.ExcelStream);
        if (parseResult.HasErrors)
            return Result<ImportBuildingSchemaResult>.Success(
                new ImportBuildingSchemaResult(true, parseResult.ErrorWorkbook, 0));

        var tenantId = _tenantContext.TenantId!.Value;

        // Mevcut hiyerarşiyi tek sorguda yükle. Global query filter YALNIZ TenantId
        // uygular (soft-delete filtresi yok) — bu bilinçli: soft-delete edilmiş bir
        // Ada/Parsel/Bina/BB ile aynı ada sahip satır import edilince onu yeniden
        // kullanıp DİRİLTİRİZ (IsDeleted=false). Aksi halde silinmiş parent altına
        // canlı çocuklar eklenir ve okuma sorgusu (!IsDeleted) tüm ağacı gizlerdi.
        var existingLands = await _db.Lands
            .Where(l => l.CompanyId == command.CompanyId)
            .Include(l => l.Parcels)
                .ThenInclude(p => p.Buildings)
                    .ThenInclude(bl => bl.Units)
            .Include(l => l.Parcels)
                .ThenInclude(p => p.Buildings)
                    .ThenInclude(bl => bl.Blocks)
            .ToListAsync(cancellationToken);

        var landByName = existingLands.ToDictionary(
            l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

        // SortOrder her import'ta Excel'deki karşılaşma sırasına göre (yeni + mevcut
        // TÜM kayıtlara) yeniden atanır → liste/rapor sırası daima Excel ile birebir.
        int unitCount = 0;
        int landSort = 0;

        foreach (var landGroup in parseResult.Rows.GroupBy(r => r.LandName, StringComparer.OrdinalIgnoreCase))
        {
            landSort++;
            if (!landByName.TryGetValue(landGroup.Key, out var land))
            {
                land = new Land
                {
                    TenantId = tenantId,
                    CompanyId = command.CompanyId,
                    Name = landGroup.Key,
                    SortOrder = landSort,
                };
                _db.Lands.Add(land);
                landByName[landGroup.Key] = land;
            }
            else { Resurrect(land); land.SortOrder = landSort; }

            var parcelByName = land.Parcels
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
            int parcelSort = 0;

            foreach (var parcelGroup in landGroup.GroupBy(r => r.ParcelName, StringComparer.OrdinalIgnoreCase))
            {
                parcelSort++;
                if (!parcelByName.TryGetValue(parcelGroup.Key, out var parcel))
                {
                    parcel = new Parcel
                    {
                        TenantId = tenantId,
                        LandId = land.Id,
                        Name = parcelGroup.Key,
                        SortOrder = parcelSort,
                    };
                    _db.Parcels.Add(parcel);
                    parcelByName[parcelGroup.Key] = parcel;
                }
                else { Resurrect(parcel); parcel.SortOrder = parcelSort; }

                var buildingByName = parcel.Buildings
                    .ToDictionary(b => b.Name, b => b, StringComparer.OrdinalIgnoreCase);
                int buildingSort = 0;

                foreach (var buildingGroup in parcelGroup.GroupBy(r => r.BuildingName, StringComparer.OrdinalIgnoreCase))
                {
                    var firstRow = buildingGroup.First();
                    buildingSort++;

                    if (!buildingByName.TryGetValue(buildingGroup.Key, out var building))
                    {
                        building = new Building
                        {
                            TenantId = tenantId,
                            ParcelId = parcel.Id,
                            Name = buildingGroup.Key,
                            Type = firstRow.BuildingType,
                            MunicipalNo = firstRow.MunicipalNo,
                            SortOrder = buildingSort,
                        };
                        _db.Buildings.Add(building);
                        buildingByName[buildingGroup.Key] = building;
                    }
                    else
                    {
                        Resurrect(building);
                        building.Type = firstRow.BuildingType;
                        building.MunicipalNo = firstRow.MunicipalNo;
                        building.SortOrder = buildingSort;
                    }

                    // Bloklar (opsiyonel) — adıyla bul/oluştur.
                    var blockByName = building.Blocks
                        .ToDictionary(b => b.Name, b => b, StringComparer.OrdinalIgnoreCase);

                    // BB anahtarı blok-kapsamlı: aynı No farklı bloklarda bulunabilir.
                    var unitByKey = building.Units
                        .ToDictionary(u => $"{u.BlockId}|{u.Number.ToUpperInvariant()}", u => u);

                    // SortOrder Excel satır sırasına göre (yapı içinde) yeniden atanır.
                    int blockSort = 0;
                    int unitSort = 0;
                    var blockSequenced = new HashSet<Guid>();

                    foreach (var row in buildingGroup)
                    {
                        // Blok çözümle (opsiyonel): adı varsa bul/oluştur, yoksa BB Yapı altına.
                        Guid? blockId = null;
                        if (!string.IsNullOrWhiteSpace(row.BlockName))
                        {
                            if (!blockByName.TryGetValue(row.BlockName, out var block))
                            {
                                block = new Block
                                {
                                    TenantId = tenantId,
                                    BuildingId = building.Id,
                                    Name = row.BlockName.Trim(),
                                };
                                _db.Blocks.Add(block);
                                blockByName[row.BlockName] = block;
                            }
                            else Resurrect(block);
                            // Blok SortOrder'ı bu import'taki ilk karşılaşmada (Excel sırası) atanır.
                            if (blockSequenced.Add(block.Id))
                                block.SortOrder = ++blockSort;
                            blockId = block.Id;
                        }

                        var unitKey = $"{blockId}|{row.UnitNumber.ToUpperInvariant()}";
                        if (!unitByKey.TryGetValue(unitKey, out var unit))
                        {
                            unit = new DomainUnit
                            {
                                TenantId = tenantId,
                                BuildingId = building.Id,
                                BlockId = blockId,
                                Number = row.UnitNumber,
                            };
                            _db.Units.Add(unit);
                            unitByKey[unitKey] = unit;
                        }
                        else Resurrect(unit);

                        unit.SortOrder = ++unitSort;
                        unit.Type = row.UnitType;
                        unit.SquareMeters = row.SquareMeters;
                        unit.LandShare = row.LandShare;
                        unit.AllocatedArea = row.AllocatedArea;
                        unit.Floor = row.Floor;
                        unit.Orientation = row.Orientation;
                        unit.Layout = row.Layout;

                        unitCount++;
                    }
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<ImportBuildingSchemaResult>.Success(
            new ImportBuildingSchemaResult(false, null, unitCount));
    }

    // Soft-delete edilmiş bir entity import sırasında aynı adla tekrar geldiğinde
    // onu canlıya çevirir (yeni satır oluşturmak yerine). Böylece önce silinip
    // sonra yeniden import edilen Ada/Parsel/Bina/Blok/BB ekranda görünür olur.
    private static void Resurrect(ISoftDeletable entity)
    {
        if (!entity.IsDeleted) return;
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
    }
}
