namespace CleanTenant.Domain.Tenant.Parties.Enums;

/// <summary>Cari (Party) türü — gerçek kişi veya tüzel kişi.</summary>
public enum PartyKind
{
    /// <summary>Gerçek kişi (birey). TCKN taşır.</summary>
    Individual = 0,

    /// <summary>Tüzel kişi (şirket/kurum). VKN + ticari unvan taşır.</summary>
    Legal = 1
}
