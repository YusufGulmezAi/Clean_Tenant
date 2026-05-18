using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Banks;

internal sealed class UpdateBankCommandHandler : IRequestHandler<UpdateBankCommand, Result>
{
    private readonly ICatalogDbContext _db;

    public UpdateBankCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateBankCommand command, CancellationToken ct)
    {
        var bank = await _db.Banks.FindAsync(new object[] { command.Id }, cancellationToken: ct);

        if (bank is null || bank.IsDeleted)
            return Result.Failure(Error.NotFound("LOOKUP-NOT-FOUND", "Banka bulunamadı."));

        var existingByFullName = await _db.Banks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FullName == command.FullName && x.Id != command.Id && !x.IsDeleted, cancellationToken: ct);

        if (existingByFullName is not null)
            return Result.Failure(Error.Conflict("LOOKUP-DUPLICATE", $"'{command.FullName}' adlı banka zaten mevcut."));

        bank.FullName = command.FullName;
        bank.ShortName = command.ShortName;
        bank.EftCode = command.EftCode;
        bank.HasVirtualPosIntegration = command.HasVirtualPosIntegration;
        bank.HasCorporateCollectionIntegration = command.HasCorporateCollectionIntegration;
        bank.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Banks.Update(bank);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
