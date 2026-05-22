using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.CostCenters;

/// <summary>Maliyet merkezini günceller.</summary>
[RequirePermission("company.accounting.account-plan.write")]
public sealed record UpdateCostCenterCommand(
    Guid CompanyId,
    Guid CostCenterId,
    string Name,
    string? Description,
    bool IsActive) : IRequest<Result<CostCenterListItem>>;
