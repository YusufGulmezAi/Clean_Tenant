using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.Infrastructure.IntegrationTests.Catalog;

public sealed class AuditingInterceptorTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public AuditingInterceptorTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Add_yapildiginda_CreatedAt_otomatik_doldurulur()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var tenant = new Tenant
        {
            Name = $"AuditTest-{Guid.NewGuid():N}",
            Status = TenantStatus.Active,
            BillingTier = BillingTier.Standard,
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var stored = await db.Tenants.AsNoTracking().FirstAsync(t => t.Id == tenant.Id);
        stored.CreatedAt.Should().NotBe(default(DateTimeOffset));
        stored.UpdatedAt.Should().BeNull();
        stored.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Id_bos_birakildiginda_UUID_v7_otomatik_atanir()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var tenant = new Tenant
        {
            Name = $"IdTest-{Guid.NewGuid():N}",
            Status = TenantStatus.Active,
            BillingTier = BillingTier.Free,
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        tenant.Id.Should().NotBe(Guid.Empty);
        // UUID v7'nin version field'ı 7 olmalı: byte[6] >> 4 == 7
        var bytes = tenant.Id.ToByteArray();
        var versionNibble = (bytes[7] & 0xF0) >> 4;
        versionNibble.Should().Be(7);
    }

    [Fact]
    public async Task Update_yapildiginda_UpdatedAt_doldurulur()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var tenant = new Tenant
        {
            Name = $"UpdateTest-{Guid.NewGuid():N}",
            Status = TenantStatus.Active,
            BillingTier = BillingTier.Standard,
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        // Yeni scope ile çek + güncelle
        using var scope2 = _fixture.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var loaded = await db2.Tenants.FirstAsync(t => t.Id == tenant.Id);
        loaded.LegalName = "Yeni Yasal Ad";
        await db2.SaveChangesAsync();

        loaded.UpdatedAt.Should().NotBeNull();
        loaded.UpdatedAt!.Value.Should().BeAfter(loaded.CreatedAt);
    }

    [Fact]
    public async Task Delete_yapildiginda_soft_delete_olur_DeletedAt_doldurulur()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var tenant = new Tenant
        {
            Name = $"SoftDelTest-{Guid.NewGuid():N}",
            Status = TenantStatus.Active,
            BillingTier = BillingTier.Free,
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        // Sil
        db.Tenants.Remove(tenant);
        await db.SaveChangesAsync();

        // Default query (soft-delete filter ile) görmez
        var visible = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenant.Id);
        visible.Should().BeNull();

        // IgnoreQueryFilters ile görünür ve IsDeleted = true
        var hidden = await db.Tenants.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenant.Id);
        hidden.Should().NotBeNull();
        hidden!.IsDeleted.Should().BeTrue();
        hidden.DeletedAt.Should().NotBeNull();
    }
}
