using CleanTenant.Domain.Auditing;
using CleanTenant.Domain.Tenant.Companies;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using CleanTenant.Infrastructure.Persistence.Audit;
using CleanTenant.Infrastructure.Persistence.Main;
using CleanTenant.SharedKernel.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CleanTenant.Infrastructure.IntegrationTests.Main;

/// <summary>
/// <para>
/// v0.2.3.a — MainDbContext + Company entity persistence + global query filter
/// + audit zinciri davranış testleri. PostgresFixture üç DB ile gelir
/// (catalog/audit/main) ve <see cref="PostgresFixture.TenantContext"/> mock'u
/// üzerinden test başına tenant kimliği set edilir.
/// </para>
/// </summary>
public sealed class MainDbContextTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public MainDbContextTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Company_eklendiginde_UrlCode_otomatik_uretilir()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var company = new Company
        {
            TenantId = tenantId,
            Name = $"UrlGen-{Guid.NewGuid():N}",
            Status = CompanyStatus.Active,
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        company.UrlCode.Should().NotBeNullOrEmpty();
        company.UrlCode.Should().HaveLength(9);
        company.UrlCode.Should().MatchRegex("^[1-9A-HJ-NP-Za-km-z]{9}$");
    }

    [Fact]
    public async Task Global_query_filter_aktif_tenant_disindaki_companies_i_gizlemeli()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Tenant A altında bir Company yaz
        SetTenantContext(tenantA);
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Companies.Add(new Company
            {
                TenantId = tenantA,
                Name = $"FilterA-{Guid.NewGuid():N}",
                Status = CompanyStatus.Active,
            });
            await db.SaveChangesAsync();
        }

        // Tenant B altında bir Company yaz
        SetTenantContext(tenantB);
        Guid tenantBCompanyId;
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var companyB = new Company
            {
                TenantId = tenantB,
                Name = $"FilterB-{Guid.NewGuid():N}",
                Status = CompanyStatus.Active,
            };
            db.Companies.Add(companyB);
            await db.SaveChangesAsync();
            tenantBCompanyId = companyB.Id;
        }

        // Tenant A bağlamında sorgu → yalnız A'nın company'leri görülür
        SetTenantContext(tenantA);
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var allFromA = await db.Companies.AsNoTracking().ToListAsync();

            allFromA.Should().NotBeEmpty();
            allFromA.Should().OnlyContain(c => c.TenantId == tenantA);
            allFromA.Should().NotContain(c => c.Id == tenantBCompanyId,
                "global query filter aktif tenant dışındaki kayıtları gizlemeli");
        }
    }

    [Fact]
    public async Task IgnoreQueryFilters_tum_tenantlarin_companies_ini_dondurmeli()
    {
        // Cross-tenant erişim System scope için bilinçli — IgnoreQueryFilters()
        // global filter'ı kapatır. Bu test, filter'ın bypass edilebilirliğini doğrular.
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        SetTenantContext(tenantA);
        Guid companyAId;
        Guid companyBId;
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var companyA = new Company { TenantId = tenantA, Name = $"BypassA-{Guid.NewGuid():N}", Status = CompanyStatus.Active };
            var companyB = new Company { TenantId = tenantB, Name = $"BypassB-{Guid.NewGuid():N}", Status = CompanyStatus.Active };
            db.Companies.AddRange(companyA, companyB);
            await db.SaveChangesAsync();
            companyAId = companyA.Id;
            companyBId = companyB.Id;
        }

        // Hangi tenant context aktif olursa olsun IgnoreQueryFilters her ikisini de getirir.
        SetTenantContext(Guid.NewGuid()); // tamamen farklı bir tenant
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var ids = await db.Companies.IgnoreQueryFilters().AsNoTracking()
                .Where(c => c.Id == companyAId || c.Id == companyBId)
                .Select(c => c.Id)
                .ToListAsync();

            ids.Should().HaveCount(2);
            ids.Should().Contain(companyAId);
            ids.Should().Contain(companyBId);
        }
    }

    [Fact]
    public async Task Vkn_CHECK_constraint_gecersiz_format_icin_ihlal_edilmeli()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var company = new Company
        {
            TenantId = tenantId,
            Name = $"InvalidVkn-{Guid.NewGuid():N}",
            Vkn = "0123456789", // 0 ile başlayan VKN regex ihlali ([1-9][0-9]{9})
            Status = CompanyStatus.Active,
        };
        db.Companies.Add(company);

        var act = async () => await db.SaveChangesAsync();
        var assertion = await act.Should().ThrowAsync<DbUpdateException>();
        assertion.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.ConstraintName.Should().Be("ck_company_vkn_format");
    }

    [Fact]
    public async Task Ayni_tenant_icinde_ayni_Name_unique_index_ile_engellenmeli()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        var duplicateName = $"DupName-{Guid.NewGuid():N}";

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Companies.Add(new Company
            {
                TenantId = tenantId,
                Name = duplicateName,
                Status = CompanyStatus.Active,
            });
            await db.SaveChangesAsync();
        }

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Companies.Add(new Company
            {
                TenantId = tenantId,
                Name = duplicateName,
                Status = CompanyStatus.Active,
            });

            var act = async () => await db.SaveChangesAsync();
            var assertion = await act.Should().ThrowAsync<DbUpdateException>();
            assertion.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be("23505"); // unique_violation
        }
    }

    [Fact]
    public async Task Company_yaratildiginda_Audit_DB_ye_create_kaydi_yazilmali()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        Guid companyId;
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var company = new Company
            {
                TenantId = tenantId,
                Name = $"AuditCreate-{Guid.NewGuid():N}",
                Status = CompanyStatus.Active,
            };
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            companyId = company.Id;
        }

        using (var scope = _fixture.Services.CreateScope())
        {
            var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            var entry = await audit.AuditEntries.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EntityId == companyId);

            entry.Should().NotBeNull();
            entry!.Action.Should().Be(AuditAction.Create);
            entry.EntityType.Should().Be("Company");
        }
    }

    private void SetTenantContext(Guid tenantId)
    {
        _fixture.TenantContext.TenantId.Returns(tenantId);
        _fixture.TenantContext.CompanyId.Returns((Guid?)null);
        _fixture.TenantContext.UnitId.Returns((Guid?)null);
        _fixture.TenantContext.CurrentScope.Returns(ScopeLevel.Tenant);
    }
}
