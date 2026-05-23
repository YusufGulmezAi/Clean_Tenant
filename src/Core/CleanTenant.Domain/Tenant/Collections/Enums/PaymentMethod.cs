namespace CleanTenant.Domain.Tenant.Collections.Enums;

/// <summary>
/// Tahsilat ödeme yöntemi. Yevmiye fişinde kasa/banka hesabı seçimini ve
/// raporlamada gruplamayı etkiler.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Nakit — kasa hesabı (100).</summary>
    Cash = 0,

    /// <summary>Havale/EFT — banka hesabı (102).</summary>
    BankTransfer = 1,

    /// <summary>Kredi kartı / POS.</summary>
    CreditCard = 2,

    /// <summary>Çek.</summary>
    Check = 3,

    /// <summary>Diğer.</summary>
    Other = 9
}
