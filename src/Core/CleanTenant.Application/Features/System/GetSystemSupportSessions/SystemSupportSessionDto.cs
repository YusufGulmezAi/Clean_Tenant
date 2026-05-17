using CleanTenant.Domain.Identity.Support;

namespace CleanTenant.Application.Features.System.GetSystemSupportSessions;

/// <summary>System operatör destek oturumu listesi DTO'su.</summary>
/// <param name="UrlCode">SupportSession URL kodu.</param>
/// <param name="OperatorEmail">Operatör e-postası.</param>
/// <param name="TargetTenantName">Hedef tenant adı.</param>
/// <param name="Mode">Oturum modu.</param>
/// <param name="Reason">Operatörün belirttiği sebep.</param>
/// <param name="StartedAt">Başlangıç anı.</param>
/// <param name="EndedAt">Bitiş anı (aktifse null).</param>
/// <param name="WriteActionCount">Yazma aksiyon sayısı.</param>
public sealed record SystemSupportSessionDto(
    string UrlCode,
    string OperatorEmail,
    string TargetTenantName,
    SupportSessionMode Mode,
    string Reason,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int WriteActionCount);
