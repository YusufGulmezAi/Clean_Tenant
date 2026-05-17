using CleanTenant.Domain.Identity.Support;

namespace CleanTenant.Application.Features.Tenant.GetTenantSupportAccessHistory;

/// <summary>
/// Tenant Admin'in destek erişim geçmişi sayfasında satır olarak görüntülenen DTO.
/// </summary>
/// <param name="UrlCode">SupportSession'ın URL kodu.</param>
/// <param name="OperatorEmail">Destek operatörünün e-posta adresi.</param>
/// <param name="OperatorFullName">Operatörün adı + soyadı.</param>
/// <param name="Mode">Oturum modu (ReadOnly / WriteEnabled / FullImpersonation).</param>
/// <param name="Reason">Operatörün belirttiği sebep.</param>
/// <param name="StartedAt">Oturum başlangıç anı (UTC).</param>
/// <param name="EndedAt">Oturum bitiş anı (UTC); aktif oturumda null.</param>
/// <param name="WriteActionCount">Oturum içi write aksiyon sayısı (v0.1.7'de doldurulacak).</param>
public sealed record TenantSupportAccessDto(
    string UrlCode,
    string OperatorEmail,
    string OperatorFullName,
    SupportSessionMode Mode,
    string Reason,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int WriteActionCount);
