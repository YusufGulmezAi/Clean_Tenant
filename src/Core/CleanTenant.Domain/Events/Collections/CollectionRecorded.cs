using CleanTenant.SharedKernel.Events;

namespace CleanTenant.Domain.Events.Collections;

/// <summary>
/// Tahsilat kaydedildiğinde fırlatılır. Outbox tüketicileri: Audit, Muhasebe
/// (yevmiye fişi — handler içinde), Bildirim ("ödemeniz alındı").
/// </summary>
public sealed record CollectionRecorded(
    Guid CollectionId,
    Guid CompanyId,
    Guid UnitId,
    decimal Amount,
    DateOnly PaymentDate) : IDomainEvent;
