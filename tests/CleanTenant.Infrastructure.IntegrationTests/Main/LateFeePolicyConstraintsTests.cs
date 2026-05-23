using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.Domain.Tenant.LateFees;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using CleanTenant.Infrastructure.Persistence.Main;
using CleanTenant.SharedKernel.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CleanTenant.Infrastructure.IntegrationTests.Main;

/// <summary>
/// FAZ 7B Slice 6 — Gecikme faizi politikası DB-level kısıt testleri: şirket-default
/// ve bütçe-override kısmi unique index'ler, monthly_rate_percent > 0 ve
/// grace_days ≥ 0 CHECK constraint'leri.
/// </summary>
public sealed class LateFeePolicyConstraintsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public LateFeePolicyConstraintsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static LateFeePolicy NewPolicy(
        Guid tenantId, Guid companyId, Guid incomeAccountId,
        Guid? budgetId = null, decimal rate = 3m, int grace = 0) => new()
    {
        TenantId = tenantId,
        CompanyId = companyId,
        BudgetId = budgetId,
        MonthlyRatePercent = rate,
        IsCompound = false,
        GraceDays = grace,
        IncomeAccountCodeId = incomeAccountId,
        IsActive = true,
    };

    [Fact]
    public async Task Sirket_default_iki_politika_unique_ihlali()
    {
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        SetTenantContext(tenantId);
        var incomeId = await SeedIncomeAccountAsync(tenantId, companyId);

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.LateFeePolicies.Add(NewPolicy(tenantId, companyId, incomeId, budgetId: null));
            await db.SaveChangesAsync();
        }

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.LateFeePolicies.Add(NewPolicy(tenantId, companyId, incomeId, budgetId: null));

            var act = async () => await db.SaveChangesAsync();
            var assertion = await act.Should().ThrowAsync<DbUpdateException>();
            assertion.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be("23505");
        }
    }

    [Fact]
    public async Task Butce_override_iki_politika_unique_ihlali()
    {
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        SetTenantContext(tenantId);
        var incomeId = await SeedIncomeAccountAsync(tenantId, companyId);
        var budgetId = await SeedBudgetAsync(tenantId, companyId);

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.LateFeePolicies.Add(NewPolicy(tenantId, companyId, incomeId, budgetId));
            await db.SaveChangesAsync();
        }

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.LateFeePolicies.Add(NewPolicy(tenantId, companyId, incomeId, budgetId));

            var act = async () => await db.SaveChangesAsync();
            var assertion = await act.Should().ThrowAsync<DbUpdateException>();
            assertion.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be("23505");
        }
    }

    [Fact]
    public async Task Sirket_default_ve_butce_override_birlikte_kabul()
    {
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        SetTenantContext(tenantId);
        var incomeId = await SeedIncomeAccountAsync(tenantId, companyId);
        var budgetId = await SeedBudgetAsync(tenantId, companyId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        db.LateFeePolicies.Add(NewPolicy(tenantId, companyId, incomeId, budgetId: null));
        db.LateFeePolicies.Add(NewPolicy(tenantId, companyId, incomeId, budgetId));

        var act = async () => await db.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Sifir_oran_check_ihlali()
    {
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        SetTenantContext(tenantId);
        var incomeId = await SeedIncomeAccountAsync(tenantId, companyId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        db.LateFeePolicies.Add(NewPolicy(tenantId, companyId, incomeId, rate: 0m));

        var act = async () => await db.SaveChangesAsync();
        var assertion = await act.Should().ThrowAsync<DbUpdateException>();
        assertion.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.ConstraintName.Should().Be("ck_late_fee_policies_rate");
    }

    [Fact]
    public async Task Negatif_grace_check_ihlali()
    {
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        SetTenantContext(tenantId);
        var incomeId = await SeedIncomeAccountAsync(tenantId, companyId);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        db.LateFeePolicies.Add(NewPolicy(tenantId, companyId, incomeId, grace: -1));

        var act = async () => await db.SaveChangesAsync();
        var assertion = await act.Should().ThrowAsync<DbUpdateException>();
        assertion.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.ConstraintName.Should().Be("ck_late_fee_policies_grace");
    }

    /// <summary>
    /// Gelir hesabı FK hedefi kurar. Yalnız satırın var olması yeterli (yaprak
    /// kontrolü application katmanında); ana-hesap kodu DB format + level_match
    /// CHECK'lerini sağlar.
    /// </summary>
    private async Task<Guid> SeedIncomeAccountAsync(Guid tenantId, Guid companyId)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var acc = new AccountCode
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = "649",
            Name = "Diğer Olağan Gelir ve Karlar",
            Level = AccountLevel.Main,
            IsDetail = false,
            IsActive = true,
        };
        db.AccountCodes.Add(acc);
        await db.SaveChangesAsync();
        return acc.Id;
    }

    /// <summary>Bütçe override FK hedefi için minimal bütçe kurar.</summary>
    private async Task<Guid> SeedBudgetAsync(Guid tenantId, Guid companyId)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var budget = new Budget
        {
            TenantId = tenantId,
            CompanyId = companyId,
            FiscalYearId = Guid.NewGuid(),
            Type = BudgetType.Aidat,
            Title = $"LF-{Guid.NewGuid():N}",
            Status = BudgetStatus.Draft,
            PeriodStartYear = 2026, PeriodStartMonth = 1, PeriodEndYear = 2026, PeriodEndMonth = 12,
        };
        db.Budgets.Add(budget);
        await db.SaveChangesAsync();
        return budget.Id;
    }

    private void SetTenantContext(Guid tenantId)
    {
        _fixture.TenantContext.TenantId.Returns(tenantId);
        _fixture.TenantContext.CompanyId.Returns((Guid?)null);
        _fixture.TenantContext.UnitId.Returns((Guid?)null);
        _fixture.TenantContext.CurrentScope.Returns(ScopeLevel.Tenant);
    }
}
