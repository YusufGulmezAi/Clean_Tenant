using CleanTenant.Domain.Tenant.Accruals;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.Domain.Tenant.Companies;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using CleanTenant.Infrastructure.Persistence.Main;
using CleanTenant.SharedKernel.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CleanTenant.Infrastructure.IntegrationTests.Main;

/// <summary>
/// FAZ 6 Slice 10 — Tahakkuk DB-level kısıt testleri: idempotency partial unique
/// (Budget kaynağı için (BudgetId, AccountingPeriodId)), accrual_details
/// (AccrualId, UnitId) unique, ay/amount CHECK constraint'leri.
/// </summary>
public sealed class AccrualConstraintsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public AccrualConstraintsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static Accrual NewBudgetAccrual(Guid tenantId, Guid companyId, Guid budgetId, Guid periodId) => new()
    {
        TenantId = tenantId,
        CompanyId = companyId,
        Source = AccrualSource.Budget,
        BudgetId = budgetId,
        AccountingPeriodId = periodId,
        Year = 2026,
        Month = 2,
        TotalAmount = 1000m,
        Description = "Test Tahakkuk",
        GeneratedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Ayni_Budget_Period_icin_iki_Accrual_idempotency_unique_ihlali()
    {
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        SetTenantContext(tenantId);

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Accruals.Add(NewBudgetAccrual(tenantId, companyId, budgetId, periodId));
            await db.SaveChangesAsync();
        }

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Accruals.Add(NewBudgetAccrual(tenantId, companyId, budgetId, periodId));

            var act = async () => await db.SaveChangesAsync();
            var assertion = await act.Should().ThrowAsync<DbUpdateException>();
            assertion.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be("23505"); // unique_violation
        }
    }

    [Fact]
    public async Task Invoice_kaynagi_idempotency_unique_kapsaminda_degil()
    {
        // İdempotency partial index yalnız source=0 (Budget). Invoice (source=1)
        // için aynı period'a birden çok accrual serbest.
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        SetTenantContext(tenantId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        for (var i = 0; i < 2; i++)
        {
            db.Accruals.Add(new Accrual
            {
                TenantId = tenantId, CompanyId = companyId,
                Source = AccrualSource.Invoice,
                AccountingPeriodId = periodId,
                Year = 2026, Month = 1, TotalAmount = 500m,
                Description = $"Fatura {i}", GeneratedAt = DateTimeOffset.UtcNow,
            });
        }

        var act = async () => await db.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Accrual_gecersiz_ay_check_ihlali()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var accrual = NewBudgetAccrual(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        accrual.Month = 13; // CHECK ihlali

        db.Accruals.Add(accrual);
        var act = async () => await db.SaveChangesAsync();
        var assertion = await act.Should().ThrowAsync<DbUpdateException>();
        assertion.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.ConstraintName.Should().Be("ck_accruals_month");
    }

    [Fact]
    public async Task AccrualDetail_ayni_Accrual_Unit_cifti_unique_ihlali()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        // BB için gerçek bir Company→Land→Parcel→Building→Unit zinciri gerekiyor (FK).
        var (companyId, unitId) = await SeedCompanyWithUnitAsync(tenantId);

        var accrual = NewBudgetAccrual(tenantId, companyId, Guid.NewGuid(), Guid.NewGuid());
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            accrual.Details.Add(new AccrualDetail
            {
                TenantId = tenantId, AccrualId = accrual.Id, UnitId = unitId,
                Amount = 100m, DistributionShare = 1m, DueDate = new DateOnly(2026, 3, 15),
            });
            db.Accruals.Add(accrual);
            await db.SaveChangesAsync();
        }

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.AccrualDetails.Add(new AccrualDetail
            {
                TenantId = tenantId, AccrualId = accrual.Id, UnitId = unitId, // aynı çift
                Amount = 200m, DistributionShare = 1m, DueDate = new DateOnly(2026, 3, 15),
            });
            var act = async () => await db.SaveChangesAsync();
            var assertion = await act.Should().ThrowAsync<DbUpdateException>();
            assertion.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be("23505");
        }
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
