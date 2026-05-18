using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.ResidentialTypes;

internal sealed class UpdateResidentialTypeCommandHandler : IRequestHandler<UpdateResidentialTypeCommand, Result>
{
    private readonly ICatalogDbContext _db;

    public UpdateResidentialTypeCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateResidentialTypeCommand command, CancellationToken ct)
    {
        var residentialType = await _db.ResidentialTypes.FindAsync(new object[] { command.Id }, cancellationToken: ct);

        if (residentialType is null || residentialType.IsDeleted)
            return Result.Failure(Error.NotFound("LOOKUP-NOT-FOUND", "Mesken tipi bulunamadı."));

        var existingByName = await _db.ResidentialTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == command.Name && x.Id != command.Id && !x.IsDeleted, cancellationToken: ct);

        if (existingByName is not null)
            return Result.Failure(Error.Conflict("LOOKUP-DUPLICATE", $"'{command.Name}' adlı mesken tipi zaten mevcut."));

        residentialType.Name = command.Name;
        residentialType.Description = command.Description;
        residentialType.UpdatedAt = DateTimeOffset.UtcNow;

        _db.ResidentialTypes.Update(residentialType);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
