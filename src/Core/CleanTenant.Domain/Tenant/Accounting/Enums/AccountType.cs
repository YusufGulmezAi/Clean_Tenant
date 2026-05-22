namespace CleanTenant.Domain.Tenant.Accounting.Enums;

/// <summary>
/// Hesabın bilanço karakteri; borç/alacak dengesini yönlendirir.
/// </summary>
public enum AccountType
{
    /// <summary>Aktif hesap — normal bakiye borç tarafındadır.</summary>
    Active,

    /// <summary>Pasif hesap — normal bakiye alacak tarafındadır.</summary>
    Passive,

    /// <summary>Karma hesap — hem borç hem alacak bakiyesi taşıyabilir.</summary>
    ActivePassive
}
