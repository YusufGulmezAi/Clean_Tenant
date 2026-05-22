using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.BankAccounts;

/// <summary>
/// <see cref="UpdateBankAccountCommand"/> handler.
/// </summary>
public sealed class UpdateBankAccountCommandHandler
    : IRequestHandler<UpdateBankAccountCommand, Result<BankAccountDetail>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateBankAccountCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<BankAccountDetail>> Handle(
        UpdateBankAccountCommand command,
        CancellationToken cancellationToken)
    {
        var bankAccount = await _db.AccountingBankAccounts
            .FirstOrDefaultAsync(ba => ba.Id == command.BankAccountId
                                    && ba.CompanyId == command.CompanyId
                                    && !ba.IsDeleted, cancellationToken);

        if (bankAccount is null)
            return Result<BankAccountDetail>.Failure(
                Error.NotFound("ACC-403", "Banka hesabı bulunamadı."));

        // IBAN benzersizliği kontrolü (kendisi hariç)
        if (!string.IsNullOrEmpty(command.Iban))
        {
            var ibanExists = await _db.AccountingBankAccounts
                .AnyAsync(ba => ba.CompanyId == command.CompanyId
                             && ba.Iban == command.Iban
                             && ba.Id != command.BankAccountId
                             && !ba.IsDeleted, cancellationToken);

            if (ibanExists)
                return Result<BankAccountDetail>.Failure(
                    Error.Conflict("ACC-402", $"'{command.Iban}' IBAN numarası zaten kayıtlı."));
        }

        bankAccount.Name = command.Name;
        bankAccount.BankName = command.BankName;
        bankAccount.BranchCode = command.BranchCode;
        bankAccount.AccountNumber = command.AccountNumber;
        bankAccount.Iban = command.Iban;
        bankAccount.AccountType = command.AccountType;
        bankAccount.CurrencyCode = command.CurrencyCode;
        bankAccount.AccountCodeId = command.AccountCodeId;
        bankAccount.IsActive = command.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<BankAccountDetail>.Success(new BankAccountDetail(
            bankAccount.Id,
            bankAccount.Name,
            bankAccount.BankName,
            bankAccount.BranchCode,
            bankAccount.AccountNumber,
            bankAccount.Iban,
            bankAccount.AccountType,
            bankAccount.CurrencyCode,
            bankAccount.AccountCodeId,
            bankAccount.IsActive));
    }
}
