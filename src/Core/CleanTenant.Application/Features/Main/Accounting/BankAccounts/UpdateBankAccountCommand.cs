using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.BankAccounts;

/// <summary>
/// Mevcut banka hesabını günceller.
/// </summary>
[RequirePermission("company.accounting.bank-account.write")]
public sealed record UpdateBankAccountCommand(
    Guid BankAccountId,
    Guid CompanyId,
    string Name,
    string BankName,
    string? BranchCode,
    string AccountNumber,
    string? Iban,
    BankAccountType AccountType,
    string CurrencyCode,
    Guid? AccountCodeId,
    bool IsActive) : IRequest<Result<BankAccountDetail>>;
