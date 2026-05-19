using MediatR;

namespace CleanTenant.Application.Features.Catalog.Roles;

/// <summary>
/// Yeni rol oluşturma komutu. <see cref="TenantId"/> ve <see cref="CompanyId"/>
/// sadece System scope assigner tarafından kullanılır; Tenant/Company scope
/// assigner'lar session'larından otomatik dolduracaktır (handler içinde override).
/// </summary>
public sealed record CreateRoleCommand(
    string Name,
    int Scope,
    string? Description,
    Guid? TenantId = null,
    Guid? CompanyId = null) : IRequest<Guid>;
