namespace CleanTenant.Application.Features.System.GetSystemSupportSessions;

/// <summary>
/// System operatörleri için tüm tenant'lara ait destek oturumlarının listesi.
/// Operatör veya tenant filtreleyebilir.
/// </summary>
/// <param name="From">Başlangıç tarihi filtresi (UTC).</param>
/// <param name="To">Bitiş tarihi filtresi (UTC).</param>
/// <param name="OperatorUserUrlCode">Operatör URL koduyla filtre (opsiyonel).</param>
/// <param name="TargetTenantUrlCode">Hedef tenant URL koduyla filtre (opsiyonel).</param>
/// <param name="Page">Sayfa numarası (0-tabanlı).</param>
/// <param name="PageSize">Sayfa boyutu; max 100.</param>
public sealed record GetSystemSupportSessionsQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? OperatorUserUrlCode,
    string? TargetTenantUrlCode,
    int Page,
    int PageSize);
