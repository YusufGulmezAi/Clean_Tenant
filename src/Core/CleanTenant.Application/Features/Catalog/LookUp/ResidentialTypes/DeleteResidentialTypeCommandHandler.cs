using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.ResidentialTypes;

internal sealed class DeleteResidentialTypeCommandHandler : IRequestHandler<DeleteResidentialTypeCommand, Result>
{
    private readonly ICatalogDbContext _db;

    public DeleteResidentialTypeCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeleteResidentialTypeCommand command, CancellationToken ct)
    {
        var residentialType = await _db.ResidentialTypes.FindAsync(new object[] { command.Id }, cancellationToken: ct);

        if (residentialType is null || residentialType.IsDeleted)
            return Result.Failure(Error.NotFound("LOOKUP-NOT-FOUND", "Mesken tipi bulunamadı."));

        residentialType.IsDeleted = true;
        residentialType.DeletedAt = DateTimeOffset.UtcNow;

        _db.ResidentialTypes.Update(residentialType);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
