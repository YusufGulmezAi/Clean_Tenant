using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.LateFees.SetLateFeePolicy;

/// <summary>
/// <para>
/// Gecikme faizi politikasını ekler/günceller (upsert). <see cref="BudgetId"/>
/// <c>null</c> ise şirket-geneli varsayılan, dolu ise o bütçeye özel override.
/// Aynı kapsamda mevcut politika varsa üzerine yazılır.
/// </para>
/// <para>
/// Oran KMK m.20 tavanı (aylık %5) ile sınırlıdır; aşılırsa LFP-001 döner.
/// </para>
/// </summary>
[RequirePermission("tenant.latefee.configure")]
public sealed record SetLateFeePolicyCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid? BudgetId,
    decimal MonthlyRatePercent,
    bool IsCompound,
    int GraceDays,
    Guid IncomeAccountCodeId) : IRequest<Result<LateFeePolicyResult>>;

/// <summary>Gecikme faizi politikası upsert sonucu.</summary>
public sealed record LateFeePolicyResult(Guid PolicyId, bool Created);
