namespace CleanTenant.Infrastructure.Identity.Authorization;

/// <summary>
/// CleanTenant'a özel ASP.NET Core authorization policy isimleri.
/// Endpoint'lerde <c>.RequireAuthorization(AuthorizationPolicies.X)</c>
/// formuyla kullanılır. Her policy bir <c>IAuthorizationHandler</c>
/// ile bağlanır; handler aktif <c>AuthSession</c>'a bakar.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Mevcut session System scope'unda olmalı.</summary>
    public const string SystemScope = "RequireSystemScope";

    /// <summary>Mevcut session aktif Support Mode (herhangi bir alt modda) içinde olmalı.</summary>
    public const string SupportModeActive = "RequireSupportModeActive";

    /// <summary>Mevcut session Support Mode WriteEnabled / FullImpersonation olmalı.</summary>
    public const string SupportWriteEnabled = "RequireSupportWriteEnabled";

    /// <summary>Mevcut session Tenant scope'unda olmalı (Tenant Admin akışları için).</summary>
    public const string TenantScope = "RequireTenantScope";
}
