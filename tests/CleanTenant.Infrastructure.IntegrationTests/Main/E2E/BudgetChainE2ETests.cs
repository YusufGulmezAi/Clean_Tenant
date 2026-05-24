using System.Globalization;
using CleanTenant.Application.Features.Main.Accruals.GenerateBudgetAccrual;
using CleanTenant.Application.Features.Main.Accruals.Queries;
using CleanTenant.Application.Features.Main.Budgeting.Budgets;
using CleanTenant.Application.Features.Main.Collections.RecordCollection;
using CleanTenant.Application.Features.Main.LateFees.GenerateLateFeeCharges;
using CleanTenant.Application.Features.Main.LateFees.SetLateFeePolicy;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.Domain.Tenant.Accruals.Enums;
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

    [Fact]
    public async Task Senaryo2_7BB_LRM_kurus_kaybi_yok()
    {
        // 12000/12 = 1000/ay; 7 BB'ye dağıtım 1000/7 = 142.857… → LRM ile kuruş tam.
        var s = await SeedScenarioAsync(unitCount: 7, plannedAnnual: 12_000m, DistributionModel.BySquareMeter);

        _fixture.Clock.UtcNow = new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero);
        var acc = Ok(await SendAsync(new GenerateBudgetAccrualCommand(
            s.TenantId, s.CompanyId, s.BudgetId, 2026, 3)));

        acc.TotalAmount.Should().Be(1_000m);
        acc.DetailCount.Should().Be(7);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var amounts = await db.AccrualDetails
            .Where(d => d.AccrualId == acc.AccrualId)
            .Select(d => d.Amount)
            .ToListAsync();

        amounts.Should().HaveCount(7);
        amounts.Sum().Should().Be(1_000m, "LRM kuruş kaybı/fazlası bırakmaz (m² ağırlıklı dağıtım)");
        amounts.Should().OnlyContain(a => a > 0m, "her BB pozitif pay alır");
        amounts.Should().OnlyContain(a => a == Math.Round(a, 2), "tutarlar kuruş hassasiyetinde");

        // Her pay, m² oranıyla hesaplanan kesin payın en çok bir kuruş yakınında (LRM)
        var totalM2 = Enumerable.Range(0, 7).Sum(i => 100m + (i * 10m)); // 910
        var byUnit = await db.AccrualDetails
            .Where(d => d.AccrualId == acc.AccrualId)
            .Select(d => new { d.UnitId, d.Amount })
            .ToListAsync();
        foreach (var d in byUnit)
        {
            var idx = s.UnitIds.ToList().IndexOf(d.UnitId);
            var exact = (100m + (idx * 10m)) / totalM2 * 1_000m;
            Math.Abs(d.Amount - exact).Should().BeLessThanOrEqualTo(0.01m);
        }

        // Yevmiye fişi de toplam ile birebir
        var entry = await db.JournalEntries.Include(e => e.Lines)
            .FirstAsync(e => e.ReferenceId == acc.AccrualId);
        entry.Lines.Where(l => l.Debit > 0).Sum(l => l.Debit).Should().Be(1_000m);
    }

    [Fact]
    public async Task Senaryo3_yil_ortasi_revizyon_donem_dogru_versiyonu_kullanir()
    {
        // V1: yıllık 36000 → 3000/ay. Temmuz'da revizyon V2: yıllık 60000 → 5000/ay.
        var s = await SeedScenarioAsync(unitCount: 3, plannedAnnual: 36_000m, DistributionModel.Equal);

        Guid v1Id, v2Id;
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var v1 = await db.BudgetVersions.FirstAsync(v => v.BudgetId == s.BudgetId);
            v1.ValidTo = new DateOnly(2026, 6, 30); // V1 Haziran sonunda kapanır
            v1Id = v1.Id;
            var lineId = (await db.BudgetLines.FirstAsync(l => l.CompanyId == s.CompanyId)).Id;

            var v2 = new BudgetVersion
            {
                TenantId = s.TenantId, BudgetId = s.BudgetId, VersionNumber = 2,
                ValidFrom = new DateOnly(2026, 7, 1), ValidTo = null,
                PublishedAt = new DateTimeOffset(2026, 6, 25, 0, 0, 0, TimeSpan.Zero),
                PreviousVersionId = v1.Id,
            };
            db.BudgetVersions.Add(v2);
            db.BudgetLineVersions.Add(new BudgetLineVersion
            {
                TenantId = s.TenantId, BudgetVersionId = v2.Id, BudgetLineId = lineId,
                PlannedAmount = 60_000m, PaymentSchedule = PaymentSchedule.MonthlyEqual,
                DistributionModel = DistributionModel.Equal, DueDayOfMonth = 15,
            });
            await db.SaveChangesAsync();
            v2Id = v2.Id;
        }

        _fixture.Clock.UtcNow = new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero);
        var march = Ok(await SendAsync(new GenerateBudgetAccrualCommand(s.TenantId, s.CompanyId, s.BudgetId, 2026, 3)));
        march.TotalAmount.Should().Be(3_000m, "Mart V1 penceresinde (36000/12)");

        var august = Ok(await SendAsync(new GenerateBudgetAccrualCommand(s.TenantId, s.CompanyId, s.BudgetId, 2026, 8)));
        august.TotalAmount.Should().Be(5_000m, "Ağustos V2 penceresinde (60000/12)");

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            (await db.Accruals.FirstAsync(a => a.Id == march.AccrualId)).BudgetVersionId.Should().Be(v1Id);
            (await db.Accruals.FirstAsync(a => a.Id == august.AccrualId)).BudgetVersionId.Should().Be(v2Id);
        }
    }

    [Fact]
    public async Task Senaryo4_gecikme_faizi_ve_TBK_m101_once_gecikme_kapatir()
    {
        var s = await SeedScenarioAsync(unitCount: 3, plannedAnnual: 36_000m, DistributionModel.Equal);

        // Gecikme geliri hesabı (yaprak, 3-3-3)
        Guid lateFeeIncomeId;
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var income = new AccountCode
            {
                TenantId = s.TenantId, CompanyId = s.CompanyId,
                Code = "642.001.001", Name = "Gecikme Faizi Geliri",
                Level = AccountLevel.Detail, IsDetail = true, IsActive = true,
            };
            db.AccountCodes.Add(income);
            await db.SaveChangesAsync();
            lateFeeIncomeId = income.Id;
        }

        // Mart tahakkuğu (3 BB × 1000, vade 2026-04-15)
        _fixture.Clock.UtcNow = new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero);
        Ok(await SendAsync(new GenerateBudgetAccrualCommand(s.TenantId, s.CompanyId, s.BudgetId, 2026, 3)));

        // Şirket-geneli gecikme politikası: aylık %3, grace 0
        Ok(await SendAsync(new SetLateFeePolicyCommand(
            s.TenantId, s.CompanyId, BudgetId: null,
            MonthlyRatePercent: 3m, IsCompound: false, GraceDays: 0, IncomeAccountCodeId: lateFeeIncomeId)));

        // 2026-05-15 (vadeden 30 gün sonra) gecikme faizi üret: 1000 × %3 × 30/30 = 30/BB
        _fixture.Clock.UtcNow = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);
        var lateFee = Ok(await SendAsync(new GenerateLateFeeChargesCommand(
            s.TenantId, s.CompanyId, new DateOnly(2026, 5, 15))));

        lateFee.ChargedUnitCount.Should().Be(3);
        lateFee.TotalLateFeeAmount.Should().Be(90m); // 3 × 30

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var lateAccruals = await db.Accruals
                .Where(a => a.CompanyId == s.CompanyId && a.Source == AccrualSource.LateFee)
                .Include(a => a.Details)
                .ToListAsync();
            lateAccruals.Should().ContainSingle();
            lateAccruals[0].Details.Should().HaveCount(3);
            lateAccruals[0].Details.Should().OnlyContain(d => d.Amount == 30m);
        }

        // TBK m.101: BB#1'e 30 ödeme → önce gecikme kapanır (anapara dokunulmaz)
        var col = Ok(await SendAsync(new RecordCollectionCommand(
            s.TenantId, s.CompanyId, s.UnitIds[0], s.PeriodId,
            new DateOnly(2026, 5, 16), 30m, PaymentMethod.Cash, s.CashAccountCodeId)));
        col.AllocationCount.Should().Be(1);

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

            // BB#1'in gecikme detayı tamamen kapandı, anaparası açık kaldı
            var unit1Details = await (
                from d in db.AccrualDetails
                join a in db.Accruals on d.AccrualId equals a.Id
                where d.UnitId == s.UnitIds[0] && a.CompanyId == s.CompanyId
                select new { d.Id, d.Amount, a.Source }).ToListAsync();

            var lateDetail = unit1Details.Single(x => x.Source == AccrualSource.LateFee);
            var principalDetail = unit1Details.Single(x => x.Source == AccrualSource.Budget);

            var lateAllocated = await db.CollectionAllocations
                .Where(al => al.AccrualDetailId == lateDetail.Id).SumAsync(al => al.AllocatedAmount);
            var principalAllocated = await db.CollectionAllocations
                .Where(al => al.AccrualDetailId == principalDetail.Id).SumAsync(al => al.AllocatedAmount);

            lateAllocated.Should().Be(30m, "TBK m.101: önce gecikme faizi kapatılır");
            principalAllocated.Should().Be(0m, "anaparaya henüz dokunulmaz");
        }
    }

    [Fact]
    public async Task Senaryo5_butce_yenileme_klon_kalem_tutar_taksit_kopyalar()
    {
        var s = await SeedScenarioAsync(unitCount: 3, plannedAnnual: 36_000m, DistributionModel.Equal);

        Guid fy2027Id, sourceVersionId, monthlyLineId, installmentLineId;
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var budget = await db.Budgets.FirstAsync(b => b.Id == s.BudgetId);
            sourceVersionId = budget.CurrentVersionId!.Value;
            monthlyLineId = (await db.BudgetLines.FirstAsync(l => l.CompanyId == s.CompanyId)).Id;
            var categoryId = (await db.ExpenseCategories.FirstAsync(c => c.CompanyId == s.CompanyId)).Id;

            // 2027 mali yıl (klon hedefi)
            var fy = new FiscalYear
            {
                TenantId = s.TenantId, CompanyId = s.CompanyId, Label = "2027",
                StartDate = new DateOnly(2027, 1, 1), EndDate = new DateOnly(2027, 12, 31),
                Status = PeriodStatus.Open,
            };
            db.FiscalYears.Add(fy);
            fy2027Id = fy.Id;

            // Kaynak yayınlı versiyona bir Installment kalemi + 2 taksit (2026-03, 2026-04)
            var instLine = new BudgetLine
            {
                TenantId = s.TenantId, CompanyId = s.CompanyId, ExpenseCategoryId = categoryId,
                Code = "YAT-01", Name = "Asansör Yatırımı", IsActive = true, DisplayOrder = 1,
            };
            db.BudgetLines.Add(instLine);
            installmentLineId = instLine.Id;
            var instLv = new BudgetLineVersion
            {
                TenantId = s.TenantId, BudgetVersionId = sourceVersionId, BudgetLineId = instLine.Id,
                PlannedAmount = 10_000m, PaymentSchedule = PaymentSchedule.Installment,
                DistributionModel = DistributionModel.Equal, DueDayOfMonth = 15,
                InstallmentStartYear = 2026, InstallmentStartMonth = 3,
                InstallmentEndYear = 2026, InstallmentEndMonth = 4, InstallmentIntervalMonths = 1,
            };
            db.BudgetLineVersions.Add(instLv);
            db.BudgetLineInstallments.Add(new BudgetLineInstallment
            {
                TenantId = s.TenantId, BudgetLineVersionId = instLv.Id,
                InstallmentNumber = 1, Year = 2026, Month = 3, Amount = 5_000m,
            });
            db.BudgetLineInstallments.Add(new BudgetLineInstallment
            {
                TenantId = s.TenantId, BudgetLineVersionId = instLv.Id,
                InstallmentNumber = 2, Year = 2026, Month = 4, Amount = 5_000m,
            });
            await db.SaveChangesAsync();
        }

        // Klonla → 2027
        var newBudgetId = Ok(await SendAsync(new CloneBudgetCommand(
            s.TenantId, s.CompanyId, s.BudgetId, fy2027Id, "2027 Yıllık Aidat")));

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var clone = await db.Budgets.Include(b => b.Versions).FirstAsync(b => b.Id == newBudgetId);

            clone.Status.Should().Be(BudgetStatus.Draft);
            clone.FiscalYearId.Should().Be(fy2027Id);
            clone.Type.Should().Be(BudgetType.Aidat);
            clone.ReceivableAccountCodeId.Should().BeNull("hesap kodları ilk tahakkukta açılır");
            clone.Versions.Should().ContainSingle();
            var v1 = clone.Versions.Single();
            v1.VersionNumber.Should().Be(1);
            v1.PublishedAt.Should().BeNull("klon Draft'tır");

            var cloneLines = await db.BudgetLineVersions
                .Where(lv => lv.BudgetVersionId == v1.Id).ToListAsync();
            cloneLines.Should().HaveCount(2);
            // Aynı şirket → kalem ID'leri YENİDEN kullanıldı (yeni tanım üretilmedi)
            cloneLines.Select(lv => lv.BudgetLineId)
                .Should().BeEquivalentTo(new[] { monthlyLineId, installmentLineId });

            cloneLines.First(lv => lv.BudgetLineId == monthlyLineId)
                .PlannedAmount.Should().Be(36_000m, "tutarlar aynen kopyalanır (yenileme)");

            var inst = cloneLines.First(lv => lv.BudgetLineId == installmentLineId);
            inst.InstallmentStartYear.Should().Be(2027, "taksit başlangıç yılı yeni döneme ötelendi");
            inst.InstallmentEndYear.Should().Be(2027);

            var cloneInstallments = await db.BudgetLineInstallments
                .Where(i => i.BudgetLineVersionId == inst.Id).OrderBy(i => i.Month).ToListAsync();
            cloneInstallments.Should().HaveCount(2);
            cloneInstallments.Select(i => (i.Year, i.Month)).Should().Equal((2027, 3), (2027, 4));
            cloneInstallments.Sum(i => i.Amount).Should().Be(10_000m);
        }
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
        AccountingPeriod marchPeriod = null!;
        for (var m = 1; m <= 12; m++)
        {
            var first = new DateOnly(2026, m, 1);
            var p = new AccountingPeriod
            {
                TenantId = tenantId, CompanyId = company.Id, FiscalYearId = fy.Id,
                Year = 2026, Month = m,
                StartDate = first, EndDate = first.AddMonths(1).AddDays(-1),
                Status = PeriodStatus.Open,
            };
            db.AccountingPeriods.Add(p);
            if (m == 3) marchPeriod = p;
        }

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

        return new Scenario(tenantId, company.Id, budget.Id, marchPeriod.Id, fy.Id, cash.Id, unitIds);
    }
}
