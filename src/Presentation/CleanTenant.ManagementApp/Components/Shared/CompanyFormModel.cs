using CleanTenant.Domain.Tenant.Companies;

namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>
/// Site oluşturma / düzenleme formu için binding modeli.
/// CreateCompanyCommand / UpdateCompanyCommand'a dönüştürülür.
/// </summary>
public sealed class CompanyFormModel
{
    /// <summary>Site kimliği (düzenleme modunda dolu, oluşturma modunda boş).</summary>
    public Guid Id { get; set; }

    /// <summary>Site adı. Zorunlu, max 256 karakter.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Yasal ad. Opsiyonel, max 512 karakter.</summary>
    public string? LegalName { get; set; }

    /// <summary>VKN (Vergi Kimlik Numarası). Opsiyonel, format ^[0-9]{10}$.</summary>
    public string? Vkn { get; set; }

    /// <summary>E-posta adresi. Opsiyonel, geçerli format.</summary>
    public string? Email { get; set; }

    /// <summary>Telefon numarası. Opsiyonel, max 20 karakter.</summary>
    public string? Phone { get; set; }

    /// <summary>Site durumu. Yeni kayıtta varsayılan Active; düzenleme modunda mevcut değer yüklenir.</summary>
    public CompanyStatus Status { get; set; } = CompanyStatus.Active;
}

/// <summary>
/// Site formu modu — render ve validasyon kararlarını belirler.
/// </summary>
public enum CompanyFormMode
{
    /// <summary>Yeni site oluşturma.</summary>
    Create,

    /// <summary>Site bilgilerini düzenleme.</summary>
    Edit,
}
