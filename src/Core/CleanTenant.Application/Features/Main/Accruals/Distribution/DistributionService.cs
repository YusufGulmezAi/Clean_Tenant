using CleanTenant.Domain.Tenant.Budgeting.Enums;

namespace CleanTenant.Application.Features.Main.Accruals.Distribution;

/// <summary>
/// <see cref="IDistributionService"/> implementasyonu. Saf hesaplama; DB bağımlılığı
/// yok. LRM yuvarlama 2 ondalık (kuruş) hassasiyetinde uygulanır.
/// </summary>
public sealed class DistributionService : IDistributionService
{
    /// <inheritdoc />
    public IReadOnlyList<UnitShare> Distribute(
        DistributionModel model,
        decimal totalAmount,
        IReadOnlyList<DistributionUnit> units)
    {
        if (units.Count == 0)
            return [];

        // Ağırlıkları belirle
        decimal[] weights = model switch
        {
            DistributionModel.Equal =>
                [.. units.Select(_ => 1m)],
            DistributionModel.BySquareMeter =>
                [.. units.Select(u => u.GrossSquareMeters)],
            _ => throw new NotSupportedException(
                $"Dağıtım modeli MVP'de desteklenmiyor: {model}")
        };

        var totalWeight = weights.Sum();

        // m² modelinde tüm m² 0 ise eşit dağıtıma düş (bölme hatasını önle)
        if (totalWeight <= 0m)
        {
            weights = [.. units.Select(_ => 1m)];
            totalWeight = units.Count;
        }

        return AllocateWithLrm(totalAmount, units, weights, totalWeight);
    }

    /// <summary>
    /// Largest Remainder Method: her payı 2 ondalığa floor'la, kalan kuruşları
    /// en büyük kesirli artıkları olan BB'lere birer birer dağıt.
    /// </summary>
    private static List<UnitShare> AllocateWithLrm(
        decimal totalAmount,
        IReadOnlyList<DistributionUnit> units,
        decimal[] weights,
        decimal totalWeight)
    {
        var n = units.Count;
        var floored = new decimal[n];
        var remainders = new decimal[n];
        var shares = new decimal[n];

        decimal allocated = 0m;
        for (var i = 0; i < n; i++)
        {
            var raw = totalAmount * weights[i] / totalWeight;
            var floorVal = Math.Floor(raw * 100m) / 100m; // 2 ondalık aşağı yuvarla
            floored[i] = floorVal;
            remainders[i] = raw - floorVal;
            shares[i] = totalWeight == 0m ? 0m : weights[i] / totalWeight;
            allocated += floorVal;
        }

        // Kalan kuruş sayısı
        var leftover = totalAmount - allocated;
        var centsToDistribute = (int)Math.Round(leftover * 100m, MidpointRounding.AwayFromZero);

        // En büyük kesirli artıktan başlayarak +0.01 ekle
        var order = Enumerable.Range(0, n)
            .OrderByDescending(i => remainders[i])
            .ThenBy(i => i)
            .ToArray();

        for (var k = 0; k < centsToDistribute && k < n; k++)
            floored[order[k]] += 0.01m;

        // centsToDistribute > n olabilir mi? Teoride hayır (her artık < 0.01),
        // ama güvenlik için kalanı ilk BB'ye yansıt.
        if (centsToDistribute > n)
            floored[order[0]] += 0.01m * (centsToDistribute - n);

        var result = new List<UnitShare>(n);
        for (var i = 0; i < n; i++)
            result.Add(new UnitShare(units[i].UnitId, floored[i], shares[i]));

        return result;
    }
}
