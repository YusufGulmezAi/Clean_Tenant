using CleanTenant.SharedKernel.Entities;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Domain.Identity.Users;

/// <summary>
/// <para>
/// CleanTenant'ın global kullanıcı entity'si. ASP.NET Core Identity'nin
/// <see cref="IdentityUser{TKey}"/> sınıfından miras alır; password hash,
/// security stamp, lockout, 2FA bayrakları gibi standart Identity alanlarını
/// otomatik taşır.
/// </para>
/// <para>
/// <b>Single inheritance kısıtı:</b> <see cref="IdentityUser{TKey}"/> base
/// olduğu için <see cref="BaseEntity"/>'den miras alamaz; audit ve soft-delete
/// alanlarını arabirimler üzerinden manuel taşır.
/// </para>
/// <para>
/// <b>Multi-Tenancy:</b> Catalog DB'de yaşar ve tüm tenant'lar tarafından
/// paylaşılır. Tenant'a bağlama <c>UserRoleAssignment</c> üzerinden olur;
/// kullanıcının kendisi tenant'sız global bir kayıttır.
/// </para>
/// <para>
/// <b>2FA:</b> <c>TwoFactorEnabled</c> (IdentityUser'dan gelir) genel
/// bayrak. Hangi yöntemler (Authenticator/Email/SMS) aktif olduğu
/// <c>IdentityUserTokens</c> tablosundaki kayıtlardan ve <c>EmailConfirmed</c>
/// / <c>PhoneNumberConfirmed</c> bayraklarından türetilir.
/// </para>
/// </summary>
public sealed class User : IdentityUser<Guid>, IAuditable, ISoftDeletable, IHasUrlCode
{
    /// <summary>9 karakterlik Base58 URL kodu (kullanıcı profil sayfasında).</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Kullanıcının adı.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Kullanıcının soyadı.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// T.C. Kimlik Numarası veya Yabancı Kimlik Numarası (11 haneli, sadece rakam).
    /// Türkiye Mernis'te her ikisi de aynı checksum algoritmasını kullanır;
    /// YKN ilk hanesi 9'la başlar, gerisi TCKN ile aynı kuralları izler.
    /// Bireysel kullanıcılar için login identifier'ı olarak kullanılır.
    /// </summary>
    public string? Tckn { get; set; }

    /// <summary>
    /// TCKN/YKN admin tarafından doğrulanmış mı. Doğrulanmadan TCKN/YKN ile
    /// login kabul edilmez (güvenlik). Manuel / yetki kontrolleriyle açılır.
    /// </summary>
    public bool TcknVerified { get; set; }

    /// <summary>
    /// Vergi Kimlik Numarası (10 haneli, sadece rakam). Bireysel veya kurumsal
    /// vergi kimlik numarası. Tenant Admin'lerin kurumsal hesap onboarding'inde
    /// kullanılır; bireysel kullanıcılarda opsiyonel.
    /// </summary>
    public string? Vkn { get; set; }

    /// <summary>
    /// VKN doğrulanmış mı. Doğrulanmadan VKN ile login kabul edilmez.
    /// Gelir İdaresi entegrasyonu (Faz 2+) ya da admin manuel onayı.
    /// </summary>
    public bool VknVerified { get; set; }

    /// <summary>
    /// Son başarılı giriş zamanı (UTC); hiç giriş yapmadıysa null.
    /// Yeni cihaz / yeni IP tespiti ve security audit için.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>Son giriş yapılan IP adresi (kabaca lokasyon takibi için).</summary>
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// <para>
    /// Kullanıcının tercih ettiği dil (BCP-47, örn. <c>"tr-TR"</c>, <c>"en-US"</c>).
    /// Login sonrası culture cookie'sini bu değere set ederiz; her oturumda
    /// kullanıcının dili otomatik gelir (v0.2.10).
    /// </para>
    /// <para>
    /// <c>null</c> → sistem varsayılanı (TR). Kullanıcı AppBar'daki dil seçici
    /// üzerinden değiştirdiğinde burası güncellenir.
    /// </para>
    /// </summary>
    public string? PreferredCulture { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    public Guid? DeletedBy { get; set; }

    /// <summary>
    /// PostgreSQL <c>xmin</c> sistem sütununa eşlenir; optimistic concurrency
    /// token'ı. IdentityUser'ın kendi <see cref="IdentityUser{TKey}.ConcurrencyStamp"/>
    /// string token'ından ayrı; her ikisi de tutulur (Identity kendi
    /// stamp'iyle çalışır, EF Core'un xmin'i bizim convention).
    /// </summary>
    public uint RowVersion { get; set; }
}
