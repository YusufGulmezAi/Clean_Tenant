using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// <para>
/// Mevcut yayınlı bütçeyi revize eder. Eski versiyonun <c>ValidTo</c>'su
/// <c>ValidFrom - 1</c>'e ayarlanır; yeni V(N+1) versiyonu yayınlanır.
/// </para>
/// <para>
/// Yeni versiyon, eski versiyondaki <c>BudgetLineVersion</c>'ların kopyalarını
/// içerir; <see cref="LineOverrides"/> ile sağlanan kalemler için tutarlar
/// değiştirilir ve <c>IsManualOverride = true</c> + <c>OverrideReason = Reason</c>
/// işaretlenir.
/// </para>
/// <para>
/// MVP'de revize tek adımda (draft + publish) yapılır; Wave 3'te taslak revizyon
/// + ayrı onay/yayın adımı eklenebilir.
/// </para>
/// </summary>
[RequirePermission("tenant.budget.publish")]
public sealed record ReviseBudgetCommand(
    Guid CompanyId,
    Guid BudgetId,
    DateOnly ValidFrom,
    string Reason,
    IReadOnlyList<BudgetLineOverride>? LineOverrides = null) : IRequest<Result<Guid>>;

/// <summary>
/// Revizyon sırasında belirli bir kalemin yeni tutarını/dağıtımını verir.
/// Null alanlar eski değer korunur. <see cref="BudgetLineId"/> eski versiyonda
/// olmalı; yoksa BDG-805 hatası.
/// </summary>
public sealed record BudgetLineOverride(
    Guid BudgetLineId,
    decimal? NewPlannedAmount,
    PaymentSchedule? NewPaymentSchedule,
    DistributionModel? NewDistributionModel,
    Guid? NewParticipationGroupId,
    string? NewDistributionConfig,
    int? NewDueDayOfMonth);
