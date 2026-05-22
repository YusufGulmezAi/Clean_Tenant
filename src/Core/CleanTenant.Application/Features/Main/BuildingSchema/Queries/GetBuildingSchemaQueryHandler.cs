using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Queries;

/// <summary>
/// <see cref="GetBuildingSchemaQuery"/> handler. Land → Parcel → Building → (Block →) Unit hiyerarşisini tek sorguda yükler.
/// </summary>
public sealed class GetBuildingSchemaQueryHandler
    : IRequestHandler<GetBuildingSchemaQuery, Result<BuildingSchemaDto>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetBuildingSchemaQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<BuildingSchemaDto>> Handle(
        GetBuildingSchemaQuery query,
        CancellationToken cancellationToken)
    {
        var companyExists = await _db.Companies
            .AnyAsync(c => c.Id == query.CompanyId, cancellationToken);
        if (!companyExists)
            return Result<BuildingSchemaDto>.Failure(
                Error.NotFound("COMPANY-NOT-FOUND", "Site bulunamadı."));

        // Tüm hiyerarşiyi tek sorguda yükle
        var lands = await _db.Lands
            .Where(l => l.CompanyId == query.CompanyId)
            .OrderBy(l => l.SortOrder)
            .Include(l => l.Parcels.Where(p => !p.IsDeleted).OrderBy(p => p.SortOrder))
                .ThenInclude(p => p.Buildings.Where(bl => !bl.IsDeleted).OrderBy(bl => bl.SortOrder))
                    .ThenInclude(bl => bl.Blocks.Where(bk => !bk.IsDeleted).OrderBy(bk => bk.SortOrder))
                        .ThenInclude(bk => bk.Units.Where(u => !u.IsDeleted).OrderBy(u => u.SortOrder))
            .Include(l => l.Parcels.Where(p => !p.IsDeleted))
                .ThenInclude(p => p.Buildings.Where(bl => !bl.IsDeleted))
                    .ThenInclude(bl => bl.Units.Where(u => !u.IsDeleted && u.BlockId == null).OrderBy(u => u.SortOrder))
            .ToListAsync(cancellationToken);

        var schema = new BuildingSchemaDto(
            CompanyId: query.CompanyId,
            Lands: lands.Select(l => new LandDto(
                Id: l.Id,
                UrlCode: l.UrlCode,
                Name: l.Name,
                SortOrder: l.SortOrder,
                Parcels: l.Parcels.Select(p => new ParcelDto(
                    Id: p.Id,
                    UrlCode: p.UrlCode,
                    Name: p.Name,
                    SortOrder: p.SortOrder,
                    Buildings: p.Buildings.Select(bl => new BuildingDto(
                        Id: bl.Id,
                        UrlCode: bl.UrlCode,
                        Name: bl.Name,
                        MunicipalNo: bl.MunicipalNo,
                        Type: bl.Type,
                        SortOrder: bl.SortOrder,
                        Blocks: bl.Blocks.Select(bk => new BlockDto(
                            Id: bk.Id,
                            UrlCode: bk.UrlCode,
                            Name: bk.Name,
                            SortOrder: bk.SortOrder,
                            Units: bk.Units.Select(u => new UnitDto(
                                Id: u.Id,
                                UrlCode: u.UrlCode,
                                Number: u.Number,
                                NationalAddressCode: u.NationalAddressCode,
                                Type: u.Type,
                                SquareMeters: u.SquareMeters,
                                GrossSquareMeters: u.GrossSquareMeters,
                                LandShare: u.LandShare,
                                AllocatedArea: u.AllocatedArea,
                                Floor: u.Floor,
                                Orientation: u.Orientation,
                                Layout: u.Layout,
                                RoomCount: u.RoomCount,
                                BlockId: u.BlockId,
                                SortOrder: u.SortOrder
                            )).ToList().AsReadOnly()
                        )).ToList().AsReadOnly(),
                        Units: bl.Units.Where(u => u.BlockId == null).Select(u => new UnitDto(
                            Id: u.Id,
                            UrlCode: u.UrlCode,
                            Number: u.Number,
                            NationalAddressCode: u.NationalAddressCode,
                            Type: u.Type,
                            SquareMeters: u.SquareMeters,
                            GrossSquareMeters: u.GrossSquareMeters,
                            LandShare: u.LandShare,
                            AllocatedArea: u.AllocatedArea,
                            Floor: u.Floor,
                            Orientation: u.Orientation,
                            Layout: u.Layout,
                            RoomCount: u.RoomCount,
                            BlockId: u.BlockId,
                            SortOrder: u.SortOrder
                        )).ToList().AsReadOnly()
                    )).ToList().AsReadOnly()
                )).ToList().AsReadOnly()
            )).ToList().AsReadOnly()
        );

        return Result<BuildingSchemaDto>.Success(schema);
    }
}
