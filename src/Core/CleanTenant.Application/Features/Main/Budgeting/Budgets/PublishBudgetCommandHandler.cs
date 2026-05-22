using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// <see cref="PublishBudgetCommand"/> handler — Draft V1'i Published'a çevirir.
/// Bütçe Status = Published, BudgetVersion.PublishedAt + ValidFrom doldurulur,
/// Budget.CurrentVersionId set edilir.
/// </summary>
public sealed class PublishBudgetCommandHandler
    : IRequestHandler<PublishBudgetCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentSessionAccessor _session;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public PublishBudgetCommandHandler(
        IMainDbContext db,
        IClock clock,
        ICurrentSessionAccessor session)
    {
        _db = db;
        _clock = clock;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(PublishBudgetCommand request, CancellationToken cancellationToken)
    {
        // Bütçe + versiyon + kalem versiyon sayısı tek sorguda
        var budget = await _db.Budgets
            .Include(b => b.Versions)
            .FirstOrDefaultAsync(b => b.Id == request.BudgetId
                                   && b.CompanyId == request.CompanyId
                                   && !b.IsDeleted, cancellationToken);

        if (budget is null)
            return Result<Guid>.Failure(Error.NotFound("BDG-100", "Bütçe bulunamadı."));

        if (budget.Status != BudgetStatus.Draft)
            return Result<Guid>.Failure(
                Error.Failure("BDG-101", "Sadece taslak bütçe yayınlanabilir."));

        // Draft versiyonu bul (PublishedAt = null)
        var draft = budget.Versions.FirstOrDefault(v => v.PublishedAt is null && !v.IsDeleted);
        if (draft is null)
            return Result<Guid>.Failure(
                Error.Failure("BDG-102", "Yayınlanacak taslak versiyon bulunamadı."));

        // En az 1 kalem versiyonu olmalı
        var lineCount = await _db.BudgetLineVersions
            .CountAsync(lv => lv.BudgetVersionId == draft.Id && !lv.IsDeleted, cancellationToken);

        if (lineCount == 0)
            return Result<Guid>.Failure(
                Error.Failure("BDG-103", "Yayınlamadan önce en az bir bütçe kalemi eklemelisiniz."));

        // Mali yıl aralığı kontrolü
        var fiscalYear = await _db.FiscalYears
            .FirstOrDefaultAsync(fy => fy.Id == budget.FiscalYearId && !fy.IsDeleted, cancellationToken);

        if (fiscalYear is null)
            return Result<Guid>.Failure(Error.NotFound("BDG-002", "Mali yıl bulunamadı."));

        if (request.ValidFrom < fiscalYear.StartDate || request.ValidFrom > fiscalYear.EndDate)
            return Result<Guid>.Failure(
                Error.Failure("BDG-104",
                    $"Geçerlilik tarihi mali yıl aralığı dışında ({fiscalYear.StartDate:dd.MM.yyyy} - {fiscalYear.EndDate:dd.MM.yyyy})."));

        // Yayınla
        var now = _clock.UtcNow;
        var userId = _session.Current?.UserId;

        draft.ValidFrom = request.ValidFrom;
        draft.PublishedAt = now;
        draft.PublishedBy = userId;

        budget.Status = BudgetStatus.Published;
        budget.CurrentVersionId = draft.Id;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(draft.Id);
    }
}
