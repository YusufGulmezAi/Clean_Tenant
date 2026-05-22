using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Periods;

/// <summary>
/// Kilitli bir muhasebe dönemini tekrar açar.
/// Mali yıl ClosedPermanent ise açılamaz.
/// </summary>
[RequirePermission("company.accounting.period.manage", "company.accounting.period.override")]
public sealed record OpenPeriodCommand(
    Guid CompanyId,
    Guid PeriodId) : IRequest<Result<bool>>;
