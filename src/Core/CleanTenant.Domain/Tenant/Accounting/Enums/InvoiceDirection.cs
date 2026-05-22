namespace CleanTenant.Domain.Tenant.Accounting.Enums;

/// <summary>
/// Faturanın akış yönü — gelen mi, giden mi.
/// </summary>
public enum InvoiceDirection
{
    /// <summary>Gelen fatura — tedarikçi/satıcıdan alınan, gider kaydedilen fatura.</summary>
    Incoming,

    /// <summary>Giden fatura — müşteriye/alıcıya kesilen, gelir kaydedilen fatura.</summary>
    Outgoing
}
