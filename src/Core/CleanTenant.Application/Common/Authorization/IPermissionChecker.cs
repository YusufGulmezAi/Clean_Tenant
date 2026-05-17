namespace CleanTenant.Application.Common.Authorization;

/// <summary>
/// Aktif kullanıcı oturumunun (Redis session) sahip olduğu permission
/// kodlarına karşı yetki kontrolü yapan sözleşme. Redis-backed
/// implementasyonu <c>SessionPermissionChecker</c> (Infrastructure.Identity).
/// </summary>
public interface IPermissionChecker
{
    /// <summary>Tek bir permission kodu için kontrol.</summary>
    bool HasPermission(string permissionCode);

    /// <summary>Herhangi birine sahip mi (any-of / OR semantiği).</summary>
    bool HasAnyPermission(IReadOnlyList<string> permissionCodes);
}
