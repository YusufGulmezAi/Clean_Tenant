using CleanTenant.Domain.Budgeting;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Support;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.Domain.Localization;
using CleanTenant.Domain.LookUp;
using CleanTenant.Domain.LookUp.Banks;
using CleanTenant.Domain.LookUp.BuildingTypes;
using CleanTenant.Domain.LookUp.Districts;
using CleanTenant.Domain.LookUp.Neighborhoods;
using CleanTenant.Domain.LookUp.Provinces;
using CleanTenant.Domain.LookUp.ResidentialTypes;
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

    /// <summary>LookUp: İl (Province) referans verisi.</summary>
    DbSet<Province> Provinces { get; }

    /// <summary>LookUp: İlçe (District) referans verisi.</summary>
    DbSet<District> Districts { get; }

    /// <summary>LookUp: Mahalle (Neighborhood) referans verisi.</summary>
    DbSet<Neighborhood> Neighborhoods { get; }

    /// <summary>LookUp: Mesken tipi referans verisi (Daire, Ofis, Dükkan vb.).</summary>
    DbSet<ResidentialType> ResidentialTypes { get; }

    /// <summary>LookUp: Yapı tipi referans verisi (Apartman, AVM vb.).</summary>
    DbSet<BuildingType> BuildingTypes { get; }

    /// <summary>LookUp: Banka referans verisi (EFT, sanal POS, tahsilat entegrasyonları).</summary>
    DbSet<Bank> Banks { get; }

    /// <summary>Lokalizasyon kaynakları (Key + Culture composite unique).</summary>
    DbSet<LocalizedResource> LocalizedResources { get; }

    // ── Muhasebe Modülü — Catalog (sistem geneli referans) ───────────────────
    /// <summary>TDHP hesap planı şablonu — yeni şirket oluşturulurken klonlanır.</summary>
    DbSet<ChartOfAccountsTemplate> ChartOfAccountsTemplates { get; }

    /// <summary>TÜİK enflasyon endeksleri (TMS 29 enflasyon muhasebesi için).</summary>
    DbSet<InflationIndex> InflationIndexes { get; }

    /// <summary>Bütçe tipi sistem kataloğu (Aidat/Yatırım/Kömür/Kuruluş base hesap kodları).</summary>
    DbSet<BudgetTypeMetadata> BudgetTypeMetadata { get; }

    /// <summary>Bekleyen değişiklikleri persist eder. Cancellation desteği var.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
