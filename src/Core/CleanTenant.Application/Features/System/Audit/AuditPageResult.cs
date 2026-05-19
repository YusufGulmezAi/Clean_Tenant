namespace CleanTenant.Application.Features.System.Audit;

/// <summary>Audit Explorer sayfalı sonuç.</summary>
public sealed record AuditPageResult(
    IReadOnlyList<AuditListItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
