using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.LookUp.Banks;

/// <summary>Banka referans verisi. EFT, sanal POS, kurumsal tahsilat entegrasyonları için. Sistem geneli sabit kütüphanesi.</summary>
public sealed class Bank : BaseEntity, IAggregateRoot, IHasUrlCode
{
    /// <summary>9 karakterlik Base58 URL kodu.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Banka tam adı (örn. Türkiye İş Bankası A.Ş.). Max 200 karakter.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Banka kısa adı (örn. İş Bankası). Max 30 karakter.</summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>EFT kodu (örn. "0032" ISO kodu). Max 10 karakter.</summary>
    public string? EftCode { get; set; }

    /// <summary>Sanal POS entegrasyonu bulunup bulunmadığı.</summary>
    public bool HasVirtualPosIntegration { get; set; }

    /// <summary>Kurumsal tahsilat entegrasyonu bulunup bulunmadığı.</summary>
    public bool HasCorporateCollectionIntegration { get; set; }
}
