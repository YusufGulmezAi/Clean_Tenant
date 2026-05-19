using CleanTenant.Domain.Auditing;

namespace CleanTenant.Application.Features.System.Audit;

/// <summary>Audit Explorer liste satırı DTO'su.</summary>
public sealed record AuditListItem(
    Guid Id,
    DateTimeOffset Timestamp,
    string EntityType,
    Guid EntityId,
    AuditAction Action,
    string? UserFullName,
    string? UserEmail,
    Guid? TenantId,
    string? TenantName,
    Guid? CompanyId,
    string ChangesJson,
    string? IpAddress,
    string? UserAgent,
    string? BrowserName,
    string? OperatingSystem,
    string? RequestPath,
    string? ScopeLevel);
