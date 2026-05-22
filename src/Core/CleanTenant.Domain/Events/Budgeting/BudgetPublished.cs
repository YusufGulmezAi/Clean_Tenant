using CleanTenant.SharedKernel.Events;

namespace CleanTenant.Domain.Events.Budgeting;

/// <summary>
/// Bütçenin ilk versiyonu (V1) yayınlandığında fırlatılır.
/// Bu olaydan sonra tahakkuk üretimi mümkün hale gelir (FAZ 6).
/// </summary>
public sealed record BudgetPublished(
    Guid BudgetId,
    Guid VersionId,
    int VersionNumber,
    DateOnly ValidFrom) : IDomainEvent;
