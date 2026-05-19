using CleanTenant.Domain.Localization;
using CleanTenant.Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Infrastructure.Persistence.Seeding;

/// <summary>
/// <para>
/// <see cref="LocalizationCatalog"/>'daki çevirileri DB'ye idempotent olarak
/// yükler. Her key için TR ve EN değerleri ayrı satır olarak yazılır.
/// </para>
/// <para>
/// <b>EN behavior:</b> Catalog'da EN explicit verildiyse onu kullanır
/// (<c>IsMachineTranslated=false</c>). Verilmediyse <c>"[EN] {tr}"</c>
/// placeholder oluşturur ve <c>IsMachineTranslated=true</c> işaretler — admin
/// elle revize eder (<c>/system/localization</c>).
/// </para>
/// <para>
/// AR / RU / DE seed edilmez (Seçenek-A: yalnız TR+EN ilk turda); admin paneli
/// üzerinden istenirse eklenir.
/// </para>
/// </summary>
public sealed class LocalizationSeeder
{
    private const string TrCulture = "tr-TR";
    private const string EnCulture = "en-US";

    private readonly CatalogDbContext _db;
    private readonly ILogger<LocalizationSeeder> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public LocalizationSeeder(CatalogDbContext db, ILogger<LocalizationSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Idempotent seed. Mevcut key+culture varsa <b>elle revize edilmiş</b>
    /// kayıtları (<c>IsMachineTranslated=false</c>) ezme; yalnız makine çevirisi
    /// veya hiç yoksa güncellenir/eklenir.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _db.LocalizedResources
            .ToDictionaryAsync(r => (r.Key, r.Culture), r => r, cancellationToken);

        var added = 0;
        var updated = 0;

        foreach (var def in LocalizationCatalog.All)
        {
            UpsertOne(existing, def.Key, TrCulture, def.Tr, isMachine: false, ref added, ref updated);

            var enValue = def.En ?? $"[EN] {def.Tr}";
            var enIsMachine = def.En is null;
            UpsertOne(existing, def.Key, EnCulture, enValue, enIsMachine, ref added, ref updated);
        }

        if (added > 0 || updated > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Localization seed: {Added} eklendi, {Updated} güncellendi (catalog: {Total} key)",
                added, updated, LocalizationCatalog.All.Count);
        }
        else
        {
            _logger.LogInformation(
                "Localization seed: değişiklik yok (catalog: {Total} key zaten mevcut)",
                LocalizationCatalog.All.Count);
        }
    }

    private void UpsertOne(
        Dictionary<(string Key, string Culture), LocalizedResource> existing,
        string key,
        string culture,
        string value,
        bool isMachine,
        ref int added,
        ref int updated)
    {
        if (existing.TryGetValue((key, culture), out var current))
        {
            // Elle revize edilmiş kaydı (IsMachineTranslated=false) ezmiyoruz —
            // sadece machine-stub veya silinmişse güncelle.
            if (!current.IsMachineTranslated && !isMachine)
            {
                // Hem mevcut hem yeni elle çevrili: yeni değer geldiyse güncelle
                // (çoğunlukla noop). Açıklama: catalog'da bir hata fark edilirse
                // dev güncellersin; admin'in bunu üstüne yazmayı tercih etmesi de mümkün.
                if (current.Value != value)
                {
                    current.Value = value;
                    updated++;
                }
                return;
            }

            // Mevcut machine ya da yeni machine — değişiklik varsa güncelle.
            var dirty = false;
            if (current.Value != value) { current.Value = value; dirty = true; }
            if (current.IsMachineTranslated != isMachine) { current.IsMachineTranslated = isMachine; dirty = true; }
            if (dirty) updated++;
            return;
        }

        var entry = _db.LocalizedResources.Add(new LocalizedResource
        {
            Key = key,
            Culture = culture,
            Value = value,
            IsMachineTranslated = isMachine,
        });
        entry.Property(nameof(LocalizedResource.Id)).CurrentValue = Guid.CreateVersion7();
        added++;
    }
}
