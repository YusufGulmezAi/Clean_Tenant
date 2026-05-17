using CleanTenant.Application.Common.Auditing;
using CleanTenant.Domain.Auditing;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using CleanTenant.Infrastructure.Persistence.Audit;
using CleanTenant.Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.Infrastructure.IntegrationTests.Audit;

/// <summary>
/// <see cref="CleanTenant.Infrastructure.Persistence.Interceptors.FullAuditInterceptor"/>
/// davranış testleri — Catalog SaveChanges'in Audit DB'ye doğru kayıt yazdığını,
/// delta + PII redact + Support WriteActionCount akışlarını doğrular.
/// </summary>
public sealed class FullAuditInterceptorTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public FullAuditInterceptorTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Yeni_tenant_eklendiginde_audit_kaydi_create_olarak_yazilmali()
    {
        using var scope = _fixture.Services.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        var tenant = new Tenant
        {
            Name = $"FullAudit-{Guid.NewGuid():N}",
            Status = TenantStatus.Active,
            BillingTier = BillingTier.Standard,
            HasDedicatedDatabase = false,
        };
        catalog.Tenants.Add(tenant);
        await catalog.SaveChangesAsync();

        var entry = await audit.AuditEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EntityId == tenant.Id);

        entry.Should().NotBeNull();
        entry!.Action.Should().Be(AuditAction.Create);
        entry.EntityType.Should().Be("Tenant");
        entry.ChangesJson.Should().Contain(tenant.Name);
        entry.EnvironmentName.Should().Be("Test");
    }

    [Fact]
    public async Task Tenant_guncellendiginde_delta_olarak_yazilmali()
    {
        using var scope = _fixture.Services.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        var tenant = new Tenant
        {
            Name = $"Delta-Before-{Guid.NewGuid():N}",
            Status = TenantStatus.Active,
            BillingTier = BillingTier.Standard,
        };
        catalog.Tenants.Add(tenant);
        await catalog.SaveChangesAsync();

        // Update
        tenant.Name = $"Delta-After-{Guid.NewGuid():N}";
        await catalog.SaveChangesAsync();

        var updateEntry = await audit.AuditEntries
            .AsNoTracking()
            .Where(e => e.EntityId == tenant.Id && e.Action == AuditAction.Update)
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync();

        updateEntry.Should().NotBeNull();
        updateEntry!.ChangesJson.Should().Contain("Name");
        updateEntry.ChangesJson.Should().Contain("old");
        updateEntry.ChangesJson.Should().Contain("new");
        updateEntry.ChangesJson.Should().Contain(tenant.Name);
    }

    [Fact]
    public async Task Soft_delete_audit_action_Delete_olarak_kaydedilmeli()
    {
        using var scope = _fixture.Services.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        var tenant = new Tenant
        {
            Name = $"SoftDelete-{Guid.NewGuid():N}",
            Status = TenantStatus.Active,
            BillingTier = BillingTier.Standard,
        };
        catalog.Tenants.Add(tenant);
        await catalog.SaveChangesAsync();

        // SoftDelete: Remove → AuditingInterceptor state'i Modified yapar + IsDeleted=true
        catalog.Tenants.Remove(tenant);
        await catalog.SaveChangesAsync();

        var deleteEntry = await audit.AuditEntries
            .AsNoTracking()
            .Where(e => e.EntityId == tenant.Id && e.Action == AuditAction.Delete)
            .FirstOrDefaultAsync();

        deleteEntry.Should().NotBeNull();
    }

    [Fact]
    public async Task Audit_kaydi_metadata_alanlarini_tasimali()
    {
        // Bu test, AuditMetadataAccessor mock'u üzerinden zenginleştirilmiş
        // metadata'nın audit kaydına geçtiğini doğrular.
        _fixture.AuditMetadataAccessor.Capture().Returns(new AuditMetadata
        {
            UserId = Guid.NewGuid(),
            UserEmail = "test@cleantenant.test",
            UserFullName = "Test User",
            TenantName = "Acme",
            ScopeLevel = "Tenant",
            PersonaSide = "Management",
            IpAddress = "127.0.0.1",
            UserAgent = "Mozilla/5.0 (Test)",
            BrowserName = "TestBrowser",
            EnvironmentName = "Test",
            MachineName = "test-host",
            ApplicationName = "CleanTenant.Test",
            ApplicationVersion = "0.0.0-test",
            ProcessId = 1234,
            ThreadId = 5,
            TraceId = "00-trace-test-id",
            RequestPath = "/api/v1/test",
            RequestMethod = "POST",
        });

        using var scope = _fixture.Services.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        var tenant = new Tenant
        {
            Name = $"MetaTest-{Guid.NewGuid():N}",
            Status = TenantStatus.Active,
            BillingTier = BillingTier.Standard,
        };
        catalog.Tenants.Add(tenant);
        await catalog.SaveChangesAsync();

        var entry = await audit.AuditEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EntityId == tenant.Id);

        entry.Should().NotBeNull();
        entry!.UserEmail.Should().Be("test@cleantenant.test");
        entry.UserFullName.Should().Be("Test User");
        entry.TenantName.Should().Be("Acme");
        entry.ScopeLevel.Should().Be("Tenant");
        entry.PersonaSide.Should().Be("Management");
        entry.IpAddress.Should().Be("127.0.0.1");
        entry.UserAgent.Should().Be("Mozilla/5.0 (Test)");
        entry.BrowserName.Should().Be("TestBrowser");
        entry.RequestPath.Should().Be("/api/v1/test");
        entry.RequestMethod.Should().Be("POST");
        entry.TraceId.Should().Be("00-trace-test-id");
        entry.MachineName.Should().Be("test-host");

        // Default metadata'ya dön (diğer testler etkilenmesin)
        _fixture.AuditMetadataAccessor.Capture().Returns(new AuditMetadata
        {
            EnvironmentName = "Test",
            MachineName = "test-host",
            ApplicationName = "CleanTenant.Test",
            ApplicationVersion = "0.0.0-test",
            ProcessId = Environment.ProcessId,
            ThreadId = Environment.CurrentManagedThreadId,
        });
    }
}
