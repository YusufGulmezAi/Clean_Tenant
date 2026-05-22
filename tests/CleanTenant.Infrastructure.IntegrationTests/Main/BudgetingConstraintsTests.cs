using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using CleanTenant.Infrastructure.Persistence.Main;
using CleanTenant.SharedKernel.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CleanTenant.Infrastructure.IntegrationTests.Main;

/// <summary>
/// FAZ 5 Slice 4e — Bütçe modülü DB-level kısıt testleri (unique index'ler +
/// CHECK constraint'ler). Handler iş kuralları unit test'lerde, burada sadece
/// veritabanı tarafının doğru kurulduğu doğrulanır.
/// </summary>
public sealed class BudgetingConstraintsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public BudgetingConstraintsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Ayni_Company_FiscalYear_Type_Title_icin_iki_Budget_unique_ihlali()
    {
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var fiscalYearId = Guid.NewGuid();
        SetTenantContext(tenantId);

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Budgets.Add(new Budget
            {
                TenantId = tenantId,
                CompanyId = companyId,
                FiscalYearId = fiscalYearId,
                Type = BudgetType.Aidat,
                Title = "2026 Ana Aidat",
                PeriodStartYear = 2026, PeriodStartMonth = 1, PeriodEndYear = 2026, PeriodEndMonth = 12,
                Status = BudgetStatus.Draft
            });
            await db.SaveChangesAsync();
        }

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            // Aynı (Company, FiscalYear, Type, Title) → unique ihlali
            db.Budgets.Add(new Budget
            {
                TenantId = tenantId,
                CompanyId = companyId,
                FiscalYearId = fiscalYearId,
                Type = BudgetType.Aidat,
                Title = "2026 Ana Aidat",
                PeriodStartYear = 2026, PeriodStartMonth = 1, PeriodEndYear = 2026, PeriodEndMonth = 12,
                Status = BudgetStatus.Draft
            });

            var act = async () => await db.SaveChangesAsync();
            var assertion = await act.Should().ThrowAsync<DbUpdateException>();
            assertion.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be("23505"); // unique_violation
        }
    }

    [Fact]
    public async Task Ayni_Company_FiscalYear_Type_farkli_Title_iki_Budget_kabul_edilir()
    {
        // v0.2.14 — Aynı yıl + tipte FARKLI isimle birden fazla bütçe olabilir (ek aidat).
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var fiscalYearId = Guid.NewGuid();
        SetTenantContext(tenantId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        db.Budgets.Add(new Budget
        {
            TenantId = tenantId, CompanyId = companyId, FiscalYearId = fiscalYearId,
            Type = BudgetType.Aidat, Title = "2026 Ana Aidat", Status = BudgetStatus.Draft,
            PeriodStartYear = 2026, PeriodStartMonth = 1, PeriodEndYear = 2026, PeriodEndMonth = 12
        });
        db.Budgets.Add(new Budget
        {
            TenantId = tenantId, CompanyId = companyId, FiscalYearId = fiscalYearId,
            Type = BudgetType.Aidat, Title = "2026 Mart Ek Aidat", Status = BudgetStatus.Draft,
            PeriodStartYear = 2026, PeriodStartMonth = 3, PeriodEndYear = 2026, PeriodEndMonth = 12
        });

        var act = async () => await db.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BudgetVersion_publish_consistency_NULL_published_at_AND_NOT_NULL_valid_from_ihlal()
    {
        // CHECK: (published_at IS NULL AND valid_from IS NULL) OR (published_at IS NOT NULL AND valid_from IS NOT NULL)
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var budget = new Budget
        {
            TenantId = tenantId,
            CompanyId = Guid.NewGuid(),
            FiscalYearId = Guid.NewGuid(),
            Title = "Tutarsız",
            PeriodStartYear = 2026, PeriodStartMonth = 1, PeriodEndYear = 2026, PeriodEndMonth = 12,
            Status = BudgetStatus.Draft
        };
        db.Budgets.Add(budget);

        // PublishedAt null, ValidFrom dolu → consistency ihlali
        var bv = new BudgetVersion
        {
            TenantId = tenantId,
            BudgetId = budget.Id,
            VersionNumber = 1,
            ValidFrom = new DateOnly(2026, 1, 1),
            PublishedAt = null
        };
        db.BudgetVersions.Add(bv);

        var act = async () => await db.SaveChangesAsync();
        var assertion = await act.Should().ThrowAsync<DbUpdateException>();
        assertion.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.ConstraintName.Should().Be("ck_budget_version_publish_consistency");
    }

    [Fact]
    public async Task BudgetVersion_ValidTo_ValidFromdan_kucukse_check_ihlali()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var budget = new Budget
        {
            TenantId = tenantId,
            CompanyId = Guid.NewGuid(),
            FiscalYearId = Guid.NewGuid(),
            Title = "Tarihler ters",
            PeriodStartYear = 2026, PeriodStartMonth = 1, PeriodEndYear = 2026, PeriodEndMonth = 12,
            Status = BudgetStatus.Published
        };
        db.Budgets.Add(budget);

        var bv = new BudgetVersion
        {
            TenantId = tenantId,
            BudgetId = budget.Id,
            VersionNumber = 1,
            ValidFrom = new DateOnly(2026, 6, 1),
            ValidTo = new DateOnly(2026, 5, 31), // < ValidFrom
            PublishedAt = DateTimeOffset.UtcNow
        };
        db.BudgetVersions.Add(bv);

        var act = async () => await db.SaveChangesAsync();
        var assertion = await act.Should().ThrowAsync<DbUpdateException>();
        assertion.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.ConstraintName.Should().Be("ck_budget_version_dates");
    }

    [Fact]
    public async Task BudgetLineVersion_Negatif_PlannedAmount_check_ihlali()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        // Önce gerekli parent kayıtları yaz (Budget + BudgetVersion + ExpenseCategory + BudgetLine)
        var budget = new Budget
        {
            TenantId = tenantId,
            CompanyId = Guid.NewGuid(),
            FiscalYearId = Guid.NewGuid(),
            Title = "Negatif amount",
            PeriodStartYear = 2026, PeriodStartMonth = 1, PeriodEndYear = 2026, PeriodEndMonth = 12,
            Status = BudgetStatus.Draft
        };
        var bv = new BudgetVersion
        {
            TenantId = tenantId,
            BudgetId = budget.Id,
            VersionNumber = 1,
            ValidFrom = null,
            PublishedAt = null
        };
        var category = new ExpenseCategory
        {
            TenantId = tenantId,
            CompanyId = budget.CompanyId,
            Code = "TEST",
            Name = "Test Kategori",
            DisplayOrder = 0
        };
        var line = new BudgetLine
        {
            TenantId = tenantId,
            CompanyId = budget.CompanyId,
            ExpenseCategoryId = category.Id,
            Code = "TST-01",
            Name = "Test Kalem",
            IsActive = true,
            DisplayOrder = 0
        };

        db.Budgets.Add(budget);
        db.BudgetVersions.Add(bv);
        db.ExpenseCategories.Add(category);
        db.BudgetLines.Add(line);
        await db.SaveChangesAsync();

        var lv = new BudgetLineVersion
        {
            TenantId = tenantId,
            BudgetVersionId = bv.Id,
            BudgetLineId = line.Id,
            PlannedAmount = -50m, // negatif → CHECK ihlali
            PaymentSchedule = PaymentSchedule.MonthlyEqual,
            DistributionModel = DistributionModel.Equal,
            DueDayOfMonth = 15
        };
        db.BudgetLineVersions.Add(lv);

        var act = async () => await db.SaveChangesAsync();
        var assertion = await act.Should().ThrowAsync<DbUpdateException>();
        assertion.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.ConstraintName.Should().Be("ck_blv_planned_amount");
    }

    [Fact]
    public async Task BudgetLineVersion_DueDay_aralik_disinda_check_ihlali()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var budget = new Budget
        {
            TenantId = tenantId, CompanyId = Guid.NewGuid(),
            FiscalYearId = Guid.NewGuid(), Title = "DueDay test", Status = BudgetStatus.Draft,
            PeriodStartYear = 2026, PeriodStartMonth = 1, PeriodEndYear = 2026, PeriodEndMonth = 12
        };
        var bv = new BudgetVersion
        {
            TenantId = tenantId, BudgetId = budget.Id, VersionNumber = 1,
            ValidFrom = null, PublishedAt = null
        };
        var category = new ExpenseCategory
        {
            TenantId = tenantId, CompanyId = budget.CompanyId,
            Code = "DD", Name = "DueDay Kategori", DisplayOrder = 0
        };
        var line = new BudgetLine
        {
            TenantId = tenantId, CompanyId = budget.CompanyId,
            ExpenseCategoryId = category.Id, Code = "DD-01",
            Name = "DueDay Kalem", IsActive = true, DisplayOrder = 0
        };
        db.Budgets.Add(budget);
        db.BudgetVersions.Add(bv);
        db.ExpenseCategories.Add(category);
        db.BudgetLines.Add(line);
        await db.SaveChangesAsync();

        var lv = new BudgetLineVersion
        {
            TenantId = tenantId,
            BudgetVersionId = bv.Id,
            BudgetLineId = line.Id,
            PlannedAmount = 100m,
            PaymentSchedule = PaymentSchedule.MonthlyEqual,
            DistributionModel = DistributionModel.Equal,
            DueDayOfMonth = 32 // > 31 → CHECK ihlali
        };
        db.BudgetLineVersions.Add(lv);

        var act = async () => await db.SaveChangesAsync();
        var assertion = await act.Should().ThrowAsync<DbUpdateException>();
        assertion.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.ConstraintName.Should().Be("ck_blv_due_day");
    }

    [Fact]
    public async Task BudgetLineVersion_ayni_Version_Line_cifti_unique_ihlali()
    {
        var tenantId = Guid.NewGuid();
        SetTenantContext(tenantId);

        var budget = new Budget
        {
            TenantId = tenantId, CompanyId = Guid.NewGuid(),
            FiscalYearId = Guid.NewGuid(), Title = "Unique test", Status = BudgetStatus.Draft,
            PeriodStartYear = 2026, PeriodStartMonth = 1, PeriodEndYear = 2026, PeriodEndMonth = 12
        };
        var bv = new BudgetVersion
        {
            TenantId = tenantId, BudgetId = budget.Id, VersionNumber = 1,
            ValidFrom = null, PublishedAt = null
        };
        var category = new ExpenseCategory
        {
            TenantId = tenantId, CompanyId = budget.CompanyId,
            Code = "UQ", Name = "Unique Kategori", DisplayOrder = 0
        };
        var line = new BudgetLine
        {
            TenantId = tenantId, CompanyId = budget.CompanyId,
            ExpenseCategoryId = category.Id, Code = "UQ-01",
            Name = "Unique Kalem", IsActive = true, DisplayOrder = 0
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Budgets.Add(budget);
            db.BudgetVersions.Add(bv);
            db.ExpenseCategories.Add(category);
            db.BudgetLines.Add(line);
            db.BudgetLineVersions.Add(new BudgetLineVersion
            {
                TenantId = tenantId,
                BudgetVersionId = bv.Id,
                BudgetLineId = line.Id,
                PlannedAmount = 100m,
                PaymentSchedule = PaymentSchedule.MonthlyEqual,
                DistributionModel = DistributionModel.Equal,
                DueDayOfMonth = 15
            });
            await db.SaveChangesAsync();
        }

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.BudgetLineVersions.Add(new BudgetLineVersion
            {
                TenantId = tenantId,
                BudgetVersionId = bv.Id,
                BudgetLineId = line.Id, // aynı çift → unique ihlali
                PlannedAmount = 200m,
                PaymentSchedule = PaymentSchedule.MonthlyEqual,
                DistributionModel = DistributionModel.Equal,
                DueDayOfMonth = 15
            });

            var act = async () => await db.SaveChangesAsync();
            var assertion = await act.Should().ThrowAsync<DbUpdateException>();
            assertion.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be("23505");
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
