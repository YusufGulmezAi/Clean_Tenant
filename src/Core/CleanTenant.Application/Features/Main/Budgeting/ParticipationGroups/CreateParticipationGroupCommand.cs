using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.ParticipationGroups;

/// <summary>
/// Yeni katılım grubu oluşturur. (CompanyId, Code) benzersizdir (BDG-501).
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record CreateParticipationGroupCommand(
    Guid TenantId,
    Guid CompanyId,
    string Code,
    string Name,
    string? Description = null) : IRequest<Result<Guid>>;
