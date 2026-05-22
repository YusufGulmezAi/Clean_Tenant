using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Periods;

/// <summary>Mali yıla ait muhasebe dönemlerini listeler.</summary>
[RequirePermission("company.accounting.account-plan.read")]
public sealed record GetPeriodsQuery(
    Guid CompanyId,
    Guid FiscalYearId) : IRequest<Result<IReadOnlyList<PeriodListItem>>>;
