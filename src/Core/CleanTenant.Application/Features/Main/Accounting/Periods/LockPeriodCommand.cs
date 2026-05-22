using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Periods;

/// <summary>
/// Açık bir muhasebe dönemini kilitler.
/// Kilitli dönemde yeni fiş girilemez.
/// </summary>
[RequirePermission("company.accounting.period.manage")]
public sealed record LockPeriodCommand(
    Guid CompanyId,
    Guid PeriodId) : IRequest<Result<bool>>;
