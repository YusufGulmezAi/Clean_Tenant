using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary>
/// <see cref="AddBudgetLineToVersionCommand"/> handler. Versiyon Draft olmalı,
/// kalem aynı şirkete ait olmalı, aynı (Version, Line) için tekrar eklenemez.
/// </summary>
public sealed class AddBudgetLineToVersionCommandHandler
    : IRequestHandler<AddBudgetLineToVersionCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AddBudgetLineToVersionCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(AddBudgetLineToVersionCommand request, CancellationToken cancellationToken)
    {
        // Versiyon var mı + bu şirketin bir Budget'ına mı bağlı + Draft mi (PublishedAt = null)
        var versionInfo = await (
            from v in _db.BudgetVersions
            join b in _db.Budgets on v.BudgetId equals b.Id
            where v.Id == request.BudgetVersionId
                && b.CompanyId == request.CompanyId
                && !v.IsDeleted && !b.IsDeleted
            select new { v.Id, v.PublishedAt }
        ).FirstOrDefaultAsync(cancellationToken);

        if (versionInfo is null)
            return Result<Guid>.Failure(Error.NotFound("BDG-400", "Bütçe versiyonu bulunamadı."));

        if (versionInfo.PublishedAt is not null)
            return Result<Guid>.Failure(
                Error.Failure("BDG-401", "Yayınlanmış versiyona kalem eklenemez. Revize edin."));

        // Kalem aynı şirkete ait + aktif
        var lineOk = await _db.BudgetLines
            .AnyAsync(l => l.Id == request.BudgetLineId
                        && l.CompanyId == request.CompanyId
                        && l.IsActive
                        && !l.IsDeleted, cancellationToken);
        if (!lineOk)
            return Result<Guid>.Failure(
                Error.NotFound("BDG-402", "Aktif bütçe kalemi bulunamadı."));

        // Aynı (Version, Line) zaten eklenmiş mi
        var duplicate = await _db.BudgetLineVersions
            .AnyAsync(lv => lv.BudgetVersionId == request.BudgetVersionId
                         && lv.BudgetLineId == request.BudgetLineId
                         && !lv.IsDeleted, cancellationToken);
        if (duplicate)
            return Result<Guid>.Failure(
                Error.Conflict("BDG-403", "Bu kalem versiyonda zaten mevcut."));

        // Katılım grubu varsa: aynı şirkette mi
        if (request.ParticipationGroupId is { } pgId)
        {
            var pgOk = await _db.ParticipationGroups
                .AnyAsync(g => g.Id == pgId
                            && g.CompanyId == request.CompanyId
                            && g.IsActive
                            && !g.IsDeleted, cancellationToken);
            if (!pgOk)
                return Result<Guid>.Failure(
                    Error.NotFound("BDG-404", "Aktif katılım grubu bulunamadı."));
        }

        // Sanity: PlannedAmount ≥ 0, DueDay 1-31
        if (request.PlannedAmount < 0)
            return Result<Guid>.Failure(
                Error.Failure("BDG-405", "Planlanan tutar negatif olamaz."));
        if (request.DueDayOfMonth < 1 || request.DueDayOfMonth > 31)
            return Result<Guid>.Failure(
                Error.Failure("BDG-406", "Vade günü 1-31 aralığında olmalı."));

        var lineVersion = new BudgetLineVersion
        {
            TenantId = request.TenantId,
            BudgetVersionId = request.BudgetVersionId,
            BudgetLineId = request.BudgetLineId,
            PlannedAmount = request.PlannedAmount,
            PaymentSchedule = request.PaymentSchedule,
            DistributionModel = request.DistributionModel,
            ParticipationGroupId = request.ParticipationGroupId,
            DistributionConfig = request.DistributionConfig,
            IsManualOverride = false,
            OverrideReason = null,
            DueDayOfMonth = request.DueDayOfMonth
        };

        _db.BudgetLineVersions.Add(lineVersion);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(lineVersion.Id);
    }
}
