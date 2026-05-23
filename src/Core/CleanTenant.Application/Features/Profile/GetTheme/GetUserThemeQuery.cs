using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Profile.GetTheme;

/// <summary>
/// Authenticated kullanıcının kayıtlı tema tercihini (renk preset adı + gece modu)
/// döner. Tema servisi circuit başlangıcında bunu çağırıp temayı uygular; böylece
/// her login'de kullanıcının teması cihazdan bağımsız gelir (v0.2.13.d).
/// </summary>
public sealed record GetUserThemeQuery : IRequest<Result<UserThemeResult>>;

/// <summary>Kullanıcının tema tercihi.</summary>
/// <param name="Preset">Renk preset adı (örn. "KurumsalMavi"); null → varsayılan.</param>
/// <param name="DarkMode">Gece modu açık mı.</param>
public sealed record UserThemeResult(string? Preset, bool DarkMode);
