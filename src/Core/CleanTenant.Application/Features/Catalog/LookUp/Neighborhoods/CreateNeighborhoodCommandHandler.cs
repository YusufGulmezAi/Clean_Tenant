using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.LookUp.Neighborhoods;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Neighborhoods;

internal sealed class CreateNeighborhoodCommandHandler : IRequestHandler<CreateNeighborhoodCommand, Result<Guid>>
{
    private readonly ICatalogDbContext _db;

    public CreateNeighborhoodCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateNeighborhoodCommand command, CancellationToken ct)
    {
        var districtExists = await _db.Districts
            .AsNoTracking()
            .AnyAsync(x => x.Id == command.DistrictId && !x.IsDeleted, cancellationToken: ct);

        if (!districtExists)
            return Result<Guid>.Failure(Error.NotFound("LOOKUP-DISTRICT-NOT-FOUND", "İlçe bulunamadı."));

        var existingByName = await _db.Neighborhoods
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DistrictId == command.DistrictId && x.Name == command.Name && !x.IsDeleted, cancellationToken: ct);

        if (existingByName is not null)
            return Result<Guid>.Failure(Error.Conflict("LOOKUP-DUPLICATE", $"'{command.Name}' adlı mahalle bu ilçede zaten mevcut."));

        var neighborhood = new Neighborhood
        {
            Name = command.Name,
            DistrictId = command.DistrictId,
        };

        _db.Neighborhoods.Add(neighborhood);
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(neighborhood.Id);
    }
}
