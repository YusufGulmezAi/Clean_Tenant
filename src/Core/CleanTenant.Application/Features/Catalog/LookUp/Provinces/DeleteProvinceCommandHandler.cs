using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Provinces;

internal sealed class DeleteProvinceCommandHandler : IRequestHandler<DeleteProvinceCommand, Result>
{
    private readonly ICatalogDbContext _db;

    public DeleteProvinceCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeleteProvinceCommand command, CancellationToken ct)
    {
        var province = await _db.Provinces.FindAsync(new object[] { command.Id }, cancellationToken: ct);

        if (province is null || province.IsDeleted)
            return Result.Failure(Error.NotFound("LOOKUP-NOT-FOUND", "İl bulunamadı."));

        province.IsDeleted = true;
        province.DeletedAt = DateTimeOffset.UtcNow;

        _db.Provinces.Update(province);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
