using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.BankAccounts;

/// <summary>
/// Banka hesabını pasifleştirir.
/// </summary>
[RequirePermission("company.accounting.bank-account.write")]
public sealed record DeactivateBankAccountCommand(
    Guid BankAccountId,
    Guid CompanyId) : IRequest<Result<bool>>;
