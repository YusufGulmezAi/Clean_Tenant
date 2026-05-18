using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Provinces;

internal sealed class UpdateProvinceCommandHandler : IRequestHandler<UpdateProvinceCommand, Result>
{
    private readonly ICatalogDbContext _db;

    public UpdateProvinceCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateProvinceCommand command, CancellationToken ct)
    {
        var province = await _db.Provinces.FindAsync(new object[] { command.Id }, cancellationToken: ct);

        if (province is null || province.IsDeleted)
            return Result.Failure(Error.NotFound("LOOKUP-NOT-FOUND", "İl bulunamadı."));

        var existingByName = await _db.Provinces
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == command.Name && x.Id != command.Id && !x.IsDeleted, cancellationToken: ct);

        if (existingByName is not null)
            return Result.Failure(Error.Conflict("LOOKUP-DUPLICATE", $"'{command.Name}' adlı il zaten mevcut."));

        province.Name = command.Name;
        province.PlateCode = command.PlateCode;
        province.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Provinces.Update(province);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
