using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Identity.Support;

/// <summary>
/// <para>
/// Bir System operatörünün bir tenant'a destek amacıyla girdiği oturumun
/// kaydı. Her giriş için yeni bir kayıt oluşturulur; çıkışta
/// <see cref="EndedAt"/> setlenir.
/// </para>
/// <para>
/// <b>Şeffaflık:</b> Tenant Admin, kendi tenant'ına ait <c>SupportSession</c>
/// kayıtlarını "Destek Erişim Geçmişi" sayfasında görebilir. KVKK uyumu ve
/// müşteri güveni için kritik.
/// </para>
/// <para>
/// <b>İlişki:</b> <c>SupportSession</c> aggregate kökü; <c>OperatorUserId</c>
/// üzerinden <see cref="Users.User"/>'a referans verir; tenant'a doğrudan
/// foreign key vardır.
/// </para>
/// </summary>
public sealed class SupportSession : BaseEntity, IAggregateRoot, IHasUrlCode
{
    /// <summary>9 karakterlik Base58 URL kodu (oturum detay sayfası için).</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Destek modunda olan System operatörünün kullanıcı kimliği.</summary>
    public Guid OperatorUserId { get; set; }

    /// <summary>Operatörün girdiği hedef tenant.</summary>
    public Guid TargetTenantId { get; set; }

    /// <summary>Operatör tenant içinde belirli bir company'ye odaklanmışsa o company'nin id'si; aksi takdirde null.</summary>
    public Guid? TargetCompanyId { get; set; }

    /// <summary>
    /// True impersonation modunda hedef tenant kullanıcısının id'si;
    /// <see cref="SupportSessionMode.FullImpersonation"/> dışındaki modlarda null.
    /// </summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>Operatörün seçtiği destek modu.</summary>
    public SupportSessionMode Mode { get; set; }

    /// <summary>
    /// Operatörün girişe başlarken zorunlu yazdığı sebep. DB CHECK constraint
    /// ile minimum 20 karakter dayatılır; ticket numarası veya açıklayıcı not.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Oturumun başladığı an (UTC).</summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>Oturumun bittiği an (UTC); aktif oturumlarda null.</summary>
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>
    /// Oturum içinde yapılan yazma aksiyonlarının sayısı. ReadOnly modunda 0
    /// kalır; WriteEnabled veya FullImpersonation modunda artar.
    /// </summary>
    public int WriteActionCount { get; set; }

    /// <summary>
    /// Hedef tenant'ın admin'ine oturumla ilgili bildirim gönderildi mi.
    /// Mevcut akışta (Sade) varsayılan false; ileride (Sıkı akış'ta) bildirim
    /// yollanırsa true setlenir.
    /// </summary>
    public bool CustomerNotified { get; set; }

    /// <summary>Operatörün IP adresi (audit için).</summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>Operatörün tarayıcı / uygulama bilgisi (User-Agent).</summary>
    public string UserAgent { get; set; } = string.Empty;
}
