using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.SharedKernel.Context;
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

        // Mevcut hiyerarşiyi tek sorguda yükle (global query filter: TenantId + !IsDeleted)
        var existingBlocks = await _db.Blocks
            .Where(b => b.CompanyId == command.CompanyId)
            .Include(b => b.Parcels)
                .ThenInclude(p => p.Buildings)
                    .ThenInclude(bl => bl.Units)
            .ToListAsync(cancellationToken);

        var blockByName = existingBlocks.ToDictionary(
            b => b.Name, b => b, StringComparer.OrdinalIgnoreCase);

        int blockMaxSort = existingBlocks.Count > 0 ? existingBlocks.Max(b => b.SortOrder) : 0;
        int unitCount = 0;

        foreach (var blockGroup in parseResult.Rows.GroupBy(r => r.BlockName, StringComparer.OrdinalIgnoreCase))
        {
            if (!blockByName.TryGetValue(blockGroup.Key, out var block))
            {
                block = new Block
                {
                    TenantId = tenantId,
                    CompanyId = command.CompanyId,
                    Name = blockGroup.Key,
                    SortOrder = ++blockMaxSort,
                };
                _db.Blocks.Add(block);
                blockByName[blockGroup.Key] = block;
            }

            var parcelByName = block.Parcels
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
            int parcelMaxSort = block.Parcels.Count > 0 ? block.Parcels.Max(p => p.SortOrder) : 0;

            foreach (var parcelGroup in blockGroup.GroupBy(r => r.ParcelName, StringComparer.OrdinalIgnoreCase))
            {
                if (!parcelByName.TryGetValue(parcelGroup.Key, out var parcel))
                {
                    parcel = new Parcel
                    {
                        TenantId = tenantId,
                        BlockId = block.Id,
                        Name = parcelGroup.Key,
                        SortOrder = ++parcelMaxSort,
                    };
                    _db.Parcels.Add(parcel);
                    parcelByName[parcelGroup.Key] = parcel;
                }

                var buildingByName = parcel.Buildings
                    .ToDictionary(b => b.Name, b => b, StringComparer.OrdinalIgnoreCase);
                int buildingMaxSort = parcel.Buildings.Count > 0 ? parcel.Buildings.Max(b => b.SortOrder) : 0;

                foreach (var buildingGroup in parcelGroup.GroupBy(r => r.BuildingName, StringComparer.OrdinalIgnoreCase))
                {
                    var firstRow = buildingGroup.First();

                    if (!buildingByName.TryGetValue(buildingGroup.Key, out var building))
                    {
                        building = new Building
                        {
                            TenantId = tenantId,
                            ParcelId = parcel.Id,
                            Name = buildingGroup.Key,
                            Type = firstRow.BuildingType,
                            SortOrder = ++buildingMaxSort,
                        };
                        _db.Buildings.Add(building);
                        buildingByName[buildingGroup.Key] = building;
                    }
                    else
                    {
                        building.Type = firstRow.BuildingType;
                    }

                    var unitByNumber = building.Units
                        .ToDictionary(u => u.Number, u => u, StringComparer.OrdinalIgnoreCase);
                    int unitMaxSort = building.Units.Count > 0 ? building.Units.Max(u => u.SortOrder) : 0;

                    foreach (var row in buildingGroup)
                    {
                        if (!unitByNumber.TryGetValue(row.UnitNumber, out var unit))
                        {
                            unit = new DomainUnit
                            {
                                TenantId = tenantId,
                                BuildingId = building.Id,
                                Number = row.UnitNumber,
                                SortOrder = ++unitMaxSort,
                            };
                            _db.Units.Add(unit);
                            unitByNumber[row.UnitNumber] = unit;
                        }

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
}
