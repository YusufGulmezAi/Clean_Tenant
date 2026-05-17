using CleanTenant.Infrastructure.Persistence.Catalog;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Infrastructure.Persistence.Seeding;

/// <summary>
/// <para>
/// Demo ortamı için ek seed verisi (paydaş gösterimleri için). Şu an
/// minimum iskelet; v0.1.4.b kapsamında demo tenant + birkaç örnek site
/// + Company / Building / Unit ileride (Faz 1) bu sınıfa eklenecek.
/// </para>
/// <para>
/// <b>Şimdilik:</b> <see cref="DevSeedData"/> ile aynı içeriği uygular
/// (admin + demo tenant). Faz 1'de zenginleştirilir.
/// </para>
/// </summary>
public sealed class DemoSeedData
{
    private readonly CatalogDbContext _db;
    private readonly DevSeedData _devSeed;
    private readonly ILogger<DemoSeedData> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DemoSeedData(CatalogDbContext db, DevSeedData devSeed, ILogger<DemoSeedData> logger)
    {
        _db = db;
        _devSeed = devSeed;
        _logger = logger;
    }

    /// <summary>Demo seed senaryosunu uygular (şimdilik dev'in eşi).</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Demo seed (iskelet): Dev senaryosu uygulanıyor. Faz 1'de zenginleştirilecek.");
        await _devSeed.SeedAsync(cancellationToken);

        // TODO Faz 1: Demo için ek company, building, unit, fatura, ödeme örnekleri.
    }
}
