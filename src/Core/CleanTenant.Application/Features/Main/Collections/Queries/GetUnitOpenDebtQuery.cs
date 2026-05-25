using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Collections.Queries;

/// <summary>
/// Bir BB'nin açık (ödenmemiş kalan) tahakkuk detaylarını <b>TBK m.101 sırasıyla</b>
/// döner (en eski vade önce; aynı vadede önce gecikme faizi, sonra anapara).
/// Tahsilat sihirbazının FIFO mahsup önizlemesi bu sırayı kullanır —
/// <see cref="RecordCollection.RecordCollectionCommandHandler"/> ile birebir aynı dağıtım.
/// </summary>
[RequirePermission("tenant.collection.view")]
public sealed record GetUnitOpenDebtQuery(
    Guid CompanyId,
    Guid UnitId) : IRequest<Result<UnitOpenDebt>>;

/// <summary>BB açık borç özeti — FIFO sıralı satırlar + toplam.</summary>
public sealed record UnitOpenDebt(
    decimal TotalOpen,
    IReadOnlyList<OpenDebtLine> Lines);

/// <summary>Tek bir açık tahakkuk detayının kalan tutarı (FIFO sırasında).</summary>
public sealed record OpenDebtLine(
    Guid AccrualDetailId,
    DateOnly DueDate,
    AccrualSource Source,
    int Year,
    int Month,
    string Description,
    decimal OpenAmount);
