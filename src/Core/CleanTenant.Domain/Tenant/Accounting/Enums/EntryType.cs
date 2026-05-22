namespace CleanTenant.Domain.Tenant.Accounting.Enums;

/// <summary>
/// Yevmiye fişinin muhasebe niteliği.
/// </summary>
public enum EntryType
{
    /// <summary>Açılış fişi — dönem başı devir kaydı.</summary>
    Opening,

    /// <summary>Kapanış fişi — dönem sonu hesap kapatma kaydı.</summary>
    Closing,

    /// <summary>Olağan fiş — dönem içi standart muhasebe kaydı.</summary>
    Normal,

    /// <summary>Düzeltme fişi — hata düzeltme amacıyla yapılan kayıt.</summary>
    Adjustment,

    /// <summary>Virman fişi — hesaplar arası transfer kaydı.</summary>
    Transfer,

    /// <summary>Düzeltme/storno fişi — önceki bir kaydın iptali ve yeniden yazımı.</summary>
    Correction,

    /// <summary>Enflasyon düzeltme fişi — TMS 29 kapsamında reel değer uyarlaması.</summary>
    InflationAdjustment
}
