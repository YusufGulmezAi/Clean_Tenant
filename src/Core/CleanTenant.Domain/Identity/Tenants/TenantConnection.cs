using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Identity.Tenants;

/// <summary>
/// <para>
/// Dedicated DB modunda çalışan tenant'lar için PostgreSQL bağlantı dizgesi
/// kaydı. Hassas bilgi olduğundan sırların entity'den ayrı bir tabloda
/// tutulması güvenlik prensibi gereği.
/// </para>
/// <para>
/// <b>Şifreleme:</b> <see cref="ConnectionStringEncrypted"/> alanı v0.1.5'te
/// Data Protection API ile şifrelenecek (Faz 2'de Vault'a göç). v0.1.4'te
/// plaintext olarak saklanır — yalnız Dev / Demo ortamı için kabul.
/// </para>
/// <para>
/// URL'i yoktur, soft-delete uygulanmaz (gerçek silme tercih); audit alanları
/// rotation tarihini izlemek için tutulur.
/// </para>
/// </summary>
public sealed class TenantConnection : BaseEntity
{
    /// <summary>Bu bağlantının ait olduğu tenant.</summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Şifrelenmiş PostgreSQL bağlantı dizgesi (host, port, database, kullanıcı, şifre).
    /// v0.1.4'te plaintext; v0.1.5'te DataProtection API ile şifrelenecek.
    /// </summary>
    public string ConnectionStringEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// Bu bağlantı aktif olarak kullanılıyor mu. Rotation sırasında eski
    /// bağlantı pasif tutulur, yeni bağlantı aktif yapılır; geçiş penceresi
    /// için her iki kayıt da bir süre saklanır.
    /// </summary>
    public bool IsActive { get; set; }
}
