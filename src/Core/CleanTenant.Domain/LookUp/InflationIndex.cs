using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.LookUp;

/// <summary>
/// <para>
/// Enflasyon endeksi — TMS 29 kapsamındaki enflasyon muhasebesi hesaplamaları
/// için TÜİK ÜFE/TÜFE verilerini saklayan Catalog DB referans tablosu.
/// </para>
/// <para>
/// <b>Kullanım:</b> Parasal olmayan varlıkların yeniden değerleme katsayısı
/// edinim tarihi ile raporlama dönemi arasındaki endeks değerleri kullanılarak
/// hesaplanır: Katsayı = Raporlama Dönemi Endeksi / Edinim Dönemi Endeksi.
/// </para>
/// <para>
/// <b>Multi-tenancy:</b> LookUp entity'si olduğundan <c>ITenantScoped</c>
/// implement etmez; Catalog DB'de tutulur, tüm tenant'lar paylaşır.
/// </para>
/// </summary>
public sealed class InflationIndex : BaseEntity
{
    /// <summary>Endeks yılı (takvim yılı).</summary>
    public int Year { get; set; }

    /// <summary>Endeks ayı (1–12).</summary>
    public int Month { get; set; }

    /// <summary>
    /// TÜİK tarafından açıklanan ÜFE veya TÜFE endeks değeri.
    /// Katsayı hesaplamalarında kesirli hassasiyet gerektirir.
    /// </summary>
    public decimal IndexValue { get; set; }
}
