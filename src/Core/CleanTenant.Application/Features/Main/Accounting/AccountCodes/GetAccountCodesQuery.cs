using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// Şirkete ait hesap kodlarını filtreli listeler.
/// <para>
/// <paramref name="OnlyActive"/> = true ise yalnızca aktif hesaplar döner.
/// <paramref name="OnlyDetail"/> = true ise yalnızca yaprak hesaplar döner.
/// </para>
/// </summary>
[RequirePermission("company.accounting.account-plan.read")]
public sealed record GetAccountCodesQuery(
    Guid CompanyId,
    bool OnlyActive = false,
    bool OnlyDetail = false) : IRequest<Result<IReadOnlyList<AccountCodeListItem>>>;
