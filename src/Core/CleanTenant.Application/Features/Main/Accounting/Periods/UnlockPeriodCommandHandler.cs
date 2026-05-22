using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.Periods;

/// <summary>
/// <see cref="UnlockPeriodCommand"/> handler. Locked → Open geçişi yapar.
/// </summary>
public sealed class UnlockPeriodCommandHandler
    : IRequestHandler<UnlockPeriodCommand, Result<bool>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UnlockPeriodCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        UnlockPeriodCommand command,
        CancellationToken cancellationToken)
    {
        var period = await _db.AccountingPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.Id == command.PeriodId
                                   && p.CompanyId == command.CompanyId
                                   && !p.IsDeleted, cancellationToken);

        if (period is null)
            return Result<bool>.Failure(
                Error.NotFound("ACC-004", "Muhasebe dönemi bulunamadı."));

        // Mali yıl kalıcı kapalıysa kilit açılamaz
        if (period.FiscalYear.Status == PeriodStatus.ClosedPermanent)
            return Result<bool>.Failure(
                Error.Failure("ACC-216", "Kalıcı kapalı mali yılın dönemi açılamaz."));

        if (period.Status == PeriodStatus.ClosedPermanent)
            return Result<bool>.Failure(
                Error.Failure("ACC-217", "Kalıcı kapalı dönem açılamaz."));

        if (period.Status == PeriodStatus.Open)
            return Result<bool>.Success(true); // Zaten açık, idempotent

        period.Status = PeriodStatus.Open;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
