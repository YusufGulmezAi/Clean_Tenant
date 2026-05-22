namespace CleanTenant.Domain.Tenant.Accounting.Enums;

/// <summary>
/// Hesap kademesi. TDHP'de 3 kademeli hiyerarşiyi tanımlar.
/// </summary>
public enum AccountLevel
{
    /// <summary>Ana hesap — 3 hane (örn. "100").</summary>
    Main = 0,

    /// <summary>Yardımcı hesap — 6 hane (örn. "100.01").</summary>
    Sub = 1,

    /// <summary>Detay hesap — 9 hane (örn. "100.01.001"); yevmiye girilebilir.</summary>
    Detail = 2
}
