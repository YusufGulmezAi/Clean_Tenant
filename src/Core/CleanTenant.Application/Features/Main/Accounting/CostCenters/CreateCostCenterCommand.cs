using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.CostCenters;

/// <summary>Yeni maliyet merkezi oluşturur.</summary>
[RequirePermission("company.accounting.account-plan.write")]
public sealed record CreateCostCenterCommand(
    Guid CompanyId,
    Guid TenantId,
    string Code,
    string Name,
    string? Description) : IRequest<Result<CostCenterListItem>>;
