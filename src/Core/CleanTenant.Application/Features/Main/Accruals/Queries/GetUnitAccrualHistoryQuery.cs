using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accruals.Queries;

/// <summary>
/// Bir Bağımsız Bölümün tahakkuk geçmişi (malik/sakin görünümü). Tarih aralığı
/// opsiyonel; her satır kalem kırılımını (LineBreakdownJson) içerir.
/// </summary>
[RequirePermission("tenant.collection.view")]
public sealed record GetUnitAccrualHistoryQuery(
    Guid CompanyId,
    Guid UnitId,
    DateOnly? From = null,
    DateOnly? To = null) : IRequest<Result<IReadOnlyList<UnitAccrualItem>>>;

/// <summary>BB tahakkuk geçmişi öğesi.</summary>
public sealed record UnitAccrualItem(
    Guid AccrualDetailId,
    Guid AccrualId,
    AccrualSource Source,
    int Year,
    int Month,
    string Description,
    decimal Amount,
    DateOnly DueDate,
    string? LineBreakdownJson);
