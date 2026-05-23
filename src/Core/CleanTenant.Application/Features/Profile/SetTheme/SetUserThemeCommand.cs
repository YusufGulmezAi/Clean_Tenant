using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Profile.SetTheme;

/// <summary>
/// Authenticated kullanıcının tema tercihini (renk preset adı + gece modu) DB'ye
/// kaydeder. Profil &gt; Tema sekmesindeki seçim veya üst bardaki gece modu düğmesi
/// tetikler; değer cihazlar arası taşınır ve her login'de uygulanır (v0.2.13.d).
/// </summary>
/// <param name="Preset">Renk preset adı (örn. "KurumsalMavi"); null → varsayılan.</param>
/// <param name="DarkMode">Gece modu açık mı.</param>
public sealed record SetUserThemeCommand(string? Preset, bool DarkMode) : IRequest<Result>;
