using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.LookUp.ResidentialTypes;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.ResidentialTypes;

internal sealed class CreateResidentialTypeCommandHandler : IRequestHandler<CreateResidentialTypeCommand, Result<Guid>>
{
    private readonly ICatalogDbContext _db;

    public CreateResidentialTypeCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateResidentialTypeCommand command, CancellationToken ct)
    {
        var existingByName = await _db.ResidentialTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == command.Name && !x.IsDeleted, cancellationToken: ct);

        if (existingByName is not null)
            return Result<Guid>.Failure(Error.Conflict("LOOKUP-DUPLICATE", $"'{command.Name}' adlı mesken tipi zaten mevcut."));

        var residentialType = new ResidentialType
        {
            Name = command.Name,
            Description = command.Description,
        };

        _db.ResidentialTypes.Add(residentialType);
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(residentialType.Id);
    }
}
