namespace CleanTenant.Domain.Identity.Tenants;

/// <summary>
/// Bir tenant'ın yaşam döngüsündeki durumu.
/// Operasyonel olarak hangi tenant'ların aktif, askıda ya da kapanmış olduğunu belirler.
/// </summary>
public enum TenantStatus
{
    /// <summary>Aktif; tüm işlevler kullanılabilir.</summary>
    Active = 1,

    /// <summary>Geçici olarak askıya alındı (örn. ödeme alınamadı). Giriş engellenir ama veri korunur.</summary>
    Suspended = 2,

    /// <summary>Hesap kapatıldı. Veri saklama süresi sonunda silinir.</summary>
    Terminated = 3,
}
