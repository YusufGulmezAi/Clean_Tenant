namespace CleanTenant.Domain.Tenant.Accounting.Enums;

/// <summary>
/// Hesap kodunun kaynağı — standart TDHP şablonundan mı yoksa
/// şirkete özgü tanımlı mı olduğunu belirtir.
/// </summary>
public enum AccountCodeSource
{
    /// <summary>TDHP standardından türetilmiş hesap.</summary>
    Standard,

    /// <summary>Şirketin kendisi tarafından tanımlanmış özel hesap.</summary>
    CompanyDefined
}
