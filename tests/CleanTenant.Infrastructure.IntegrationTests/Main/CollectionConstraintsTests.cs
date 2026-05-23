using CleanTenant.Domain.Tenant.Accruals;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.Domain.Tenant.Collections;
using CleanTenant.Domain.Tenant.Collections.Enums;
using CleanTenant.Domain.Tenant.Companies;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using CleanTenant.Infrastructure.Persistence.Main;
using CleanTenant.SharedKernel.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CleanTenant.Infrastructure.IntegrationTests.Main;

/// <summary>
/// FAZ 7 Slice 6 — Tahsilat DB-level kısıt testleri: collections UrlCode unique,
/// amount/unallocated CHECK; collection_allocations (CollectionId, AccrualDetailId)
/// unique, allocated_amount > 0 CHECK, AccrualDetail FK restrict.
/// </summary>
public sealed class CollectionConstraintsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public CollectionConstraintsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static string NewUrlCode() => Guid.NewGuid().ToString("N")[..9];

    private static Collection NewCollection(
        Guid tenantId, Guid companyId, Guid unitId, string urlCode,
        decimal amount = 500m, decimal unallocated = 0m) => new()
    {
        TenantId = tenantId,
        CompanyId = companyId,
        UnitId = unitId,
        AccountingPeriodId = Guid.NewGuid(),
        UrlCode = urlCode,
        PaymentDate = new DateOnly(2026, 3, 15),
        Amount = amount,
        Method = PaymentMethod.Cash,
        CashAccountCodeId = Guid.NewGuid(),
        UnallocatedAmount = unallocated,
        RecordedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Ayni_UrlCode_iki_Collection_unique_ihlali()
    {
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var urlCode = NewUrlCode();
        SetTenantContext(tenantId);

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Collections.Add(NewCollection(tenantId, companyId, unitId, urlCode));
            await db.SaveChangesAsync();
        }

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Collections.Add(NewCollection(tenantId, companyId, unitId, urlCode));

            var act = async () => await db.SaveChangesAsync();
            var assertion = await act.Should().ThrowAsync<DbUpdateException>();
            assertion.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be("23505"); // unique_violation
        }
    }

    [Fact]
    public async Task Negatif_amount_check_ihlali()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        db.Collections.Add(NewCollection(tenantId, Guid.NewGuid(), Guid.NewGuid(), NewUrlCode(), amount: -1m));

        var act = async () => await db.SaveChangesAsync();
        var assertion = await act.Should().ThrowAsync<DbUpdateException>();
        assertion.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.ConstraintName.Should().Be("ck_collections_amount");
    }

    [Fact]
    public async Task Negatif_unallocated_check_ihlali()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        db.Collections.Add(NewCollection(tenantId, Guid.NewGuid(), Guid.NewGuid(), NewUrlCode(), unallocated: -1m));

        var act = async () => await db.SaveChangesAsync();
        var assertion = await act.Should().ThrowAsync<DbUpdateException>();
        assertion.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.ConstraintName.Should().Be("ck_collections_unallocated");
    }

    [Fact]
    public async Task CollectionAllocation_ayni_Collection_Detail_cifti_unique_ihlali()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        var (companyId, unitId) = await SeedCompanyWithUnitAsync(tenantId);
        var detailId = await SeedAccrualDetailAsync(tenantId, companyId, unitId);

        var collection = NewCollection(tenantId, companyId, unitId, NewUrlCode(), amount: 500m);
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            collection.Allocations.Add(new CollectionAllocation
            {
                TenantId = tenantId, CollectionId = collection.Id,
                AccrualDetailId = detailId, AllocatedAmount = 200m,
            });
            db.Collections.Add(collection);
            await db.SaveChangesAsync();
        }

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.CollectionAllocations.Add(new CollectionAllocation
            {
                TenantId = tenantId, CollectionId = collection.Id,
                AccrualDetailId = detailId, AllocatedAmount = 100m, // aynı çift
            });
            var act = async () => await db.SaveChangesAsync();
            var assertion = await act.Should().ThrowAsync<DbUpdateException>();
            assertion.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be("23505");
        }
    }

    [Fact]
    public async Task CollectionAllocation_sifir_tutar_check_ihlali()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        var (companyId, unitId) = await SeedCompanyWithUnitAsync(tenantId);
        var detailId = await SeedAccrualDetailAsync(tenantId, companyId, unitId);

        var collection = NewCollection(tenantId, companyId, unitId, NewUrlCode());
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        collection.Allocations.Add(new CollectionAllocation
        {
            TenantId = tenantId, CollectionId = collection.Id,
            AccrualDetailId = detailId, AllocatedAmount = 0m, // CHECK ihlali
        });
        db.Collections.Add(collection);

        var act = async () => await db.SaveChangesAsync();
        var assertion = await act.Should().ThrowAsync<DbUpdateException>();
        assertion.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.ConstraintName.Should().Be("ck_collection_alloc_amount");
    }

    [Fact]
    public async Task CollectionAllocation_gecersiz_AccrualDetail_FK_ihlali()
    {
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        SetTenantContext(tenantId);

        var collection = NewCollection(tenantId, companyId, unitId, NewUrlCode());
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Collections.Add(collection);
            await db.SaveChangesAsync();
        }

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.CollectionAllocations.Add(new CollectionAllocation
            {
                TenantId = tenantId, CollectionId = collection.Id,
                AccrualDetailId = Guid.NewGuid(), // var olmayan tahakkuk detayı
                AllocatedAmount = 100m,
            });
            var act = async () => await db.SaveChangesAsync();
            var assertion = await act.Should().ThrowAsync<DbUpdateException>();
            assertion.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be("23503"); // foreign_key_violation
        }
    }

    /// <summary>Test için bir Accrual + AccrualDetail kurar; detay id döner.</summary>
    private async Task<Guid> SeedAccrualDetailAsync(Guid tenantId, Guid companyId, Guid unitId)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var accrual = new Accrual
        {
            TenantId = tenantId, CompanyId = companyId,
            Source = AccrualSource.Budget,
            BudgetId = Guid.NewGuid(), AccountingPeriodId = Guid.NewGuid(),
            Year = 2026, Month = 2, TotalAmount = 500m,
            Description = "Test Tahakkuk", GeneratedAt = DateTimeOffset.UtcNow,
        };
        var detail = new AccrualDetail
        {
            TenantId = tenantId, AccrualId = accrual.Id, UnitId = unitId,
            Amount = 500m, DistributionShare = 1m, DueDate = new DateOnly(2026, 3, 15),
        };
        accrual.Details.Add(detail);
        db.Accruals.Add(accrual);
        await db.SaveChangesAsync();
        return detail.Id;
    }

    /// <summary>Test için minimal Company→Land→Parcel→Building→Unit zinciri kurar; (companyId, unitId) döner.</summary>
    private async Task<(Guid CompanyId, Guid UnitId)> SeedCompanyWithUnitAsync(Guid tenantId)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var company = new Company { TenantId = tenantId, Name = $"Site-{Guid.NewGuid():N}", Status = CompanyStatus.Active };
        db.Companies.Add(company);
        var land = new Land { TenantId = tenantId, CompanyId = company.Id, Name = $"Ada-{Guid.NewGuid():N}", SortOrder = 0 };
        db.Lands.Add(land);
        var parcel = new Parcel { TenantId = tenantId, LandId = land.Id, Name = $"Parsel-{Guid.NewGuid():N}", SortOrder = 0 };
        db.Parcels.Add(parcel);
        var building = new Building { TenantId = tenantId, ParcelId = parcel.Id, Name = "A Blok", SortOrder = 0 };
        db.Buildings.Add(building);
        var unit = new Unit
        {
            TenantId = tenantId, BuildingId = building.Id, Number = "1",
            GrossSquareMeters = 100m, SortOrder = 0,
        };
        db.Units.Add(unit);
        await db.SaveChangesAsync();
        return (company.Id, unit.Id);
    }

    private void SetTenantContext(Guid tenantId)
    {
        _fixture.TenantContext.TenantId.Returns(tenantId);
        _fixture.TenantContext.CompanyId.Returns((Guid?)null);
        _fixture.TenantContext.UnitId.Returns((Guid?)null);
        _fixture.TenantContext.CurrentScope.Returns(ScopeLevel.Tenant);
    }
}
