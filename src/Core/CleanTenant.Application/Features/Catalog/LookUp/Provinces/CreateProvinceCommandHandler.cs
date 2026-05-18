using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.LookUp.Provinces;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Provinces;

internal sealed class CreateProvinceCommandHandler : IRequestHandler<CreateProvinceCommand, Result<Guid>>
{
    private readonly ICatalogDbContext _db;

    public CreateProvinceCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateProvinceCommand command, CancellationToken ct)
    {
        var existingByName = await _db.Provinces
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == command.Name && !x.IsDeleted, cancellationToken: ct);

        if (existingByName is not null)
            return Result<Guid>.Failure(Error.Conflict("LOOKUP-DUPLICATE", $"'{command.Name}' adlı il zaten mevcut."));

        var province = new Province
        {
            Name = command.Name,
            PlateCode = command.PlateCode,
        };

        _db.Provinces.Add(province);
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(province.Id);
    }
}
