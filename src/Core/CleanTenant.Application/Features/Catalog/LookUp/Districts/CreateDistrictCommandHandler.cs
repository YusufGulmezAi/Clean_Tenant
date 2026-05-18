using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.LookUp.Districts;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Districts;

internal sealed class CreateDistrictCommandHandler : IRequestHandler<CreateDistrictCommand, Result<Guid>>
{
    private readonly ICatalogDbContext _db;

    public CreateDistrictCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateDistrictCommand command, CancellationToken ct)
    {
        var provinceExists = await _db.Provinces
            .AsNoTracking()
            .AnyAsync(x => x.Id == command.ProvinceId && !x.IsDeleted, cancellationToken: ct);

        if (!provinceExists)
            return Result<Guid>.Failure(Error.NotFound("LOOKUP-PROVINCE-NOT-FOUND", "Il bulunamadı."));

        var existingByName = await _db.Districts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProvinceId == command.ProvinceId && x.Name == command.Name && !x.IsDeleted, cancellationToken: ct);

        if (existingByName is not null)
            return Result<Guid>.Failure(Error.Conflict("LOOKUP-DUPLICATE", $"'{command.Name}' adlı ilçe bu ilde zaten mevcut."));

        var district = new District
        {
            Name = command.Name,
            ProvinceId = command.ProvinceId,
        };

        _db.Districts.Add(district);
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(district.Id);
    }
}
