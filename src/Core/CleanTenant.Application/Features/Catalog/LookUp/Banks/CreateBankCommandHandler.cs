using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.LookUp.Banks;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Banks;

internal sealed class CreateBankCommandHandler : IRequestHandler<CreateBankCommand, Result<Guid>>
{
    private readonly ICatalogDbContext _db;

    public CreateBankCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateBankCommand command, CancellationToken ct)
    {
        var existingByFullName = await _db.Banks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FullName == command.FullName && !x.IsDeleted, cancellationToken: ct);

        if (existingByFullName is not null)
            return Result<Guid>.Failure(Error.Conflict("LOOKUP-DUPLICATE", $"'{command.FullName}' adlı banka zaten mevcut."));

        var bank = new Bank
        {
            FullName = command.FullName,
            ShortName = command.ShortName,
            EftCode = command.EftCode,
            HasVirtualPosIntegration = command.HasVirtualPosIntegration,
            HasCorporateCollectionIntegration = command.HasCorporateCollectionIntegration,
            IsActive = command.IsActive,
        };

        _db.Banks.Add(bank);
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(bank.Id);
    }
}
