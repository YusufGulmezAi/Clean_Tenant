using CleanTenant.SharedKernel.Context;

namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>
/// Kullanıcı oluşturma / düzenleme formu için binding modeli.
/// CreateSystemUserCommand / UpdateUserCommand'a dönüştürülür.
/// </summary>
public sealed class UserFormModel
{
    /// <summary>Kullanıcı kimliği (düzenleme modunda dolu; oluşturma modunda boş).</summary>
    public Guid Id { get; set; }

    /// <summary>URL kodu (düzenleme modunda dolu).</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Ad. Zorunlu, max 100 karakter.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Soyad. Zorunlu, max 100 karakter.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>E-posta adresi (aynı zamanda login identifier). Zorunlu.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Telefon numarası. Opsiyonel, max 20 karakter.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Şifre. Yalnız Create modunda zorunlu; Edit modunda boş kalabilir.</summary>
    public string? Password { get; set; }

    /// <summary>Seçili rol ID listesi. En az bir rol zorunlu.</summary>
    public List<Guid> SelectedRoleIds { get; set; } = [];

    /// <summary>Kullanıcı aktif mi (Edit modunda gösterilir).</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Kullanıcı formu modu — render ve validasyon kararlarını belirler.
/// </summary>
public enum UserFormMode
{
    /// <summary>Yeni kullanıcı oluşturma.</summary>
    Create,

    /// <summary>Kullanıcı bilgilerini düzenleme.</summary>
    Edit,
}

/// <summary>
/// Drawer içeriği — form tipi.
/// </summary>
public enum UserDrawerMode
{
    /// <summary>Kapalı.</summary>
    None,

    /// <summary>Kullanıcı oluşturma / düzenleme formu.</summary>
    UserForm,

    /// <summary>Şifre sıfırlama formu.</summary>
    ResetPassword,

    /// <summary>Mevcut kullanıcıyı tenant'a atama formu.</summary>
    AssignUser,

    /// <summary>Akıllı kullanıcı onboarding sihirbazı (arama + oluştur/ata).</summary>
    Onboarding,
}
