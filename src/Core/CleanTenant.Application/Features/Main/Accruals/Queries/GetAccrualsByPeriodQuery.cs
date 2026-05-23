using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accruals.Queries;

/// <summary>Bir şirketin belirli dönemdeki (Yıl + opsiyonel Ay) tahakkuklarını listeler.</summary>
[RequirePermission("tenant.collection.view")]
public sealed record GetAccrualsByPeriodQuery(
    Guid CompanyId,
    int Year,
    int? Month = null) : IRequest<Result<IReadOnlyList<AccrualListItem>>>;

/// <summary>Tahakkuk başlığı liste öğesi.</summary>
public sealed record AccrualListItem(
    Guid Id,
    AccrualSource Source,
    int Year,
    int Month,
    string Description,
    decimal TotalAmount,
    int DetailCount,
    Guid? JournalEntryId,
    DateTimeOffset GeneratedAt);
