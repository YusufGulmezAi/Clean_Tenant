using CleanTenant.Domain.Auditing;

namespace CleanTenant.Application.Features.System.Audit;

/// <summary>Audit Explorer için sorgu filtresi parametreleri.</summary>
public sealed record AuditFilter(
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    string? UserFullName,
    string? EntityType,
    AuditAction? Action,
    string? TenantName,
    Guid? CompanyId,
    int Page,
    int PageSize);
