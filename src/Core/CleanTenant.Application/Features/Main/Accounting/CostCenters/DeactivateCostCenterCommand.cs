using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.CostCenters;

/// <summary>Maliyet merkezini pasifleştirir.</summary>
[RequirePermission("company.accounting.account-plan.write")]
public sealed record DeactivateCostCenterCommand(
    Guid CompanyId,
    Guid CostCenterId) : IRequest<Result<bool>>;
