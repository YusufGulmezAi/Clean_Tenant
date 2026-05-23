namespace CleanTenant.Application.Features.Main.LateFees.Calculation;

/// <summary>
/// Gecikme faizi hesaplama servisi (saf, deterministik). Basit faiz; KMK m.20
/// aylık %5 tavanı içeride uygulanır.
/// </summary>
public interface ILateFeeCalculator
{
    /// <summary>
    /// Tek bir gecikmiş borç için basit faiz: <c>anapara × min(oran, KMK tavan)/100 ×
    /// gün/30</c>. Gecikme vade + <paramref name="graceDays"/> sonrası başlar;
    /// <paramref name="asOfDate"/> bu tarihten önce veya eşitse 0 döner.
    /// </summary>
    decimal ComputeForDebt(
        decimal remainingPrincipal,
        DateOnly dueDate,
        int graceDays,
        decimal monthlyRatePercent,
        DateOnly asOfDate);
}
