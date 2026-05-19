namespace CleanTenant.Application.Features.System.Localization;

/// <summary>Localization yönetim sayfası liste satırı DTO'su.</summary>
public sealed record LocalizationEntryListItem(
    Guid Id,
    string Key,
    string Culture,
    string Value,
    bool IsMachineTranslated,
    DateTimeOffset UpdatedAt,
    Guid? UpdatedBy);
