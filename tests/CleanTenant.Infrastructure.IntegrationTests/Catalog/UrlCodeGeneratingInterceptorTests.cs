using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.Infrastructure.Persistence.Identifiers;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.Infrastructure.IntegrationTests.Catalog;

public sealed class UrlCodeGeneratingInterceptorTests : IClassFixture<PostgresFixture>
{
    private const string Base58Alphabet =
        "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    private readonly PostgresFixture _fixture;

    public UrlCodeGeneratingInterceptorTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IHasUrlCode_entity_Add_edildiginde_UrlCode_otomatik_uretilir()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var tenant = new Tenant
        {
            Name = $"UrlGenTest-{Guid.NewGuid():N}",
            Status = TenantStatus.Active,
            BillingTier = BillingTier.Free,
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        tenant.UrlCode.Should().NotBeNullOrEmpty();
        tenant.UrlCode.Should().HaveLength(9);
        tenant.UrlCode.Should().MatchRegex("^[1-9A-HJ-NP-Za-km-z]{9}$");
    }

    [Fact]
    public async Task UrlCodeRegistry_ye_kayit_eklenir_OwnerType_ve_OwnerId_ile()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var tenant = new Tenant
        {
            Name = $"RegistryTest-{Guid.NewGuid():N}",
            Status = TenantStatus.Active,
            BillingTier = BillingTier.Standard,
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var registryEntry = await db.UrlCodeRegistry
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Code == tenant.UrlCode);

        registryEntry.Should().NotBeNull();
        registryEntry!.OwnerType.Should().Be(nameof(Tenant));
        registryEntry.OwnerId.Should().Be(tenant.Id);
    }

    [Fact]
    public async Task Birden_cok_entity_aynda_eklendiginde_her_birine_farkli_UrlCode_uretilir()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var prefix = Guid.NewGuid().ToString("N")[..8];
        var tenants = new[]
        {
            new Tenant { Name = $"Multi1-{prefix}", Status = TenantStatus.Active, BillingTier = BillingTier.Free },
            new Tenant { Name = $"Multi2-{prefix}", Status = TenantStatus.Active, BillingTier = BillingTier.Standard },
            new Tenant { Name = $"Multi3-{prefix}", Status = TenantStatus.Active, BillingTier = BillingTier.Enterprise },
        };
        db.Tenants.AddRange(tenants);
        await db.SaveChangesAsync();

        var codes = tenants.Select(t => t.UrlCode).ToArray();
        codes.Should().OnlyHaveUniqueItems();
        codes.Should().AllSatisfy(c => c.Should().HaveLength(9));
    }
}
