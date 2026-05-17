using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using CleanTenant.SharedKernel.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.Infrastructure.IntegrationTests.Catalog;

public sealed class UserRoleAssignmentScopeTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public UserRoleAssignmentScopeTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task System_scope_tenant_id_dolu_olursa_CHECK_constraint_ihlali()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var (userId, roleId) = await EnsureUserAndRoleAsync(scope.ServiceProvider);

        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = userId,
            RoleId = roleId,
            ScopeLevel = ScopeLevel.System,
            TenantId = Guid.NewGuid(), // GEÇERSİZ — System scope'ta TenantId olamaz
            AssignedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        });

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Tenant_scope_company_id_dolu_olursa_CHECK_constraint_ihlali()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var (userId, roleId) = await EnsureUserAndRoleAsync(scope.ServiceProvider);

        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = userId,
            RoleId = roleId,
            ScopeLevel = ScopeLevel.Tenant,
            TenantId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(), // GEÇERSİZ — Tenant scope'ta Company olamaz
            AssignedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        });

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Unit_scope_tum_id_ler_dolu_olmali_eksikse_CHECK_ihlali()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var (userId, roleId) = await EnsureUserAndRoleAsync(scope.ServiceProvider);

        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = userId,
            RoleId = roleId,
            ScopeLevel = ScopeLevel.Unit,
            TenantId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            UnitId = null, // GEÇERSİZ — Unit scope tüm Id'leri ister
            AssignedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        });

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Gecerli_System_atamasi_sorunsuz_kaydedilir()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var (userId, roleId) = await EnsureUserAndRoleAsync(scope.ServiceProvider);

        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = userId,
            RoleId = roleId,
            ScopeLevel = ScopeLevel.System,
            AssignedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        });

        var act = async () => await db.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    private static async Task<(Guid userId, Guid roleId)> EnsureUserAndRoleAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        var db = services.GetRequiredService<CatalogDbContext>();

        var email = $"scope-test-{Guid.NewGuid():N}@example.com";
        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Scope",
            LastName = "Test",
        };
        var createResult = await userManager.CreateAsync(user, "ScopeTest-Pass-2026!");
        createResult.Succeeded.Should().BeTrue();

        var roleName = $"ScopeTestRole-{Guid.NewGuid():N}";
        var role = new Role
        {
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            Scope = ScopeLevel.System,
            Description = "Scope test rolü",
            IsBuiltIn = false,
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        return (user.Id, role.Id);
    }
}
