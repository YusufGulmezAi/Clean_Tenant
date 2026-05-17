using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Infrastructure.Identity.Context;

/// <summary>
/// <para>
/// HTTP isteğindeki aktif auth session'ı <see cref="IUserContext"/> arabirimine
/// köprüleyen scoped implementasyon. SessionLookupMiddleware tarafından
/// Redis'ten yüklenen session burada okunur.
/// </para>
/// <para>
/// SystemUserContext'in (Persistence katmanındaki default) HTTP scope'undaki
/// override'ı. MigrationRunner gibi HTTP dışı bağlamlarda SystemUserContext
/// devrede kalır.
/// </para>
/// </summary>
public sealed class HttpUserContext : IUserContext, ICurrentSessionAccessor
{
    /// <summary>Mevcut isteğin auth session'ı; doğrulanmamış istek için null.</summary>
    public AuthSession? Current { get; set; }

    /// <inheritdoc />
    public Guid? UserId => Current?.UserId;

    /// <inheritdoc />
    public string? UserName => Current?.UserName;

    /// <inheritdoc />
    public string? Email => Current?.Email;

    /// <inheritdoc />
    public bool IsAuthenticated => Current is not null;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles => Current?.Roles ?? [];

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions => Current?.Permissions ?? [];
}
