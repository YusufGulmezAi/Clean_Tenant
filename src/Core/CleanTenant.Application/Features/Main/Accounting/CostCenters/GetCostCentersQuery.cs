using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.CostCenters;

/// <summary>Şirkete ait maliyet merkezlerini listeler.</summary>
[RequirePermission("company.accounting.account-plan.read")]
public sealed record GetCostCentersQuery(
    Guid CompanyId,
    bool OnlyActive = false) : IRequest<Result<IReadOnlyList<CostCenterListItem>>>;
