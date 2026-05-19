using CleanTenant.Domain.Auditing;

namespace CleanTenant.Application.Features.System.Audit;

/// <summary>Audit Explorer sorgu filtresi.</summary>
public sealed record AuditFilter(
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    Guid? UserId,
    string? UserEmail,
    string? EntityType,
    AuditAction? Action,
    Guid? TenantId,
    Guid? CompanyId,
    int Page,
    int PageSize);
