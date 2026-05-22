using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Periods;

/// <summary>
/// Kilitli bir dönemi tekrar açar. Bu işlem override yetkisi gerektirir.
/// Mali yıl ClosedPermanent ise kilit açılamaz.
/// </summary>
[RequirePermission("company.accounting.period.override")]
public sealed record UnlockPeriodCommand(
    Guid CompanyId,
    Guid PeriodId) : IRequest<Result<bool>>;
