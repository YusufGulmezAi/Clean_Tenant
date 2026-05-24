using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Parties.CurrentAccount;

/// <summary>Bir BB'nin cari KPI özetini (tahakkuk/tahsilat/bakiye/vadesi geçmiş) getirir.</summary>
[RequirePermission("tenant.currentaccount.view")]
public sealed record GetUnitCurrentAccountKpiQuery(
    Guid CompanyId,
    Guid UnitId) : IRequest<Result<CurrentAccountKpi>>;
