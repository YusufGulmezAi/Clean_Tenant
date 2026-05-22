using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>Tek hesap kodunun tam detayını döner.</summary>
[RequirePermission("company.accounting.account-plan.read")]
public sealed record GetAccountCodeDetailQuery(
    Guid CompanyId,
    Guid AccountCodeId) : IRequest<Result<AccountCodeDetail>>;
