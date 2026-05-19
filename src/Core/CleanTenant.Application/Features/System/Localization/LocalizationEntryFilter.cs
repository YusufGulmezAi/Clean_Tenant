namespace CleanTenant.Application.Features.System.Localization;

/// <summary>Localization yönetim sayfası için sorgu filtresi.</summary>
public sealed record LocalizationEntryFilter(
    string Culture,
    string? SearchTerm,
    bool OnlyMachineTranslated,
    int Page,
    int PageSize);
