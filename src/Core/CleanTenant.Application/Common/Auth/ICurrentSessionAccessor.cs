namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// HTTP isteği üzerine, SessionLookupMiddleware'in yüklediği aktif
/// <see cref="AuthSession"/>'a erişimi sağlar. MigrationRunner / seed
/// gibi HTTP dışı bağlamlarda <see cref="Current"/> null döner.
/// </para>
/// </summary>
public interface ICurrentSessionAccessor
{
    /// <summary>
    /// Mevcut HTTP isteğinin auth session'ı; kimlik doğrulanmamış istek veya
    /// HTTP dışı bağlamda null.
    /// </summary>
    AuthSession? Current { get; }
}
