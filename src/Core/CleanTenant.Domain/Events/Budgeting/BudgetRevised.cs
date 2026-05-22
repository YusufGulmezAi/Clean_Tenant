using CleanTenant.SharedKernel.Events;

namespace CleanTenant.Domain.Events.Budgeting;

/// <summary>
/// <para>
/// Yıl ortası bütçe revizyonu yapıldığında fırlatılır. Eski versiyonun
/// <c>ValidTo</c>'su <c>NewValidFrom - 1</c>'e ayarlanır, yeni versiyon (V2, V3 …)
/// oluşur ve <c>Budget.CurrentVersionId</c> yeni versiyona güncellenir.
/// </para>
/// <para>
/// Eski versiyona bağlı tahakkuklar değişmez; yeni dönemde üretilecek tahakkuklar
/// yeni versiyonu kullanır.
/// </para>
/// </summary>
public sealed record BudgetRevised(
    Guid BudgetId,
    Guid OldVersionId,
    Guid NewVersionId,
    int NewVersionNumber,
    DateOnly NewValidFrom,
    string Reason) : IDomainEvent;
