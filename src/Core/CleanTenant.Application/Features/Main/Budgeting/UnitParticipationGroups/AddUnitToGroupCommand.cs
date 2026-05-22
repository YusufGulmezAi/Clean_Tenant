using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.UnitParticipationGroups;

/// <summary>
/// Bir Bağımsız Bölümü bir katılım grubuna ekler. Tarih penceresi (ValidFrom dahil,
/// ValidTo dahil) ile geçerli. Aynı (Group, Unit) çifti için aktif kayıt varsa
/// reddedilir (BDG-601); kapanmış üyelik yeniden açılabilir.
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record AddUnitToGroupCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid ParticipationGroupId,
    Guid UnitId,
    DateOnly ValidFrom,
    DateOnly? ValidTo = null,
    string? Notes = null) : IRequest<Result<Guid>>;
