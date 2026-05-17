namespace CleanTenant.Domain.Identity.Tenants;

/// <summary>
/// Tenant'ın faturalama seviyesi. Hibrit multi-tenancy kararını ve özellik
/// kullanım limitlerini etkiler (örn. Enterprise → dedicated DB).
/// </summary>
public enum BillingTier
{
    /// <summary>Ücretsiz katman; sınırlı kullanım, shared DB.</summary>
    Free = 1,

    /// <summary>Standart ücretli katman; shared DB.</summary>
    Standard = 2,

    /// <summary>Kurumsal katman; dedicated DB ve SLA garantisi sağlanabilir.</summary>
    Enterprise = 3,
}
