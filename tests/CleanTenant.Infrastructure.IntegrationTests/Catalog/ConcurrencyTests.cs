using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.Infrastructure.IntegrationTests.Catalog;

public sealed class ConcurrencyTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public ConcurrencyTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Iki_paralel_update_ikincide_DbUpdateConcurrencyException_atar()
    {
        // İlk scope: tenant oluştur
        Guid tenantId;
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var tenant = new Tenant
            {
                Name = $"ConcTest-{Guid.NewGuid():N}",
                Status = TenantStatus.Active,
                BillingTier = BillingTier.Standard,
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
            tenantId = tenant.Id;
        }

        // İki bağımsız scope; her biri aynı tenant'ı çeker
        using var scopeA = _fixture.Services.CreateScope();
        using var scopeB = _fixture.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var fromA = await dbA.Tenants.FirstAsync(t => t.Id == tenantId);
        var fromB = await dbB.Tenants.FirstAsync(t => t.Id == tenantId);

        // A güncellemesini önce kaydeder
        fromA.LegalName = "A tarafından güncellendi";
        await dbA.SaveChangesAsync();

        // B aynı tenant üzerinden güncelleme deneyince concurrency token (xmin) eski olduğu için patlar
        fromB.LegalName = "B tarafından güncellendi";
        var act = async () => await dbB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
