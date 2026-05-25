using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Redis-backed auth session kaydının iç temsili. Login sırasında oluşturulur,
/// her HTTP isteğinde Redis'ten çekilir, JWT'nin "yetki dolusu" karşılığıdır.
/// </para>
/// <para>
/// JWT thin (sadece referans) olduğu için tüm zengin bilgi burada. Yetki
/// değişimi olduğunda bu kayıt güncellenir → bir sonraki istekte anında yansır.
/// </para>
/// </summary>
public sealed class AuthSession
{
    /// <summary>Session kimliği (JWT'deki <c>sid</c>).</summary>
    public required Guid SessionId { get; init; }

    /// <summary>Kullanıcı kimliği.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Sekme / persona context kimliği.</summary>
    public required Guid ContextId { get; init; }

    /// <summary>Kullanıcının e-posta adresi (denetim ve log için).</summary>
    public required string Email { get; init; }

    /// <summary>Kullanıcının username (genelde e-posta ile aynı).</summary>
    public required string UserName { get; init; }

    /// <summary>Aktif yetki kapsamı seviyesi.</summary>
    public required ScopeLevel ScopeLevel { get; init; }

    /// <summary>Aktif tenant; System scope'unda null.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>Aktif tenant adı (denormalize; audit/log enrichment için).</summary>
    public string? TenantName { get; init; }

    /// <summary>Kullanıcının görünen adı (FirstName + LastName, denormalize).</summary>
    public string? FullName { get; init; }

    /// <summary>Aktif şirket; Tenant ve üstü scope'larda null.</summary>
    public Guid? CompanyId { get; init; }

    /// <summary>Aktif bağımsız bölüm; Company ve üstü scope'larda null.</summary>
    public Guid? UnitId { get; init; }

    /// <summary>Bu bağlamda aktif rol isimleri.</summary>
    public required IReadOnlyList<string> Roles { get; init; }

    /// <summary>Rollerden türetilen aktif permission kodları.</summary>
    public required IReadOnlyList<string> Permissions { get; init; }

    /// <summary>Persona tarafı (Management / Portal); login persona zorunluluğundan gelir.</summary>
    public required PersonaSide PersonaSide { get; init; }

    /// <summary>Bu bir System Support oturumu mu (v0.1.5.b'de Support Mode için).</summary>
    public bool IsSystemSession { get; init; }

    /// <summary>Support Mode aktifse ilgili <c>SupportSession.Id</c>.</summary>
    public Guid? SupportSessionId { get; init; }

    /// <summary>
    /// Support Mode modu — Redis session içinde elevation sırasında mutate edilebilir,
    /// o yüzden <c>init</c> değil <c>set</c>. Değerler: None | ReadOnly | WriteEnabled | FullImpersonation.
    /// </summary>
    public string SupportMode { get; set; } = "None";

    /// <summary>
    /// Support Mode session'ında operatörün orijinal (Support Mode öncesi) session id'si.
    /// Exit sırasında bu session'a geri dönmek için kullanılır.
    /// </summary>
    public Guid? OriginalSessionId { get; init; }

    /// <summary>
    /// Full Impersonation aktifse, gerçek operatörün kullanıcı id'si.
    /// JWT'nin <c>sub</c>'u hedef kullanıcıdır; audit ve geri-çıkış için gerçek operatör burada.
    /// </summary>
    public Guid? ImpersonatedBy { get; init; }

    /// <summary>Session ilk oluşturma anı.</summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>Son aktivite anı; her HTTP isteğinde güncellenir (sliding TTL için).</summary>
    public DateTimeOffset LastActivity { get; set; }

    /// <summary>
    /// İzinlerin hangi "authorization damgası" ile çözüldüğü. Global damga
    /// (<c>IAuthorizationStampStore</c>) bir yetki değişiminde artar; bir sonraki
    /// istekte bu değer global damgayla eşleşmiyorsa izinler yeniden çözülür
    /// (re-login gerektirmeden). <c>null</c> = damgasız (eski/yeni oturum) →
    /// ilk istekte bir kez tazelenir.
    /// </summary>
    public string? AuthzStamp { get; init; }
}
