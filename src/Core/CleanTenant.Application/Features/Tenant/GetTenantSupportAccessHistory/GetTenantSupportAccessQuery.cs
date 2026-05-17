namespace CleanTenant.Application.Features.Tenant.GetTenantSupportAccessHistory;

/// <summary>
/// Tenant Admin için kendi tenant'ına yapılan destek erişimlerinin listesi.
/// Sayfalama: offset-based (Faz 1'de cursor'a geçilebilir).
/// </summary>
/// <param name="From">Başlangıç tarihi filtresi (UTC); null ise sınırsız geri.</param>
/// <param name="To">Bitiş tarihi filtresi (UTC); null ise şu ana kadar.</param>
/// <param name="Page">Sayfa numarası (0-tabanlı).</param>
/// <param name="PageSize">Sayfa boyutu; max 100.</param>
public sealed record GetTenantSupportAccessQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize);
