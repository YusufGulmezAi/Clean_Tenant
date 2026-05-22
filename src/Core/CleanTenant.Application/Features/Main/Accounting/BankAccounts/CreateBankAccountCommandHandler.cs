using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.BankAccounts;

/// <summary>
/// <see cref="CreateBankAccountCommand"/> handler.
/// </summary>
public sealed class CreateBankAccountCommandHandler
    : IRequestHandler<CreateBankAccountCommand, Result<BankAccountDetail>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateBankAccountCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<BankAccountDetail>> Handle(
        CreateBankAccountCommand command,
        CancellationToken cancellationToken)
    {
        // IBAN benzersizliği kontrolü (aynı şirket içinde)
        if (!string.IsNullOrEmpty(command.Iban))
        {
            var ibanExists = await _db.AccountingBankAccounts
                .AnyAsync(ba => ba.CompanyId == command.CompanyId
                             && ba.Iban == command.Iban
                             && !ba.IsDeleted, cancellationToken);

            if (ibanExists)
                return Result<BankAccountDetail>.Failure(
                    Error.Conflict("ACC-402", $"'{command.Iban}' IBAN numarası zaten kayıtlı."));
        }

        var bankAccount = new BankAccount
        {
            TenantId = command.TenantId,
            CompanyId = command.CompanyId,
            Name = command.Name,
            BankName = command.BankName,
            BranchCode = command.BranchCode,
            AccountNumber = command.AccountNumber,
            Iban = command.Iban,
            AccountType = command.AccountType,
            CurrencyCode = command.CurrencyCode,
            AccountCodeId = command.AccountCodeId,
            IsActive = true
        };

        _db.AccountingBankAccounts.Add(bankAccount);
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
