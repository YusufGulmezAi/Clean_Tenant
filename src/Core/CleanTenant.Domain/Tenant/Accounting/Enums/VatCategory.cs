namespace CleanTenant.Domain.Tenant.Accounting.Enums;

/// <summary>
/// KDV kategorisi — Türkiye KDV oranı sınıflandırması.
/// </summary>
public enum VatCategory
{
    /// <summary>KDV'den muaf işlem.</summary>
    NoVat,

    /// <summary>%1 KDV oranı (temel gıda, bazı tarım ürünleri).</summary>
    Vat1,

    /// <summary>%10 KDV oranı (temel tüketim malları).</summary>
    Vat10,

    /// <summary>%20 KDV oranı (genel oran).</summary>
    Vat20
}
