namespace CleanTenant.Domain.Tenant.Accounting.Enums;

/// <summary>
/// KDV beyanname dönemi — şirketin vergi dairesine bildirdiği KDV periyodu.
/// </summary>
public enum VatPeriod
{
    /// <summary>Aylık KDV beyannamesi (varsayılan).</summary>
    Monthly,

    /// <summary>Üç aylık (çeyrek) KDV beyannamesi.</summary>
    Quarterly
}
