using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.Infrastructure.Persistence.Seeding;
using CleanTenant.SharedKernel.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CleanTenant.Infrastructure.IntegrationTests.Catalog;

/// <summary>
/// Built-in rol seed davranışı (karar 2026-05-24) + <see cref="ScopePermissionResolver"/>
/// cascade kuralı entegrasyon testleri. Gerçek Postgres (Testcontainers) üzerinde çalışır.
/// <para>
/// Yeni rol modeli: <b>SystemAdmin = tam erişim</b> (tüm izinler, auto-grow);
/// <b>TenantAdmin / CompanyAdmin = seed ile izin almaz</b> (boş başlar; SystemAdmin
/// Rol Yönetimi ekranından düzenler). Cascade testleri built-in rolleri kirletmemek
/// için kendi izole (custom) rollerini oluşturur.
/// </para>
/// </summary>
public sealed class SuperAdminRolesAndCascadeTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public SuperAdminRolesAndCascadeTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Seed_SystemAdmin_katalogdaki_tum_izinleri_alir()
    {
        using var scope = _fixture.Services.CreateScope();
        await EnsureCoreSeededAsync(scope);
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var systemAdminPerms = await PermissionCodesForRoleAsync(db, "SYSTEMADMIN", ScopeLevel.System);

        var allPerms = await db.Permissions.AsNoTracking()
            .Select(p => p.Code)
            .ToHashSetAsync();

        systemAdminPerms.Should().NotBeEmpty();
        systemAdminPerms.Should().BeEquivalentTo(allPerms,
            "SystemAdmin tam erişim rolüdür; katalogdaki tüm izinleri almalı");
    }

    [Fact]
    public async Task Seed_TenantAdmin_ve_CompanyAdmin_izinsiz_baslar()
    {
        using var scope = _fixture.Services.CreateScope();
        await EnsureCoreSeededAsync(scope);
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var tenantAdminPerms = await PermissionCodesForRoleAsync(db, "TENANTADMIN", ScopeLevel.Tenant);
        var companyAdminPerms = await PermissionCodesForRoleAsync(db, "COMPANYADMIN", ScopeLevel.Company);

        tenantAdminPerms.Should().BeEmpty(
            "TenantAdmin'e seed ile izin atanmaz; izinleri SystemAdmin elle verir");
        companyAdminPerms.Should().BeEmpty(
            "CompanyAdmin'e seed ile izin atanmaz; izinleri SystemAdmin elle verir");
    }

    [Fact]
    public async Task Seed_idempotent_ikinci_calistirmada_yeni_atama_yapmaz()
    {
        using var scope = _fixture.Services.CreateScope();
        await EnsureCoreSeededAsync(scope);
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var before = (await PermissionCodesForRoleAsync(db, "SYSTEMADMIN", ScopeLevel.System)).Count;

        // İkinci seed çağrısı — idempotent olmalı.
        await EnsureCoreSeededAsync(scope);

        var after = (await PermissionCodesForRoleAsync(db, "SYSTEMADMIN", ScopeLevel.System)).Count;

        after.Should().Be(before);
    }

    [Fact]
    public async Task Cascade_tenant_scope_rol_company_baglaminda_izinlerini_kazanir()
    {
        using var scope = _fixture.Services.CreateScope();
        await EnsureCoreSeededAsync(scope);
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        // İzole Tenant-scope rol; üzerine bir Tenant- ve bir Company-scope izin atanır.
        var roleId = await CreateCustomRoleAsync(roleManager, ScopeLevel.Tenant);
        var tenantPermId = await FirstPermissionIdAsync(db, ScopeLevel.Tenant);
        var companyPermId = await FirstPermissionIdAsync(db, ScopeLevel.Company);
        await GrantPermissionsToRoleAsync(db, roleId, [tenantPermId, companyPermId]);

        // Yalnız Tenant-scope ataması olan kullanıcı (Company ataması YOK).
        var user = await CreateUserAsync(userManager);
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = roleId,
            ScopeLevel = ScopeLevel.Tenant,
            TenantId = tenantId,
            AssignedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        });
        await db.SaveChangesAsync();

        var resolver = new ScopePermissionResolver(db);
        var (_, permissions) = await resolver.ResolveAsync(
            user.Id, ScopeLevel.Company, tenantId, companyId, null, CancellationToken.None);

        // Company bağlamında çözüm, parent Tenant atamasını cascade ile katar:
        // rolün TÜM izinleri (Tenant + Company) gelir.
        var expected = await PermissionCodesForRoleIdAsync(db, roleId);
        permissions.Should().BeEquivalentTo(expected,
            "cascade: Tenant-scope rol bir sitede tam yetkili olmalı");

        var companyPermCode = await db.Permissions.AsNoTracking()
            .Where(p => p.Id == companyPermId).Select(p => p.Code).FirstAsync();
        permissions.Should().Contain(companyPermCode);
    }

    [Fact]
    public async Task Cascade_yok_sadece_company_atamasi_olan_kullanici_tenant_iznini_almaz()
    {
        using var scope = _fixture.Services.CreateScope();
        await EnsureCoreSeededAsync(scope);
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        // İzole Company-scope rol; üzerine bir Company-scope izin atanır.
        var roleId = await CreateCustomRoleAsync(roleManager, ScopeLevel.Company);
        var companyPermId = await FirstPermissionIdAsync(db, ScopeLevel.Company);
        await GrantPermissionsToRoleAsync(db, roleId, [companyPermId]);

        // Yalnız Company-scope ataması olan kullanıcı (Tenant ataması YOK).
        var user = await CreateUserAsync(userManager);
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = roleId,
            ScopeLevel = ScopeLevel.Company,
            TenantId = tenantId,
            CompanyId = companyId,
            AssignedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        });
        await db.SaveChangesAsync();

        var resolver = new ScopePermissionResolver(db);
        var (_, permissions) = await resolver.ResolveAsync(
            user.Id, ScopeLevel.Company, tenantId, companyId, null, CancellationToken.None);

        var expected = await PermissionCodesForRoleIdAsync(db, roleId);
        permissions.Should().BeEquivalentTo(expected);

        // Tenant-only bir izin yukarı doğru SIZMAMALI.
        var aTenantOnlyPerm = await db.Permissions.AsNoTracking()
            .Where(p => p.MinimumRoleScope == ScopeLevel.Tenant)
            .Select(p => p.Code).FirstAsync();
        permissions.Should().NotContain(aTenantOnlyPerm,
            "cascade yalnız aşağı yönlü; company kullanıcısı tenant yetkisi almaz");
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private static async Task EnsureCoreSeededAsync(IServiceScope scope)
    {
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var seeder = new CatalogSeeder(db, roleManager, NullLogger<CatalogSeeder>.Instance);
        await seeder.SeedCoreCatalogAsync();
    }

    private static async Task<Guid> RoleIdAsync(CatalogDbContext db, string normalizedName, ScopeLevel scope)
        => await db.Roles.AsNoTracking()
            .Where(r => r.NormalizedName == normalizedName && r.Scope == scope)
            .Select(r => r.Id)
            .FirstAsync();

    private static async Task<HashSet<string>> PermissionCodesForRoleAsync(
        CatalogDbContext db, string normalizedName, ScopeLevel scope)
    {
        var roleId = await RoleIdAsync(db, normalizedName, scope);
        return await PermissionCodesForRoleIdAsync(db, roleId);
    }

    private static async Task<HashSet<string>> PermissionCodesForRoleIdAsync(CatalogDbContext db, Guid roleId)
        => await db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Join(db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.Code)
            .ToHashSetAsync();

    private static async Task<Guid> FirstPermissionIdAsync(CatalogDbContext db, ScopeLevel minimumRoleScope)
        => await db.Permissions.AsNoTracking()
            .Where(p => p.MinimumRoleScope == minimumRoleScope)
            .Select(p => p.Id)
            .FirstAsync();

    private static async Task<Guid> CreateCustomRoleAsync(RoleManager<Role> roleManager, ScopeLevel scope)
    {
        var name = $"CascadeTest-{scope}-{Guid.NewGuid():N}";
        var role = new Role
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Description = "Cascade testi için geçici rol",
            Scope = scope,
            IsBuiltIn = false,
        };
        var result = await roleManager.CreateAsync(role);
        result.Succeeded.Should().BeTrue();
        return role.Id;
    }

    private static async Task GrantPermissionsToRoleAsync(
        CatalogDbContext db, Guid roleId, IReadOnlyCollection<Guid> permissionIds)
    {
        foreach (var permissionId in permissionIds)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedBy = null,
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task<User> CreateUserAsync(UserManager<User> userManager)
    {
        var email = $"superadmin-test-{Guid.NewGuid():N}@example.com";
        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
        };
        var result = await userManager.CreateAsync(user, "SuperAdmin-Pass-2026!");
        result.Succeeded.Should().BeTrue();
        return user;
    }
}
