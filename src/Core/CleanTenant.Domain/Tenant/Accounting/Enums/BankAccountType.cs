namespace CleanTenant.Domain.Tenant.Accounting.Enums;

/// <summary>
/// Banka hesabının türü.
/// </summary>
public enum BankAccountType
{
    /// <summary>Vadesiz mevduat hesabı (checking / demand deposit).</summary>
    Checking,

    /// <summary>Vadeli mevduat hesabı (savings / time deposit).</summary>
    Savings,

    /// <summary>Kredi limiti / kredili mevduat hesabı.</summary>
    CreditLine
}
