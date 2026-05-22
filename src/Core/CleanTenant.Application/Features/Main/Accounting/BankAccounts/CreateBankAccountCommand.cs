using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.BankAccounts;

/// <summary>
/// Şirkete yeni banka hesabı ekler.
/// </summary>
[RequirePermission("company.accounting.bank-account.write")]
public sealed record CreateBankAccountCommand(
    Guid CompanyId,
    Guid TenantId,
    string Name,
    string BankName,
    string? BranchCode,
    string AccountNumber,
    string? Iban,
    BankAccountType AccountType,
    string CurrencyCode,
    Guid? AccountCodeId) : IRequest<Result<BankAccountDetail>>;
