using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accruals.Queries;

/// <summary><see cref="GetUnitDebtStatusQuery"/> handler.</summary>
public sealed class GetUnitDebtStatusQueryHandler
    : IRequestHandler<GetUnitDebtStatusQuery, Result<UnitDebtStatus>>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetUnitDebtStatusQueryHandler(IMainDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<UnitDebtStatus>> Handle(
        GetUnitDebtStatusQuery request, CancellationToken cancellationToken)
    {
        var rows = await (
            from d in _db.AccrualDetails
            join a in _db.Accruals on d.AccrualId equals a.Id
            where d.UnitId == request.UnitId
                && a.CompanyId == request.CompanyId
                && !d.IsDeleted && !a.IsDeleted
            select new { d.Amount, d.DueDate }
        ).ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var totalAccrued = rows.Sum(r => r.Amount);
        var overdue = rows.Where(r => r.DueDate < today).Sum(r => r.Amount);
        var earliestDue = rows.Count == 0 ? (DateOnly?)null : rows.Min(r => r.DueDate);

        // FAZ 7'ye kadar ödeme yok: paid = 0, kalan = toplam
        var status = new UnitDebtStatus(
            request.UnitId,
            totalAccrued,
            overdue,
            rows.Count,
            earliestDue,
            PaidAmount: 0m,
            RemainingAmount: totalAccrued);

        return Result<UnitDebtStatus>.Success(status);
    }
}
