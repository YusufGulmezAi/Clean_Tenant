using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// <see cref="ReviseBudgetCommand"/> handler — yayınlı bütçeden V(N+1) üretir.
/// Eski versiyon dondurulur (ValidTo set), yeni versiyon line copy + override
/// ile yayınlanır.
/// </summary>
public sealed class ReviseBudgetCommandHandler
    : IRequestHandler<ReviseBudgetCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentSessionAccessor _session;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ReviseBudgetCommandHandler(
        IMainDbContext db,
        IClock clock,
        ICurrentSessionAccessor session)
    {
        _db = db;
        _clock = clock;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(ReviseBudgetCommand request, CancellationToken cancellationToken)
    {
        // Bütçe + tüm versiyonları
        var budget = await _db.Budgets
            .Include(b => b.Versions)
            .FirstOrDefaultAsync(b => b.Id == request.BudgetId
                                   && b.CompanyId == request.CompanyId
                                   && !b.IsDeleted, cancellationToken);

        if (budget is null)
            return Result<Guid>.Failure(Error.NotFound("BDG-800", "Bütçe bulunamadı."));

        if (budget.Status != BudgetStatus.Published)
            return Result<Guid>.Failure(
                Error.Failure("BDG-801", "Sadece yayınlı bütçe revize edilebilir."));

        if (budget.CurrentVersionId is not { } currentId)
            return Result<Guid>.Failure(
                Error.Failure("BDG-802", "Aktif yayınlı versiyon yok."));

        var currentVersion = budget.Versions.FirstOrDefault(v => v.Id == currentId);
        if (currentVersion is null || currentVersion.PublishedAt is null)
            return Result<Guid>.Failure(
                Error.NotFound("BDG-803", "Aktif versiyon bulunamadı."));

        // ValidFrom mevcut versiyondan sonra mı
        if (currentVersion.ValidFrom is { } currentFrom && request.ValidFrom <= currentFrom)
            return Result<Guid>.Failure(
                Error.Failure("BDG-804", "Revizyon tarihi mevcut versiyondan sonra olmalı."));

        // Mali yıl aralığı kontrolü
        var fiscalYear = await _db.FiscalYears
            .FirstOrDefaultAsync(fy => fy.Id == budget.FiscalYearId && !fy.IsDeleted, cancellationToken);
        if (fiscalYear is null)
            return Result<Guid>.Failure(Error.NotFound("BDG-002", "Mali yıl bulunamadı."));

        if (request.ValidFrom < fiscalYear.StartDate || request.ValidFrom > fiscalYear.EndDate)
            return Result<Guid>.Failure(
                Error.Failure("BDG-104",
                    $"Geçerlilik tarihi mali yıl aralığı dışında ({fiscalYear.StartDate:dd.MM.yyyy} - {fiscalYear.EndDate:dd.MM.yyyy})."));

        // Eski versiyon kalem versiyonları
        var oldLines = await _db.BudgetLineVersions
            .Where(lv => lv.BudgetVersionId == currentVersion.Id && !lv.IsDeleted)
            .ToListAsync(cancellationToken);

        if (oldLines.Count == 0)
            return Result<Guid>.Failure(
                Error.Failure("BDG-103", "Mevcut versiyonda kalem bulunamadı; revizyon anlamsız."));

        // Override id'leri eski versiyondaki line id'leri ile eşleşiyor mu
        var overrides = request.LineOverrides?.ToDictionary(x => x.BudgetLineId)
                        ?? new Dictionary<Guid, BudgetLineOverride>();
        foreach (var ov in overrides.Values)
        {
            if (oldLines.All(l => l.BudgetLineId != ov.BudgetLineId))
                return Result<Guid>.Failure(
                    Error.Failure("BDG-805",
                        $"Override edilen kalem (Id={ov.BudgetLineId}) mevcut versiyonda yok."));
        }

        var now = _clock.UtcNow;
        var userId = _session.Current?.UserId;

        // Yeni versiyon
        var newVersion = new BudgetVersion
        {
            TenantId = budget.TenantId,
            BudgetId = budget.Id,
            VersionNumber = budget.Versions.Max(v => v.VersionNumber) + 1,
            ValidFrom = request.ValidFrom,
            ValidTo = null,
            PreviousVersionId = currentVersion.Id,
            PublishedAt = now,
            PublishedBy = userId,
            RevisionReason = request.Reason.Trim()
        };
        _db.BudgetVersions.Add(newVersion);

        // Kalem versiyonlarını kopyala (+ override uygula)
        foreach (var ol in oldLines)
        {
            var ov = overrides.GetValueOrDefault(ol.BudgetLineId);
            var newLv = new BudgetLineVersion
            {
                TenantId = budget.TenantId,
                BudgetVersionId = newVersion.Id,
                BudgetLineId = ol.BudgetLineId,
                PlannedAmount = ov?.NewPlannedAmount ?? ol.PlannedAmount,
                PaymentSchedule = ov?.NewPaymentSchedule ?? ol.PaymentSchedule,
                DistributionModel = ov?.NewDistributionModel ?? ol.DistributionModel,
                ParticipationGroupId = ov is null ? ol.ParticipationGroupId : ov.NewParticipationGroupId,
                DistributionConfig = ov is null ? ol.DistributionConfig : ov.NewDistributionConfig,
                IsManualOverride = ov is not null,
                OverrideReason = ov is not null ? request.Reason.Trim() : null,
                DueDayOfMonth = ov?.NewDueDayOfMonth ?? ol.DueDayOfMonth
            };
            _db.BudgetLineVersions.Add(newLv);
        }

        // Eski versiyonu kapat
        currentVersion.ValidTo = request.ValidFrom.AddDays(-1);

        // Bütçenin aktif versiyonunu güncelle
        budget.CurrentVersionId = newVersion.Id;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(newVersion.Id);
    }
}
