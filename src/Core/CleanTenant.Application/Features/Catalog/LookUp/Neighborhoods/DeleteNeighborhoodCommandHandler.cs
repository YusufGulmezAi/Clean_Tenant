using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Neighborhoods;

internal sealed class DeleteNeighborhoodCommandHandler : IRequestHandler<DeleteNeighborhoodCommand, Result>
{
    private readonly ICatalogDbContext _db;

    public DeleteNeighborhoodCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeleteNeighborhoodCommand command, CancellationToken ct)
    {
        var neighborhood = await _db.Neighborhoods.FindAsync(new object[] { command.Id }, cancellationToken: ct);

        if (neighborhood is null || neighborhood.IsDeleted)
            return Result.Failure(Error.NotFound("LOOKUP-NOT-FOUND", "Mahalle bulunamadı."));

        neighborhood.IsDeleted = true;
        neighborhood.DeletedAt = DateTimeOffset.UtcNow;

        _db.Neighborhoods.Update(neighborhood);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
