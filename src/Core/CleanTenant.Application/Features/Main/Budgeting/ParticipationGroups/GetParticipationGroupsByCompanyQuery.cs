using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.ParticipationGroups;

/// <summary>Şirkete ait katılım gruplarını listeler (aktif üyelik sayısı dahil).</summary>
[RequirePermission("tenant.budget.view")]
public sealed record GetParticipationGroupsByCompanyQuery(
    Guid CompanyId,
    bool OnlyActive = true) : IRequest<Result<IReadOnlyList<ParticipationGroupListItem>>>;

/// <summary>Katılım grubu liste öğesi.</summary>
public sealed record ParticipationGroupListItem(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    int MemberCount);
