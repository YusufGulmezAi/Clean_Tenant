using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.BankAccounts;

/// <summary>
/// Şirkete ait banka hesaplarını listeler.
/// <para>
/// <paramref name="OnlyActive"/> = true ise yalnızca aktif hesaplar döner.
/// </para>
/// </summary>
[RequirePermission("company.accounting.bank-account.read")]
public sealed record GetBankAccountsQuery(
    Guid CompanyId,
    bool OnlyActive = false) : IRequest<Result<IReadOnlyList<BankAccountListItem>>>;
