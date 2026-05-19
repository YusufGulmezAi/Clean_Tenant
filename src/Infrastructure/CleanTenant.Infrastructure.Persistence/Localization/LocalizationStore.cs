using CleanTenant.Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Infrastructure.Persistence.Localization;

/// <summary>
/// <para>
/// Tüm çeviri kayıtlarını in-memory tutan singleton store. Startup'ta DB'den
/// preload, sonrasında <see cref="Get"/> ile sync (microsaniye) lookup.
/// Admin paneli üzerinden çeviri güncellendiğinde <see cref="ReloadAsync"/>
/// çağrılarak yenilenir (v0.2.10.g).
/// </para>
/// <para>
/// Yapısı: <c>Dictionary&lt;culture, Dictionary&lt;key, value&gt;&gt;</c>.
/// Bellek maliyeti küçük (toplam string sayısı x ortalama uzunluk); büyük
/// uygulamalarda lazy per-culture loading'e geçilebilir.
/// </para>
/// </summary>
public sealed class LocalizationStore
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LocalizationStore> _logger;
    private readonly object _lock = new();
    private Dictionary<string, Dictionary<string, string>> _translations = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>DI bağımlılıklarını alır.</summary>
    public LocalizationStore(IServiceProvider services, ILogger<LocalizationStore> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Belirtilen kültürdeki anahtarın değerini sync olarak döner; yoksa null.
    /// Fallback chain caller tarafında yönetilir (<see cref="DbStringLocalizer"/>).
    /// </summary>
    public string? Get(string culture, string key)
    {
        var snapshot = _translations;
        if (snapshot.TryGetValue(culture, out var dict)
            && dict.TryGetValue(key, out var value))
        {
            return value;
        }
        return null;
    }

    /// <summary>Şu an yüklü kültürlerin listesi (sırasız).</summary>
    public IReadOnlyCollection<string> LoadedCultures => _translations.Keys.ToList();

    /// <summary>
    /// Veritabanındaki tüm aktif <c>LocalizedResource</c> kayıtlarını yükler ve
    /// in-memory store'u atomik olarak değiştirir (lock ile). Startup'ta ve
    /// admin update sonrasında çağrılır.
    /// </summary>
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CatalogDbContext>>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var rows = await db.LocalizedResources
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Select(r => new { r.Key, r.Culture, r.Value })
            .ToListAsync(cancellationToken);

        var newDict = rows
            .GroupBy(r => r.Culture, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal),
                StringComparer.OrdinalIgnoreCase);

        lock (_lock)
        {
            _translations = newDict;
        }

        _logger.LogInformation(
            "Localization store yüklendi: {Cultures} kültür, {Total} kayıt",
            newDict.Count, rows.Count);
    }
}
