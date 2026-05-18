using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.Tenants;

/// <summary>
/// <para>
/// Yönetim'i <b>soft delete</b> eder. System scope only — TenantAdmin kendi
/// yönetimini silemez. Side-effect: tüm <c>UserRoleAssignment</c>'lar
/// (Tenant/Company/Unit scope) IsActive=false yapılır. User'lar dokunulmaz
/// (başka yönetimde rolü olabilir).
/// </para>
/// <para>
/// Reactivate / restore Faz 1.5+'da; şimdi tek-yön. Audit izi korunur.
/// </para>
/// </summary>
[RequirePermission("Tenant.Terminate")]
[TenantWriteOperation]
public sealed record DeleteTenantCommand(Guid TenantId) : IRequest<Result>;
