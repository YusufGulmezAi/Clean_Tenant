using CleanTenant.Domain.Tenant.Accruals.Enums;
using CleanTenant.SharedKernel.Events;

namespace CleanTenant.Domain.Events.Accruals;

/// <summary>
/// <para>
/// Tahakkuk üretildiğinde fırlatılır. Outbox üzerinden tüketiciler:
/// Audit, Bildirim (maliklere "yeni borç tahakkuk etti").
/// </para>
/// </summary>
public sealed record AccrualGenerated(
    Guid AccrualId,
    Guid CompanyId,
    AccrualSource Source,
    int Year,
    int Month,
    decimal TotalAmount,
    int DetailCount) : IDomainEvent;
