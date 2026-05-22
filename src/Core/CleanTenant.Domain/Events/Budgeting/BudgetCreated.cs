using CleanTenant.SharedKernel.Events;

namespace CleanTenant.Domain.Events.Budgeting;

/// <summary>
/// Yeni bir bütçe (taslak halinde) oluşturulduğunda fırlatılır.
/// Henüz yayınlı versiyon yoktur; sadece konteyner mevcuttur.
/// </summary>
public sealed record BudgetCreated(
    Guid BudgetId,
    Guid CompanyId,
    Guid FiscalYearId) : IDomainEvent;
