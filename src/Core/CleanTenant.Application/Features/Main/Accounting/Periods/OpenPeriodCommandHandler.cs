using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.Periods;

/// <summary>
/// <see cref="OpenPeriodCommand"/> handler.
/// Kilitli dönemi Open'a çevirir.
/// </summary>
public sealed class OpenPeriodCommandHandler
    : IRequestHandler<OpenPeriodCommand, Result<bool>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public OpenPeriodCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        OpenPeriodCommand command,
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

        // Mali yıl kalıcı kapalıysa dönem açılamaz
        if (period.FiscalYear.Status == PeriodStatus.ClosedPermanent)
            return Result<bool>.Failure(
                Error.Failure("ACC-216", "Kalıcı kapalı mali yılın dönemi açılamaz."));

        if (period.Status == PeriodStatus.Open)
            return Result<bool>.Success(true); // Zaten açık, idempotent

        if (period.Status == PeriodStatus.ClosedPermanent)
            return Result<bool>.Failure(
                Error.Failure("ACC-217", "Kalıcı kapalı dönem açılamaz."));

        period.Status = PeriodStatus.Open;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
