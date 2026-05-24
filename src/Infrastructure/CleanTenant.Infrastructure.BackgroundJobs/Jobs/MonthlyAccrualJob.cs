using CleanTenant.Application.Common.Jobs;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.Accruals.GenerateBudgetAccrual;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// <para>
/// Aylık otomatik tahakkuk üretici (FAZ 6.8). Her ayın 1'inde çalışır; tüm aktif
/// tenant'ları gezer, her tenant için dönem penceresi o ayı kapsayan yayınlı
/// bütçelere <see cref="GenerateBudgetAccrualCommand"/> (Force=false, idempotent)
/// gönderir. Zaten üretilmiş veya açık dönemi olmayan bütçeler atlanır ve loglanır;
/// bir tenant/bütçe hatası diğerlerini durdurmaz.
/// </para>
/// </summary>
public sealed class MonthlyAccrualJob
{
    /// <summary>Hangfire recurring job kimliği.</summary>
    public const string RecurringJobId = "monthly-budget-accrual";

    private static readonly IReadOnlyList<string> JobPermissions = ["tenant.accrual.generate"];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISystemJobExecutor _executor;
    private readonly IClock _clock;
    private readonly ILogger<MonthlyAccrualJob> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public MonthlyAccrualJob(
        IServiceScopeFactory scopeFactory,
        ISystemJobExecutor executor,
        IClock clock,
        ILogger<MonthlyAccrualJob> logger)
    {
        _scopeFactory = scopeFactory;
        _executor = executor;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Recurring job giriş noktası.</summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var year = today.Year;
        var month = today.Month;

        // 1. Aktif tenant'lar (Catalog — tenant-bağımsız sorgu)
        List<Guid> tenantIds;
        using (var scope = _scopeFactory.CreateScope())
        {
            var catalog = scope.ServiceProvider.GetRequiredService<ICatalogDbContext>();
            tenantIds = await catalog.Tenants
                .Where(t => !t.IsDeleted && t.Status == TenantStatus.Active)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Otomatik tahakkuk başladı: dönem {Year}-{Month:D2}, {Count} aktif tenant.",
            year, month, tenantIds.Count);

        var totalGenerated = 0;
        foreach (var tenantId in tenantIds)
        {
            try
            {
                totalGenerated += await _executor.RunForTenantAsync(
                    tenantId,
                    JobPermissions,
                    (sp, ct) => GenerateForTenantAsync(sp, tenantId, year, month, ct),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Tenant {TenantId} otomatik tahakkuk sırasında hata; diğer tenant'lara devam ediliyor.",
                    tenantId);
            }
        }

        _logger.LogInformation(
            "Otomatik tahakkuk bitti: dönem {Year}-{Month:D2}, toplam {Total} tahakkuk üretildi.",
            year, month, totalGenerated);
    }

    private static async Task<int> GenerateForTenantAsync(
        IServiceProvider sp, Guid tenantId, int year, int month, CancellationToken ct)
    {
        var db = sp.GetRequiredService<IMainDbContext>();
        var mediator = sp.GetRequiredService<ISender>();
        var logger = sp.GetRequiredService<ILogger<MonthlyAccrualJob>>();

        var periodIdx = year * 12 + month;

        // Dönem penceresi bu ayı kapsayan, yayınlı bütçeler (query filter tenant'a göre)
        var budgets = await db.Budgets
            .Where(b => b.Status == BudgetStatus.Published
                && !b.IsDeleted
                && (b.PeriodStartYear * 12 + b.PeriodStartMonth) <= periodIdx
                && (b.PeriodEndYear * 12 + b.PeriodEndMonth) >= periodIdx)
            .Select(b => new { b.Id, b.CompanyId })
            .ToListAsync(ct);

        var generated = 0;
        foreach (var b in budgets)
        {
            var result = await mediator.Send(new GenerateBudgetAccrualCommand(
                TenantId: tenantId,
                CompanyId: b.CompanyId,
                BudgetId: b.Id,
                Year: year,
                Month: month,
                Force: false), ct);

            if (result.IsSuccess)
            {
                generated++;
            }
            else
            {
                logger.LogWarning(
                    "Bütçe {BudgetId} {Year}-{Month:D2} tahakkuk atlandı: {Code} — {Message}",
                    b.Id, year, month, result.FirstError.Code, result.FirstError.Message);
            }
        }

        return generated;
    }
}
