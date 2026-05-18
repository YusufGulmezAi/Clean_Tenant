namespace CleanTenant.Application.Common.Authorization;

/// <summary>
/// <para>
/// v0.2.3.c — Support Mode v2 marker. Bir komutun <b>Yönetim/Site verisini
/// değiştirdiğini</b> işaretler. Sistem operatörü destek bağlamında ve
/// <c>SupportMode = "ReadOnly"</c> ise, bu attribute'u taşıyan komutlar
/// <c>AUTH-SUPPORT-WRITE-DENIED</c> ile reddedilir.
/// </para>
/// <para>
/// <b>Opt-in</b> tasarım: AuthorizationBehavior bu marker'ı arar; varsa
/// kontrol yapar, yoksa pass-through. Bu sayede oturum hareketi komutları
/// (SwitchTenant, Logout, Refresh, 2FA vb.) marker olmadan her zaman çalışır;
/// yalnız gerçek tenant-veri yazma komutları (CreateCompanyCommand,
/// UpdateUserCommand, vb.) bu marker'ı taşır.
/// </para>
/// </summary>
/// <example>
/// <code>
/// [TenantWriteOperation]
/// public sealed record CreateCompanyCommand(...) : IRequest&lt;Result&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TenantWriteOperationAttribute : Attribute;
