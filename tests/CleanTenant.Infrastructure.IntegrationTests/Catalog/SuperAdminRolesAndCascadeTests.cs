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
/// v0.2.13.e — Süper yetkili roller (TenantAdmin/CompanyAdmin) seed davranışı +
/// <see cref="ScopePermissionResolver"/> cascade kuralı entegrasyon testleri.
/// Gerçek Postgres (Testcontainers) üzerinde çalışır.
/// </summary>
public sealed class SuperAdminRolesAndCascadeTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public SuperAdminRolesAndCascadeTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Seed_TenantAdmin_tum_tenant_ve_company_izinlerini_alir()
    {
        using var scope = _fixture.Services.CreateScope();
        await EnsureCoreSeededAsync(scope);
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var tenantAdminPerms = await PermissionCodesForRoleAsync(db, "TENANTADMIN", ScopeLevel.Tenant);

        var expected = await db.Permissions.AsNoTracking()
            .Where(p => p.MinimumRoleScope == ScopeLevel.Tenant || p.MinimumRoleScope == ScopeLevel.Company)
            .Select(p => p.Code)
            .ToHashSetAsync();

        tenantAdminPerms.Should().NotBeEmpty();
        tenantAdminPerms.Should().BeEquivalentTo(expected,
            "TenantAdmin tüm Tenant- ve Company-scope izinlerini almalı (System/Unit hariç)");
    }

    [Fact]
    public async Task Seed_CompanyAdmin_yalniz_company_izinlerini_alir()
    {
        using var scope = _fixture.Services.CreateScope();
        await EnsureCoreSeededAsync(scope);
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var companyAdminPerms = await PermissionCodesForRoleAsync(db, "COMPANYADMIN", ScopeLevel.Company);

        var expected = await db.Permissions.AsNoTracking()
            .Where(p => p.MinimumRoleScope == ScopeLevel.Company)
            .Select(p => p.Code)
            .ToHashSetAsync();

        companyAdminPerms.Should().NotBeEmpty();
        companyAdminPerms.Should().BeEquivalentTo(expected,
            "CompanyAdmin yalnız Company-scope izinlerini almalı");
    }

    [Fact]
    public async Task Seed_idempotent_ikinci_calistirmada_yeni_atama_yapmaz()
    {
        using var scope = _fixture.Services.CreateScope();
        await EnsureCoreSeededAsync(scope);
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var before = (await PermissionCodesForRoleAsync(db, "TENANTADMIN", ScopeLevel.Tenant)).Count;

        // İkinci seed çağrısı — idempotent olmalı.
        await EnsureCoreSeededAsync(scope);

        var after = (await PermissionCodesForRoleAsync(db, "TENANTADMIN", ScopeLevel.Tenant)).Count;

        after.Should().Be(before);
    }

    [Fact]
    public async Task Cascade_TenantAdmin_company_baglaminda_company_izinlerini_kazanir()
    {
        using var scope = _fixture.Services.CreateScope();
        await EnsureCoreSeededAsync(scope);
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        // Yalnız Tenant-scope TenantAdmin ataması olan kullanıcı (Company ataması YOK).
        var user = await CreateUserAsync(userManager);
        var tenantAdminRoleId = await RoleIdAsync(db, "TENANTADMIN", ScopeLevel.Tenant);
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = tenantAdminRoleId,
            ScopeLevel = ScopeLevel.Tenant,
            TenantId = tenantId,
            AssignedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        });
        await db.SaveChangesAsync();

        var resolver = new ScopePermissionResolver(db);
        var (roles, permissions) = await resolver.ResolveAsync(
            user.Id, ScopeLevel.Company, tenantId, companyId, null, CancellationToken.None);

        // Company bağlamında çözüm, parent Tenant atamasını (TenantAdmin) cascade ile katar.
        var tenantAdminPerms = await PermissionCodesForRoleAsync(db, "TENANTADMIN", ScopeLevel.Tenant);
        permissions.Should().BeEquivalentTo(tenantAdminPerms,
            "cascade: TenantAdmin bir sitede tam yetkili olmalı");
        roles.Should().Contain("TenantAdmin");

        var aCompanyPerm = await db.Permissions.AsNoTracking()
            .Where(p => p.MinimumRoleScope == ScopeLevel.Company)
            .Select(p => p.Code).FirstAsync();
        permissions.Should().Contain(aCompanyPerm);
    }

    [Fact]
    public async Task Cascade_yok_sadece_company_atamasi_olan_kullanici_tenant_iznini_almaz()
    {
        using var scope = _fixture.Services.CreateScope();
        await EnsureCoreSeededAsync(scope);
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        // Yalnız Company-scope CompanyAdmin ataması olan kullanıcı (Tenant ataması YOK).
        var user = await CreateUserAsync(userManager);
        var companyAdminRoleId = await RoleIdAsync(db, "COMPANYADMIN", ScopeLevel.Company);
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = companyAdminRoleId,
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

        var companyAdminPerms = await PermissionCodesForRoleAsync(db, "COMPANYADMIN", ScopeLevel.Company);
        permissions.Should().BeEquivalentTo(companyAdminPerms);

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
        return await db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Join(db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.Code)
            .ToHashSetAsync();
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
