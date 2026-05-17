using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Support;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Domain.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Common.Persistence;

/// <summary>
/// <para>
/// Catalog veri tabanı için Application katmanının okuduğu soyutlama.
/// Concrete <c>CatalogDbContext</c> Infrastructure.Persistence projesindedir;
/// Application handler'ları yalnız bu arabirim üzerinden DbContext'i tüketir
/// — concrete tip ve Identity tooling Application katmanına sızmaz.
/// </para>
/// <para>
/// <b>Saklanan koleksiyonlar:</b> Tenant, TenantConnection, User, Role,
/// Permission, RolePermission, UserRoleAssignment, RefreshToken, SupportSession.
/// </para>
/// <para>
/// Identity'nin kendi tabloları (<c>IdentityUserClaim</c>, <c>IdentityUserLogin</c>,
/// <c>IdentityUserToken</c>, <c>IdentityUserRole</c>, <c>IdentityRoleClaim</c>)
/// ASP.NET Core Identity tarafından yönetilir ve <c>UserManager</c> /
/// <c>RoleManager</c> üzerinden erişilir; bu arabirim üzerinden expose edilmez.
/// </para>
/// </summary>
public interface ICatalogDbContext
{
    /// <summary>Tenant kayıtları.</summary>
    DbSet<Tenant> Tenants { get; }

    /// <summary>Dedicated DB tenant'ları için bağlantı bilgileri.</summary>
    DbSet<TenantConnection> TenantConnections { get; }

    /// <summary>Global kullanıcı kayıtları.</summary>
    DbSet<User> Users { get; }

    /// <summary>Rol kayıtları (System / Tenant / Company / Unit scope).</summary>
    DbSet<Role> Roles { get; }

    /// <summary>Permission kayıtları.</summary>
    DbSet<Permission> Permissions { get; }

    /// <summary>Rol ↔ Permission eşlemeleri.</summary>
    DbSet<RolePermission> RolePermissions { get; }

    /// <summary>Kullanıcı rol atamaları (scope bilgili).</summary>
    DbSet<UserRoleAssignment> UserRoleAssignments { get; }

    /// <summary>Refresh token kayıtları (rotation chain ile).</summary>
    DbSet<RefreshToken> RefreshTokens { get; }

    /// <summary>System operatörü destek oturum kayıtları.</summary>
    DbSet<SupportSession> SupportSessions { get; }

    /// <summary>Bekleyen değişiklikleri persist eder. Cancellation desteği var.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
