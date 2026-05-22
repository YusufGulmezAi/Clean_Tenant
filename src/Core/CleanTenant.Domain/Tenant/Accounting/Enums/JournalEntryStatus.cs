namespace CleanTenant.Domain.Tenant.Accounting.Enums;

/// <summary>
/// Yevmiye fişinin yaşam döngüsü durumu.
/// </summary>
public enum JournalEntryStatus
{
    /// <summary>Taslak — henüz onaylanmamış, silinebilir.</summary>
    Draft = 0,

    /// <summary>Onay bekliyor — dual-control akışında ikinci göz bekleniyor.</summary>
    PendingApproval = 1,

    /// <summary>Muhasebeleştirildi — dönemi etkileyen kalıcı kayıt.</summary>
    Posted = 2,

    /// <summary>İptal edildi — ters fiş kesilmiş; değiştirilemez.</summary>
    Voided = 3
}
