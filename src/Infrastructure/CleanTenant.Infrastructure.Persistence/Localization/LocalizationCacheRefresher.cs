using CleanTenant.Application.Common.Localization;

namespace CleanTenant.Infrastructure.Persistence.Localization;

/// <summary>
/// Application katmanının <see cref="ILocalizationCacheRefresher"/> kontratını
/// in-memory <see cref="LocalizationStore"/>'a köprüler. Update sonrası
/// <c>ReloadAsync</c> çağrısı tüm aktif kayıtları yeniden yükler.
/// </summary>
public sealed class LocalizationCacheRefresher : ILocalizationCacheRefresher
{
    private readonly LocalizationStore _store;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public LocalizationCacheRefresher(LocalizationStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public Task RefreshAsync(CancellationToken cancellationToken = default)
        => _store.ReloadAsync(cancellationToken);
}
