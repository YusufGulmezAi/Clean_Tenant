using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.FiscalYears;

/// <summary>Şirkete ait mali yılları listeler.</summary>
[RequirePermission("company.accounting.account-plan.read")]
public sealed record GetFiscalYearsQuery(
    Guid CompanyId) : IRequest<Result<IReadOnlyList<FiscalYearListItem>>>;
