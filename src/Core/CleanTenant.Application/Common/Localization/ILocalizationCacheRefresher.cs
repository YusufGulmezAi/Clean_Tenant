namespace CleanTenant.Application.Common.Localization;

/// <summary>
/// Lokalizasyon in-memory cache'ini DB'den yeniden yükler. Admin paneli
/// üzerinden çeviri güncellenince çağrılır; concrete implementasyon
/// Infrastructure katmanındaki <c>LocalizationStore</c>'u sarmalar.
/// </summary>
public interface ILocalizationCacheRefresher
{
    /// <summary>Cache'i DB'den atomik olarak yeniden yükler.</summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
