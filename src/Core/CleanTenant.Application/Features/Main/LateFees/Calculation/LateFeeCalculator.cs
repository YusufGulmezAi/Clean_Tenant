using CleanTenant.Domain.Tenant.LateFees;

namespace CleanTenant.Application.Features.Main.LateFees.Calculation;

/// <summary>
/// <see cref="ILateFeeCalculator"/> implementasyonu — basit faiz, KMK m.20 tavanlı.
/// MVP: bileşik faiz desteklenmez (politika <c>IsCompound</c> flag'i ileri kullanım için).
/// </summary>
public sealed class LateFeeCalculator : ILateFeeCalculator
{
    private const decimal DaysPerMonth = 30m;

    /// <inheritdoc />
    public decimal ComputeForDebt(
        decimal remainingPrincipal,
        DateOnly dueDate,
        int graceDays,
        decimal monthlyRatePercent,
        DateOnly asOfDate)
    {
        if (remainingPrincipal <= 0m)
            return 0m;

        // KMK m.20: oran aylık %5 tavanını aşamaz
        var effectiveRate = Math.Min(monthlyRatePercent, RegulatoryLimits.KmkM20MonthlyCapPercent);
        if (effectiveRate <= 0m)
            return 0m;

        // Gecikme vade + ödemesiz gün sonrası başlar
        var overdueStart = dueDate.AddDays(graceDays);
        var overdueDays = asOfDate.DayNumber - overdueStart.DayNumber;
        if (overdueDays <= 0)
            return 0m;

        return remainingPrincipal * (effectiveRate / 100m) * (overdueDays / DaysPerMonth);
    }
}
