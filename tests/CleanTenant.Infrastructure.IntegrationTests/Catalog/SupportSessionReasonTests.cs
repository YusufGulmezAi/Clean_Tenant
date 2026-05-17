using CleanTenant.Domain.Identity.Support;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.Infrastructure.IntegrationTests.Catalog;

public sealed class SupportSessionReasonTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public SupportSessionReasonTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Reason_yirmiden_kisa_olunca_CHECK_constraint_ihlali()
    {
        using var scope = _fixture.Services.CreateScope();
        var (db, userId, tenantId) = await SetupAsync(scope.ServiceProvider);

        db.SupportSessions.Add(new SupportSession
        {
            OperatorUserId = userId,
            TargetTenantId = tenantId,
            Mode = SupportSessionMode.ReadOnly,
            Reason = "Çok kısa", // 8 karakter < 20
            StartedAt = DateTimeOffset.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "test",
        });

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Reason_yirmi_veya_uzeri_olunca_sorunsuz_kaydedilir()
    {
        using var scope = _fixture.Services.CreateScope();
        var (db, userId, tenantId) = await SetupAsync(scope.ServiceProvider);

        db.SupportSessions.Add(new SupportSession
        {
            OperatorUserId = userId,
            TargetTenantId = tenantId,
            Mode = SupportSessionMode.ReadOnly,
            Reason = "Ticket #1234 — kullanıcı fatura görme sorunu inceleniyor",
            StartedAt = DateTimeOffset.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "test",
        });

        var act = async () => await db.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    private static async Task<(CatalogDbContext db, Guid userId, Guid tenantId)> SetupAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<CatalogDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        var email = $"support-reason-{Guid.NewGuid():N}@example.com";
        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Support",
            LastName = "Operator",
        };
        var createResult = await userManager.CreateAsync(user, "SupportTest-Pass-2026!");
        createResult.Succeeded.Should().BeTrue();

        var tenant = new Tenant
        {
            Name = $"SupportTarget-{Guid.NewGuid():N}",
            Status = TenantStatus.Active,
            BillingTier = BillingTier.Standard,
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        return (db, user.Id, tenant.Id);
    }
}
