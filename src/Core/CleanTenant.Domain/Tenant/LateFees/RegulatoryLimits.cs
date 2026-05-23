namespace CleanTenant.Domain.Tenant.LateFees;

/// <summary>
/// <para>
/// Mevzuat kaynaklı sistem-geneli kilitli sabitler. UI değiştiremez. MVP'de domain
/// sabiti; ileride SystemAdmin yönetimli <c>MevzuatTavanlari</c> kataloğuna taşınır
/// (SDD Bölüm 8.4 / 13.2).
/// </para>
/// </summary>
public static class RegulatoryLimits
{
    /// <summary>
    /// KMK m.20 — gecikme tazminatı tavanı: aylık %5. Politika oranı bu değeri aşamaz;
    /// hesaplamada <c>min(policyRate, cap)</c> uygulanır.
    /// </summary>
    public const decimal KmkM20MonthlyCapPercent = 5m;
}
