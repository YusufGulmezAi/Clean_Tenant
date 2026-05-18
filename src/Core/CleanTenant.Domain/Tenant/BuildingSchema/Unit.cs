using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.BuildingSchema;

/// <summary>
/// <para>
/// Bir <see cref="Building"/> bünyesindeki bağımsız bölüm (tapu ünitesi).
/// Bütçe dağıtımının temel birimi; arsa payı ve alan bilgileri aidat
/// hesaplamalarında kullanılır.
/// </para>
/// <para>
/// <b>Land Share:</b> Pay değeri olarak saklanır (int). Payda, aynı
/// yapıdaki tüm Unit'lerin LandShare değerlerinin toplamıdır — tapu sistemi ile uyumlu.
/// </para>
/// </summary>
public sealed class Unit : BaseEntity, IHasUrlCode, ITenantScoped
{
    /// <summary>9 karakterlik Base58 URL kodu.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Bağlı olduğu Yapı.</summary>
    public Guid BuildingId { get; set; }

    /// <summary>Bağımsız bölüm numarası (örn. "1", "2A", "B-05").</summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>Ulusal adres/numarataj sistemi numarası (opsiyonel).</summary>
    public string? NationalAddressCode { get; set; }

    /// <summary>Bağımsız bölümün tapu niteliği.</summary>
    public UnitType Type { get; set; }

    /// <summary>Net kullanım alanı (m²), iki ondalık hassasiyet.</summary>
    public decimal SquareMeters { get; set; }

    /// <summary>
    /// Arsa payı (pay değeri). Payda = yapıdaki tüm Unit'lerin LandShare toplamı.
    /// Örn: 15 pay / 1000 toplam → arsa payı 15/1000.
    /// </summary>
    public int LandShare { get; set; }

    /// <summary>Balkon, teras gibi tahsis alanı (m²); opsiyonel.</summary>
    public decimal? AllocatedArea { get; set; }

    /// <summary>Bulunduğu kat numarası (bodrum için negatif değer kullanılabilir).</summary>
    public int Floor { get; set; }

    /// <summary>Ana cephenin baktığı yön.</summary>
    public Orientation Orientation { get; set; }

    /// <summary>Oda ve salon sayısı kombinasyonu.</summary>
    public ApartmentLayout Layout { get; set; }

    /// <summary>Sıralama değeri; listelemede ve raporlarda kullanılır.</summary>
    public int SortOrder { get; set; }

    /// <summary>Bu Unit'in ait olduğu Building (navigation property).</summary>
    public Building Building { get; set; } = null!;
}
