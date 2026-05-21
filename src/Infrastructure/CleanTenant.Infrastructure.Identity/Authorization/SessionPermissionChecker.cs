using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Infrastructure.Identity.Authorization;

/// <summary>
/// <see cref="IPermissionChecker"/>'ın Redis session'a dayalı implementasyonu.
/// Aktif <see cref="AuthSession.Permissions"/> listesinden okur — rol/permission
/// değişimi Redis session'a yansıdığı an yetki de değişir (anlık revocation).
/// </summary>
internal sealed class SessionPermissionChecker : IPermissionChecker
{
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SessionPermissionChecker(ICurrentSessionAccessor sessionAccessor)
    {
        _sessionAccessor = sessionAccessor;
    }

    /// <inheritdoc />
    public bool HasPermission(string permissionCode)
    {
        var session = _sessionAccessor.Current;
        if (session is null) return false;
        // System scope kullanıcısı tüm yetkileri bypass eder.
        if (session.ScopeLevel == ScopeLevel.System) return true;
        return session.Permissions.Contains(permissionCode);
    }

    /// <inheritdoc />
    public bool HasAnyPermission(IReadOnlyList<string> permissionCodes)
    {
        var session = _sessionAccessor.Current;
        if (session is null || permissionCodes.Count == 0) return false;
        // System scope kullanıcısı tüm yetkileri bypass eder.
        if (session.ScopeLevel == ScopeLevel.System) return true;

        var owned = session.Permissions;
        for (var i = 0; i < permissionCodes.Count; i++)
        {
            if (owned.Contains(permissionCodes[i]))
            {
                return true;
            }
        }
        return false;
    }
}
