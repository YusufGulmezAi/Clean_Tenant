using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Neighborhoods;

internal sealed class UpdateNeighborhoodCommandHandler : IRequestHandler<UpdateNeighborhoodCommand, Result>
{
    private readonly ICatalogDbContext _db;

    public UpdateNeighborhoodCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateNeighborhoodCommand command, CancellationToken ct)
    {
        var neighborhood = await _db.Neighborhoods.FindAsync(new object[] { command.Id }, cancellationToken: ct);

        if (neighborhood is null || neighborhood.IsDeleted)
            return Result.Failure(Error.NotFound("LOOKUP-NOT-FOUND", "Mahalle bulunamadı."));

        var districtExists = await _db.Districts
            .AsNoTracking()
            .AnyAsync(x => x.Id == command.DistrictId && !x.IsDeleted, cancellationToken: ct);

        if (!districtExists)
            return Result.Failure(Error.NotFound("LOOKUP-DISTRICT-NOT-FOUND", "İlçe bulunamadı."));

        var existingByName = await _db.Neighborhoods
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DistrictId == command.DistrictId && x.Name == command.Name && x.Id != command.Id && !x.IsDeleted, cancellationToken: ct);

        if (existingByName is not null)
            return Result.Failure(Error.Conflict("LOOKUP-DUPLICATE", $"'{command.Name}' adlı mahalle bu ilçede zaten mevcut."));

        neighborhood.Name = command.Name;
        neighborhood.DistrictId = command.DistrictId;
        neighborhood.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Neighborhoods.Update(neighborhood);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
