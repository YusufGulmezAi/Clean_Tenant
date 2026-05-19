namespace CleanTenant.Application.Features.System.Localization;

/// <summary>Localization yönetim sayfası sayfalı sonuç.</summary>
public sealed record LocalizationPageResult(
    IReadOnlyList<LocalizationEntryListItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
