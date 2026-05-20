using CleanTenant.Domain.LookUp.Districts;
using CleanTenant.Domain.LookUp.Neighborhoods;
using CleanTenant.Domain.LookUp.Provinces;
using CleanTenant.Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Infrastructure.Persistence.Seeding;

/// <summary>
/// <para>
/// Türkiye il/ilçe/mahalle coğrafya tablolarını CSV'den idempotent olarak
/// seed eder. Eğer Provinces/Districts/Neighborhoods tablolarından <b>en az
/// biri</b> dolu ise tüm seed atlanır — yarım kalmış seed durumunu önler.
/// </para>
/// <para>
/// <b>CSV format</b> (UTF-8, <c>;</c> ayraçlı, ilk satır header):
/// <c>İl Sıra No;PLAKA KODU;İl Adı;İLÇE SIRA NO;İlçe Adı;MAHALLE SIRA NO;Mahalle Adı</c>.
/// Her satır bir mahalle; il ve ilçe defalarca tekrarlanır.
/// </para>
/// <para>
/// CSV csproj <c>Content</c> ile output'a kopyalanır;
/// <c>AppContext.BaseDirectory/data/seed/turkey-administrative-regions.csv</c>
/// yolundan okunur.
/// </para>
/// </summary>
public sealed class LookUpSeeder
{
    private const string CsvRelativePath = "data/seed/turkey-administrative-regions.csv";

    private readonly CatalogDbContext _db;
    private readonly ILogger<LookUpSeeder> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public LookUpSeeder(CatalogDbContext db, ILogger<LookUpSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Coğrafya seed. Idempotent — tablolardan biri dolu ise atlar.
    /// </summary>
    public async Task SeedGeographyAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Provinces.AnyAsync(cancellationToken)
            || await _db.Districts.AnyAsync(cancellationToken)
            || await _db.Neighborhoods.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("LookUp coğrafya seed: tablolarda kayıt mevcut, seed atlandı.");
            return;
        }

        var csvPath = Path.Combine(AppContext.BaseDirectory, CsvRelativePath);
        if (!File.Exists(csvPath))
        {
            _logger.LogWarning("LookUp coğrafya CSV bulunamadı: {Path} — seed atlandı.", csvPath);
            return;
        }

        _logger.LogInformation("LookUp coğrafya seed başladı (CSV: {Path}).", csvPath);

        var lines = await File.ReadAllLinesAsync(csvPath, cancellationToken);
        if (lines.Length < 2)
        {
            _logger.LogWarning("LookUp coğrafya CSV boş veya yalnız header içeriyor — seed atlandı.");
            return;
        }

        var rows = ParseRows(lines).ToList();
        _logger.LogInformation("LookUp coğrafya CSV: {RowCount} satır parse edildi.", rows.Count);

        // Performans: ChangeTracker AutoDetectChanges kapalı (yığın insert için)
        var autoDetectOriginal = _db.ChangeTracker.AutoDetectChangesEnabled;
        _db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            var provinceMap = await SeedProvincesAsync(rows, cancellationToken);
            var districtMap = await SeedDistrictsAsync(rows, provinceMap, cancellationToken);
            await SeedNeighborhoodsAsync(rows, districtMap, cancellationToken);
        }
        finally
        {
            _db.ChangeTracker.AutoDetectChangesEnabled = autoDetectOriginal;
        }

        _logger.LogInformation("LookUp coğrafya seed tamamlandı.");
    }

    private static IEnumerable<CsvRow> ParseRows(string[] lines)
    {
        // Header'ı atla
        for (var i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(';');
            if (parts.Length < 7) continue;
            yield return new CsvRow(
                ProvincePlateCode: parts[1].Trim(),
                ProvinceName: parts[2].Trim(),
                DistrictName: parts[4].Trim(),
                NeighborhoodName: parts[6].Trim());
        }
    }

    private async Task<Dictionary<string, Guid>> SeedProvincesAsync(
        List<CsvRow> rows, CancellationToken ct)
    {
        var distinctProvinces = rows
            .GroupBy(r => r.ProvinceName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Name = g.Key,
                PlateCode = int.TryParse(g.First().ProvincePlateCode, out var pc) ? pc : (int?)null,
            })
            .OrderBy(p => p.PlateCode ?? int.MaxValue)
            .ToList();

        var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in distinctProvinces)
        {
            var entity = new Province { Name = p.Name, PlateCode = p.PlateCode };
            var entry = _db.Provinces.Add(entity);
            entry.Property(nameof(Province.Id)).CurrentValue = Guid.CreateVersion7();
            map[p.Name] = (Guid)entry.Property(nameof(Province.Id)).CurrentValue!;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("LookUp coğrafya seed: {Count} il eklendi.", distinctProvinces.Count);
        return map;
    }

    private async Task<Dictionary<(string Province, string District), Guid>> SeedDistrictsAsync(
        List<CsvRow> rows, Dictionary<string, Guid> provinceMap, CancellationToken ct)
    {
        var distinctDistricts = rows
            .GroupBy(r => (r.ProvinceName, r.DistrictName), TupleComparer)
            .Select(g => g.Key)
            .ToList();

        var map = new Dictionary<(string, string), Guid>(TupleComparer);
        foreach (var (province, district) in distinctDistricts)
        {
            if (!provinceMap.TryGetValue(province, out var provinceId))
            {
                continue;
            }

            var entity = new District { Name = district, ProvinceId = provinceId };
            var entry = _db.Districts.Add(entity);
            entry.Property(nameof(District.Id)).CurrentValue = Guid.CreateVersion7();
            map[(province, district)] = (Guid)entry.Property(nameof(District.Id)).CurrentValue!;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("LookUp coğrafya seed: {Count} ilçe eklendi.", distinctDistricts.Count);
        return map;
    }

    private async Task SeedNeighborhoodsAsync(
        List<CsvRow> rows,
        Dictionary<(string Province, string District), Guid> districtMap,
        CancellationToken ct)
    {
        // 50K+ satır olabilir — chunk'lı SaveChanges bellek + log spam'i sınırlar.
        const int ChunkSize = 5000;
        var added = 0;
        var batch = 0;

        foreach (var row in rows)
        {
            if (!districtMap.TryGetValue((row.ProvinceName, row.DistrictName), out var districtId))
            {
                continue;
            }

            var entity = new Neighborhood { Name = row.NeighborhoodName, DistrictId = districtId };
            var entry = _db.Neighborhoods.Add(entity);
            entry.Property(nameof(Neighborhood.Id)).CurrentValue = Guid.CreateVersion7();
            added++;
            batch++;

            if (batch >= ChunkSize)
            {
                await _db.SaveChangesAsync(ct);
                _db.ChangeTracker.Clear();
                batch = 0;
            }
        }

        if (batch > 0)
        {
            await _db.SaveChangesAsync(ct);
            _db.ChangeTracker.Clear();
        }

        _logger.LogInformation("LookUp coğrafya seed: {Count} mahalle eklendi.", added);
    }

    private static readonly TupleStringComparer TupleComparer = new();

    private sealed record CsvRow(
        string ProvincePlateCode,
        string ProvinceName,
        string DistrictName,
        string NeighborhoodName);

    private sealed class TupleStringComparer : IEqualityComparer<(string, string)>
    {
        public bool Equals((string, string) x, (string, string) y) =>
            string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string, string) obj) =>
            HashCode.Combine(
                obj.Item1?.ToUpperInvariant(),
                obj.Item2?.ToUpperInvariant());
    }
}
