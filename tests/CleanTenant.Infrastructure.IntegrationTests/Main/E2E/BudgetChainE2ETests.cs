using System.Globalization;
using CleanTenant.Application.Features.Main.Accruals.GenerateBudgetAccrual;
using CleanTenant.Application.Features.Main.Accruals.Queries;
using CleanTenant.Application.Features.Main.Collections.RecordCollection;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.Domain.Tenant.Collections.Enums;
using CleanTenant.Domain.Tenant.Companies;
using CleanTenant.Domain.Budgeting;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.Infrastructure.Persistence.Main;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BuildingUnit = CleanTenant.Domain.Tenant.BuildingSchema.Unit;

namespace CleanTenant.Infrastructure.IntegrationTests.Main.E2E;

/// <summary>
/// FAZ 8 — Bütçe → Tahakkuk → Tahsilat → Borç Durumu uçtan-uca senaryosu.
/// Gerçek PostgreSQL + tüm Application handler'ları ile çalışır; hesap kodu
/// otomasyonu (3-3-3), dağıtım, yevmiye fişi ve tahsilat dağıtımı bir arada doğrulanır.
/// </summary>
public sealed class BudgetChainE2ETests : IClassFixture<BudgetE2EFixture>
{
    private readonly BudgetE2EFixture _fixture;

    public BudgetChainE2ETests(BudgetE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Senaryo1_3BB_aylik_aidat_tahakkuk_tahsilat_borc_durumu()
    {
        var s = await SeedScenarioAsync(unitCount: 3, plannedAnnual: 36_000m, DistributionModel.Equal);

        // ── 1. Tahakkuk üret (2026-03) ───────────────────────────────────────────
        _fixture.Clock.UtcNow = new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero);
        var acc = Ok(await SendAsync(new GenerateBudgetAccrualCommand(
            s.TenantId, s.CompanyId, s.BudgetId, 2026, 3)));

        acc.TotalAmount.Should().Be(3_000m);   // 36000/12 = 3000/ay
        acc.DetailCount.Should().Be(3);

        // ── 2. Hesap kodları 3-3-3 otomatik açıldı + yevmiye fişi dengeli ────────
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

            var detailCodes = await db.AccountCodes
                .Where(a => a.CompanyId == s.CompanyId && a.IsDetail)
                .Select(a => a.Code)
                .ToListAsync();
            detailCodes.Should().Contain("120.001.001"); // alacak (Aidat)
            detailCodes.Should().Contain("600.001.001"); // gelir (Aidat)

            var details = await db.AccrualDetails
                .Where(d => d.AccrualId == acc.AccrualId)
                .ToListAsync();
            details.Should().HaveCount(3);
            details.Should().OnlyContain(d => d.Amount == 1_000m); // 3000/3 eşit
            details.Sum(d => d.Amount).Should().Be(3_000m);
            details.Should().OnlyContain(d => d.DueDate == new DateOnly(2026, 4, 15)); // ertesi ay gün 15

            var entry = await db.JournalEntries
                .Include(e => e.Lines)
                .FirstAsync(e => e.ReferenceId == acc.AccrualId);
            entry.Status.Should().Be(JournalEntryStatus.Posted);
            entry.TotalDebit.Should().Be(3_000m);
            entry.TotalCredit.Should().Be(3_000m);
            entry.Lines.Should().HaveCount(2);
            entry.Lines.Sum(l => l.Debit).Should().Be(entry.Lines.Sum(l => l.Credit));
        }

        // ── 3. BB#1 tam ödeme (1000) ─────────────────────────────────────────────
        var col = Ok(await SendAsync(new RecordCollectionCommand(
            s.TenantId, s.CompanyId, s.UnitIds[0], s.PeriodId,
            new DateOnly(2026, 3, 20), 1_000m, PaymentMethod.Cash, s.CashAccountCodeId)));

        col.AllocatedAmount.Should().Be(1_000m);
        col.UnallocatedAmount.Should().Be(0m);
        col.AllocationCount.Should().Be(1);

        // ── 4. Borç durumu — vadeden sonra (2026-05) ─────────────────────────────
        _fixture.Clock.UtcNow = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

        var paid = Ok(await SendAsync(new GetUnitDebtStatusQuery(s.CompanyId, s.UnitIds[0])));
        paid.TotalAccrued.Should().Be(1_000m);
        paid.PaidAmount.Should().Be(1_000m);
        paid.RemainingAmount.Should().Be(0m);
        paid.OverdueAmount.Should().Be(0m);

        var unpaid = Ok(await SendAsync(new GetUnitDebtStatusQuery(s.CompanyId, s.UnitIds[1])));
        unpaid.TotalAccrued.Should().Be(1_000m);
        unpaid.PaidAmount.Should().Be(0m);
        unpaid.RemainingAmount.Should().Be(1_000m);
        unpaid.OverdueAmount.Should().Be(1_000m); // vade 2026-04-15 < 2026-05-01
    }

    /// <summary>Result'ı başarı doğrulayıp non-null değerini döner.</summary>
    private static T Ok<T>(Result<T> result)
    {
        result.IsFailure.Should().BeFalse(
            result.IsFailure ? $"{result.FirstError.Code}: {result.FirstError.Message}" : "ok");
        return result.Value!;
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────────

    private async Task<T> SendAsync<T>(IRequest<T> request)
    {
        using var scope = _fixture.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        return await mediator.Send(request);
    }

    private sealed record Scenario(
        Guid TenantId, Guid CompanyId, Guid BudgetId, Guid PeriodId,
        Guid FiscalYearId, Guid CashAccountCodeId, IReadOnlyList<Guid> UnitIds);

    private async Task<Scenario> SeedScenarioAsync(int unitCount, decimal plannedAnnual, DistributionModel model)
    {
        var tenantId = Guid.NewGuid();
        _fixture.SetTenant(tenantId);

        using var scope = _fixture.Services.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        // Catalog: Aidat bütçe tipi metadata (allocator base kodları okur)
        if (!await catalog.BudgetTypeMetadata.AnyAsync(m => m.Type == BudgetType.Aidat))
        {
            catalog.BudgetTypeMetadata.Add(new BudgetTypeMetadata
            {
                Type = BudgetType.Aidat,
                DisplayName = "Aidat",
                BaseReceivableCode = "120.001",
                BaseIncomeCode = "600.001",
                DefaultPaymentSchedule = PaymentSchedule.MonthlyEqual,
                AllowMultiplePerYear = true,
                DisplayOrder = 0,
                IsActive = true,
            });
            await catalog.SaveChangesAsync();
        }

        // Yapı şeması
        var company = new Company { TenantId = tenantId, Name = $"Site-{Guid.NewGuid():N}", Status = CompanyStatus.Active };
        var land = new Land { TenantId = tenantId, CompanyId = company.Id, Name = "Ada-1", SortOrder = 0 };
        var parcel = new Parcel { TenantId = tenantId, LandId = land.Id, Name = "Parsel-1", SortOrder = 0 };
        var building = new Building { TenantId = tenantId, ParcelId = parcel.Id, Name = "A Blok", SortOrder = 0 };
        db.Companies.Add(company);
        db.Lands.Add(land);
        db.Parcels.Add(parcel);
        db.Buildings.Add(building);

        var unitIds = new List<Guid>();
        for (var i = 0; i < unitCount; i++)
        {
            var unit = new BuildingUnit
            {
                TenantId = tenantId, BuildingId = building.Id,
                Number = (i + 1).ToString(CultureInfo.InvariantCulture),
                GrossSquareMeters = 100m + (i * 10m), SortOrder = i,
            };
            db.Units.Add(unit);
            unitIds.Add(unit.Id);
        }

        // Mali yıl + Mart dönemi (Open)
        var fy = new FiscalYear
        {
            TenantId = tenantId, CompanyId = company.Id, Label = "2026",
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            Status = PeriodStatus.Open, IsCurrentYear = true,
        };
        db.FiscalYears.Add(fy);
        var period = new AccountingPeriod
        {
            TenantId = tenantId, CompanyId = company.Id, FiscalYearId = fy.Id,
            Year = 2026, Month = 3,
            StartDate = new DateOnly(2026, 3, 1), EndDate = new DateOnly(2026, 3, 31),
            Status = PeriodStatus.Open,
        };
        db.AccountingPeriods.Add(period);

        // Kasa hesabı (tahsilat yevmiyesi borç tarafı) — 3-3-3 yaprak
        var cash = new AccountCode
        {
            TenantId = tenantId, CompanyId = company.Id,
            Code = "100.001.001", Name = "Merkez Kasa",
            Level = AccountLevel.Detail, IsDetail = true, IsActive = true,
        };
        db.AccountCodes.Add(cash);

        // Bütçe + V1 (yayınlı) + kalem
        var budget = new Budget
        {
            TenantId = tenantId, CompanyId = company.Id, FiscalYearId = fy.Id,
            Type = BudgetType.Aidat, Title = "2026 Yıllık Aidat", Status = BudgetStatus.Published,
            PeriodStartYear = 2026, PeriodStartMonth = 1, PeriodEndYear = 2026, PeriodEndMonth = 12,
        };
        var version = new BudgetVersion
        {
            TenantId = tenantId, BudgetId = budget.Id, VersionNumber = 1,
            ValidFrom = new DateOnly(2026, 1, 1), ValidTo = null,
            PublishedAt = new DateTimeOffset(2025, 12, 20, 0, 0, 0, TimeSpan.Zero),
        };
        budget.CurrentVersionId = version.Id;
        var category = new ExpenseCategory
        {
            TenantId = tenantId, CompanyId = company.Id, Code = "GEN", Name = "Genel", DisplayOrder = 0,
        };
        var line = new BudgetLine
        {
            TenantId = tenantId, CompanyId = company.Id, ExpenseCategoryId = category.Id,
            Code = "AID-01", Name = "Aidat", IsActive = true, DisplayOrder = 0,
        };
        var lineVersion = new BudgetLineVersion
        {
            TenantId = tenantId, BudgetVersionId = version.Id, BudgetLineId = line.Id,
            PlannedAmount = plannedAnnual, PaymentSchedule = PaymentSchedule.MonthlyEqual,
            DistributionModel = model, DueDayOfMonth = 15,
        };
        db.Budgets.Add(budget);
        db.BudgetVersions.Add(version);
        db.ExpenseCategories.Add(category);
        db.BudgetLines.Add(line);
        db.BudgetLineVersions.Add(lineVersion);

        await db.SaveChangesAsync();

        return new Scenario(tenantId, company.Id, budget.Id, period.Id, fy.Id, cash.Id, unitIds);
    }
}
