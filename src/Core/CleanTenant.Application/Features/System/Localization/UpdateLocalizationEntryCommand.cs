using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.System.Localization;

/// <summary>
/// Tek bir lokalizasyon kaydını (Key + Culture) yeni değerle günceller.
/// Kayıt yoksa hata döner; varsa <c>Value</c> ve <c>IsMachineTranslated=false</c>
/// olarak işaretlenir (admin manuel çeviri yaptığı için). Update sonrası
/// <c>LocalizationStore</c> cache'i yeniden yüklenir.
/// </summary>
[RequirePermission("System.Localization.Manage")]
public sealed record UpdateLocalizationEntryCommand(
    string Key,
    string Culture,
    string NewValue) : IRequest<Result>;
