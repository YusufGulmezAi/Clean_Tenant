using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.BankAccounts;

/// <summary>
/// <see cref="DeactivateBankAccountCommand"/> handler.
/// </summary>
public sealed class DeactivateBankAccountCommandHandler
    : IRequestHandler<DeactivateBankAccountCommand, Result<bool>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeactivateBankAccountCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        DeactivateBankAccountCommand command,
        CancellationToken cancellationToken)
    {
        var bankAccount = await _db.AccountingBankAccounts
            .FirstOrDefaultAsync(ba => ba.Id == command.BankAccountId
                                    && ba.CompanyId == command.CompanyId
                                    && !ba.IsDeleted, cancellationToken);

        if (bankAccount is null)
            return Result<bool>.Failure(
                Error.NotFound("ACC-403", "Banka hesabı bulunamadı."));

        if (!bankAccount.IsActive)
            return Result<bool>.Success(true); // Zaten pasif, idempotent

        bankAccount.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
