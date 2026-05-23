using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accruals.Queries;

/// <summary>
/// Bir BB'nin borç durumu özeti. v0.2.14 (FAZ 6): yalnız toplam tahakkuk +
/// vadesi geçen tutar hesaplanır. Tahsilat düşümü (ödenen/kalan) FAZ 7'de eklenecek.
/// </summary>
[RequirePermission("tenant.collection.view")]
public sealed record GetUnitDebtStatusQuery(
    Guid CompanyId,
    Guid UnitId) : IRequest<Result<UnitDebtStatus>>;

/// <summary>
/// BB borç durumu özeti. <see cref="PaidAmount"/> ve <see cref="RemainingAmount"/>
/// FAZ 7 (Tahsilat) gelene dek <see cref="TotalAccrued"/> ile aynı varsayılır
/// (henüz ödeme kaydı yok).
/// </summary>
public sealed record UnitDebtStatus(
    Guid UnitId,
    decimal TotalAccrued,
    decimal OverdueAmount,
    int AccrualCount,
    DateOnly? EarliestDueDate,
    decimal PaidAmount,
    decimal RemainingAmount);
