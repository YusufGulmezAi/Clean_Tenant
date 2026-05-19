using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.System.Localization;

/// <summary>
/// Lokalizasyon kayıtlarını filtre + sayfaya göre döner.
/// Yalnız <c>System.Localization.Manage</c> yetkisi olan kullanıcı çağırabilir.
/// </summary>
[RequirePermission("System.Localization.Manage")]
public sealed record GetLocalizationEntriesQuery(LocalizationEntryFilter Filter)
    : IRequest<LocalizationPageResult>;
