namespace CleanTenant.SharedKernel.Context;

/// <summary>
/// <para>
/// Mevcut isteğin kimlik doğrulanmış kullanıcı bilgisini ifade eder.
/// JWT claim'lerinden türetilen kullanıcı ID'si, kullanıcı adı, e-posta,
/// aktif rol ve permission listesi taşır.
/// </para>
/// <para>
/// <b>Implementasyon konumu:</b> Concrete sınıf <c>Infrastructure.Identity</c>
/// içinde yaşar; <c>HttpContext</c>'ten okunur (Blazor Server ve WebApi için
/// farklı kaynaklarla; arabirim aynı).
/// </para>
/// <para>
/// <b>Kullanım yerleri:</b> Audit interceptor (CreatedBy / UpdatedBy /
/// DeletedBy alanları), authorization behavior (permission listesi),
/// loglama enricher'ı (her log satırına UserId enjekte).
/// </para>
/// </summary>
public interface IUserContext
{
    /// <summary>Kullanıcı kimliği; kimlik doğrulaması yapılmamışsa null.</summary>
    Guid? UserId { get; }

    /// <summary>Kullanıcının login adı; null olabilir.</summary>
    string? UserName { get; }

    /// <summary>Kullanıcının e-posta adresi; null olabilir.</summary>
    string? Email { get; }

    /// <summary>Geçerli bir kimlik doğrulamasının var olup olmadığı.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Aktif bağlamdaki rol isimleri (örn. <c>TenantAdmin</c>, <c>Malik</c>).</summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>Rollerden türetilen, aktif bağlamdaki permission anahtarları (örn. <c>Invoice.Approve</c>).</summary>
    IReadOnlyCollection<string> Permissions { get; }
}
