using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Collections.ApplyAdvance;

/// <summary>
/// Bir BB'nin biriken avans bakiyesini (fazla ödemelerden kalan
/// <c>Collection.UnallocatedAmount</c>) açık tahakkuklara TBK m.101 sırasıyla mahsup eder.
/// <para>
/// 120-negatif modelde avans nakdi zaten tahsil edildiği ve 120 bakiyesi avansı
/// içerdiği için bu işlem <b>yalnız yardımcı defter</b> hareketidir (yeni
/// <c>CollectionAllocation</c> satırları + <c>UnallocatedAmount</c> düşümü) —
/// <b>yeni yevmiye fişi açılmaz</b>.
/// </para>
/// </summary>
[RequirePermission("tenant.collection.record")]
public sealed record ApplyAdvanceCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid UnitId) : IRequest<Result<AdvanceApplicationResult>>;

/// <summary>Avans mahsup sonucu.</summary>
public sealed record AdvanceApplicationResult(
    decimal AppliedAmount,
    int AllocationCount,
    decimal RemainingAdvance);
