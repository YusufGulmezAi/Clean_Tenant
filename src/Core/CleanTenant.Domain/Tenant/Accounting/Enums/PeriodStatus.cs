namespace CleanTenant.Domain.Tenant.Accounting.Enums;

/// <summary>
/// Muhasebe dönemi veya mali yılın kilitlenme durumu.
/// </summary>
public enum PeriodStatus
{
    /// <summary>Açık — yeni fiş girilebilir ve mevcut fişler düzenlenebilir.</summary>
    Open,

    /// <summary>Kilitli — geçici kapanış; yeni fiş girilemez, ancak yönetici açabilir.</summary>
    Locked,

    /// <summary>Kalıcı kapalı — kesinleşmiş dönem; hiçbir değişiklik yapılamaz.</summary>
    ClosedPermanent
}
