using CleanTenant.Domain.Tenant.Accounting.Enums;

namespace CleanTenant.Application.Features.Main.Accounting.BankAccounts;

/// <summary>Banka hesabı liste elemanı — GetBankAccountsQuery dönüş tipi.</summary>
public record BankAccountListItem(
    Guid Id,
    string Name,
    string BankName,
    string? Iban,
    BankAccountType AccountType,
    string CurrencyCode,
    bool IsActive);

/// <summary>Banka hesabı tam detay — GetBankAccountDetailQuery dönüş tipi.</summary>
public record BankAccountDetail(
    Guid Id,
    string Name,
    string BankName,
    string? BranchCode,
    string AccountNumber,
    string? Iban,
    BankAccountType AccountType,
    string CurrencyCode,
    Guid? AccountCodeId,
    bool IsActive);
