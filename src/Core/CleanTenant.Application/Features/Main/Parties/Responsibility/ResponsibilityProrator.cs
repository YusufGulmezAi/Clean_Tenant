using CleanTenant.Domain.Tenant.Parties.Enums;

namespace CleanTenant.Application.Features.Main.Parties.Responsibility;

/// <summary>
/// Saf (DB'siz) gün-bazlı proration algoritması. Bir ayın borcunu tenure
/// pencerelerine göre taraflara böler. Birim testleri bunu doğrudan test eder.
/// </summary>
public static class ResponsibilityProrator
{
    /// <summary>Malik tenure penceresi (proration girdisi).</summary>
    public sealed record OwnerWindow(Guid PartyId, DateOnly Start, DateOnly? End, decimal Share);

    /// <summary>Kiracı tenure penceresi (proration girdisi).</summary>
    public sealed record TenantWindow(Guid PartyId, DateOnly Start, DateOnly? End);

    /// <summary>
    /// (year, month) ayının tamamı [01–son gün] üzerinden, her günü sorumlu tarafa
    /// atayıp bitişik aynı-taraf günleri birleştirerek parçalar üretir. Tutar gün
    /// oranıyla bölünür; kuruş artığı + çözümlenemeyen gün payı en büyük parçaya eklenir
    /// (Σ Amount = total). Hiç tenure yoksa boş (PrimaryPartyId=null).
    /// </summary>
    public static ResponsibilityResult Prorate(
        int year, int month, decimal total, ResponsibilityMode mode,
        IReadOnlyList<OwnerWindow> owners, IReadOnlyList<TenantWindow> tenants)
    {
        var first = new DateOnly(year, month, 1);
        var totalDays = DateTime.DaysInMonth(year, month);

        // 1) Her günün sorumlusu
        var dayKey = new (Guid Party, ResponsibilityKind Kind)?[totalDays];
        for (var i = 0; i < totalDays; i++)
            dayKey[i] = ResolveDay(first.AddDays(i), mode, owners, tenants);

        // 2) Bitişik aynı-taraf günleri parçalara grupla (null günler kesinti)
        var segments = new List<(Guid Party, ResponsibilityKind Kind, DateOnly From, DateOnly To, int Len)>();
        var idx = 0;
        while (idx < totalDays)
        {
            if (dayKey[idx] is not { } cur) { idx++; continue; }
            var start = idx;
            while (idx < totalDays && dayKey[idx] is { } k && k.Party == cur.Party && k.Kind == cur.Kind) idx++;
            segments.Add((cur.Party, cur.Kind, first.AddDays(start), first.AddDays(idx - 1), idx - start));
        }

        if (segments.Count == 0)
            return new ResponsibilityResult(null, "Sorumlu atanamadı (tenure yok).", []);

        // 3) Gün oranıyla tutar
        var assigned = 0m;
        var splits = new List<ResponsibilitySplitDto>(segments.Count);
        foreach (var s in segments)
        {
            var amount = Math.Round(total * s.Len / totalDays, 2, MidpointRounding.AwayFromZero);
            assigned += amount;
            splits.Add(new ResponsibilitySplitDto(s.Party, s.Kind, s.From, s.To, s.Len, amount));
        }

        // 4) Kalan (yuvarlama + çözümlenemeyen gün payı) → en uzun parçaya
        var remainder = total - assigned;
        if (remainder != 0m)
        {
            var maxI = 0;
            for (var i = 1; i < splits.Count; i++)
                if (splits[i].DayCount > splits[maxI].DayCount) maxI = i;
            splits[maxI] = splits[maxI] with { Amount = splits[maxI].Amount + remainder };
        }

        // 5) Birincil = en çok güne sahip taraf
        var primary = splits.GroupBy(x => x.PartyId)
            .OrderByDescending(g => g.Sum(x => x.DayCount))
            .First().Key;

        var covered = segments.Sum(s => s.Len);
        var note = segments.Count == 1 && covered == totalDays
            ? (segments[0].Kind == ResponsibilityKind.Tenant ? "Dönem boyunca kiracı." : "Dönem boyunca malik.")
            : $"{segments.Count} parça ({covered}/{totalDays} gün).";

        return new ResponsibilityResult(primary, note, splits);
    }

    private static (Guid Party, ResponsibilityKind Kind)? ResolveDay(
        DateOnly d, ResponsibilityMode mode, IReadOnlyList<OwnerWindow> owners, IReadOnlyList<TenantWindow> tenants)
    {
        if (mode == ResponsibilityMode.TenantThenOwner)
        {
            var t = tenants
                .Where(x => x.Start <= d && (x.End is null || x.End >= d))
                .OrderBy(x => x.Start)
                .FirstOrDefault();
            if (t is not null)
                return (t.PartyId, ResponsibilityKind.Tenant);
        }

        var owner = owners
            .Where(x => x.Start <= d && (x.End is null || x.End >= d))
            .OrderByDescending(x => x.Share).ThenBy(x => x.Start)
            .FirstOrDefault();
        return owner is null ? null : (owner.PartyId, ResponsibilityKind.Owner);
    }
}
