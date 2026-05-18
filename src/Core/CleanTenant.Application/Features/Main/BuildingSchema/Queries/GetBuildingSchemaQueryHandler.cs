using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Queries;

/// <summary>
/// <see cref="GetBuildingSchemaQuery"/> handler. Block → Parcel → Building → Unit hiyerarşisini tek sorguda yükler.
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

        // Tüm hiyerarşiyi tek sorguda yükle (4 JOIN)
        var blocks = await _db.Blocks
            .Where(b => b.CompanyId == query.CompanyId)
            .OrderBy(b => b.SortOrder)
            .Include(b => b.Parcels.Where(p => !p.IsDeleted).OrderBy(p => p.SortOrder))
                .ThenInclude(p => p.Buildings.Where(bl => !bl.IsDeleted).OrderBy(bl => bl.SortOrder))
                    .ThenInclude(bl => bl.Units.Where(u => !u.IsDeleted).OrderBy(u => u.SortOrder))
            .ToListAsync(cancellationToken);

        var schema = new BuildingSchemaDto(
            CompanyId: query.CompanyId,
            Blocks: blocks.Select(b => new BlockDto(
                Id: b.Id,
                UrlCode: b.UrlCode,
                Name: b.Name,
                SortOrder: b.SortOrder,
                Parcels: b.Parcels.Select(p => new ParcelDto(
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
                        Units: bl.Units.Select(u => new UnitDto(
                            Id: u.Id,
                            UrlCode: u.UrlCode,
                            Number: u.Number,
                            NationalAddressCode: u.NationalAddressCode,
                            Type: u.Type,
                            SquareMeters: u.SquareMeters,
                            LandShare: u.LandShare,
                            AllocatedArea: u.AllocatedArea,
                            Floor: u.Floor,
                            Orientation: u.Orientation,
                            Layout: u.Layout,
                            SortOrder: u.SortOrder
                        )).ToList().AsReadOnly()
                    )).ToList().AsReadOnly()
                )).ToList().AsReadOnly()
            )).ToList().AsReadOnly()
        );

        return Result<BuildingSchemaDto>.Success(schema);
    }
}
