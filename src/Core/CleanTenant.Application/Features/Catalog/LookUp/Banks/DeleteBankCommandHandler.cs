using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Banks;

internal sealed class DeleteBankCommandHandler : IRequestHandler<DeleteBankCommand, Result>
{
    private readonly ICatalogDbContext _db;

    public DeleteBankCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeleteBankCommand command, CancellationToken ct)
    {
        var bank = await _db.Banks.FindAsync(new object[] { command.Id }, cancellationToken: ct);

        if (bank is null || bank.IsDeleted)
            return Result.Failure(Error.NotFound("LOOKUP-NOT-FOUND", "Banka bulunamadı."));

        bank.IsDeleted = true;
        bank.DeletedAt = DateTimeOffset.UtcNow;

        _db.Banks.Update(bank);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
