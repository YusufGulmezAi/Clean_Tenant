namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>
/// <see cref="TenantForm"/>'un render ve validasyon davranışını belirleyen mod.
/// </summary>
public enum TenantFormMode
{
    /// <summary>Sistem operatör yeni Yönetim oluşturur — tüm alanlar + Sorumlu Yönetici bloğu.</summary>
    Create = 1,

    /// <summary>Sistem operatör mevcut Yönetim'i düzenler — Sorumlu Yönetici bloğu yok.</summary>
    Edit = 2,

    /// <summary>TenantAdmin kendi Yönetim'inin ayarlarını düzenler — kimlik/BillingTier/dedicated DB read-only.</summary>
    Settings = 3,
}
