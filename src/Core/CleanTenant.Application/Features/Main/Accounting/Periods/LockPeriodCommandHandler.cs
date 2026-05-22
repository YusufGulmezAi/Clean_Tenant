using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.Periods;

/// <summary>
/// <see cref="LockPeriodCommand"/> handler. Open → Locked geçişi yapar.
/// </summary>
public sealed class LockPeriodCommandHandler
    : IRequestHandler<LockPeriodCommand, Result<bool>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public LockPeriodCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        LockPeriodCommand command,
        CancellationToken cancellationToken)
    {
        var period = await _db.AccountingPeriods
            .FirstOrDefaultAsync(p => p.Id == command.PeriodId
                                   && p.CompanyId == command.CompanyId
                                   && !p.IsDeleted, cancellationToken);

        if (period is null)
            return Result<bool>.Failure(
                Error.NotFound("ACC-004", "Muhasebe dönemi bulunamadı."));

        if (period.Status == PeriodStatus.ClosedPermanent)
            return Result<bool>.Failure(
                Error.Failure("ACC-201", "Muhasebe dönemi açık değil."));

        if (period.Status == PeriodStatus.Locked)
            return Result<bool>.Success(true); // Zaten kilitli, idempotent

        period.Status = PeriodStatus.Locked;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
