using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Districts;

internal sealed class UpdateDistrictCommandHandler : IRequestHandler<UpdateDistrictCommand, Result>
{
    private readonly ICatalogDbContext _db;

    public UpdateDistrictCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateDistrictCommand command, CancellationToken ct)
    {
        var district = await _db.Districts.FindAsync(new object[] { command.Id }, cancellationToken: ct);

        if (district is null || district.IsDeleted)
            return Result.Failure(Error.NotFound("LOOKUP-NOT-FOUND", "İlçe bulunamadı."));

        var provinceExists = await _db.Provinces
            .AsNoTracking()
            .AnyAsync(x => x.Id == command.ProvinceId && !x.IsDeleted, cancellationToken: ct);

        if (!provinceExists)
            return Result.Failure(Error.NotFound("LOOKUP-PROVINCE-NOT-FOUND", "İl bulunamadı."));

        var existingByName = await _db.Districts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProvinceId == command.ProvinceId && x.Name == command.Name && x.Id != command.Id && !x.IsDeleted, cancellationToken: ct);

        if (existingByName is not null)
            return Result.Failure(Error.Conflict("LOOKUP-DUPLICATE", $"'{command.Name}' adlı ilçe bu ilde zaten mevcut."));

        district.Name = command.Name;
        district.ProvinceId = command.ProvinceId;
        district.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Districts.Update(district);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
