using CleanTenant.SharedKernel.Events;

namespace CleanTenant.Domain.Events.LateFees;

/// <summary>
/// Bir şirket için gecikme faizi tahakkukları üretildiğinde fırlatılır
/// (KMK m.20). Outbox tüketicileri: Audit, Bildirim ("gecikme faizi işlendi").
/// </summary>
public sealed record LateFeesGenerated(
    Guid CompanyId,
    DateOnly AsOfDate,
    int ChargedUnitCount,
    decimal TotalLateFeeAmount) : IDomainEvent;
