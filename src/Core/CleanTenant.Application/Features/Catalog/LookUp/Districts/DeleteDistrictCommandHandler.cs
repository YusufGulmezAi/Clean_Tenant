using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Districts;

internal sealed class DeleteDistrictCommandHandler : IRequestHandler<DeleteDistrictCommand, Result>
{
    private readonly ICatalogDbContext _db;

    public DeleteDistrictCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeleteDistrictCommand command, CancellationToken ct)
    {
        var district = await _db.Districts.FindAsync(new object[] { command.Id }, cancellationToken: ct);

        if (district is null || district.IsDeleted)
            return Result.Failure(Error.NotFound("LOOKUP-NOT-FOUND", "İlçe bulunamadı."));

        district.IsDeleted = true;
        district.DeletedAt = DateTimeOffset.UtcNow;

        _db.Districts.Update(district);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
