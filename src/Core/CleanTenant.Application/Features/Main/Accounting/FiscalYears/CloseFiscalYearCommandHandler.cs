using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.FiscalYears;

/// <summary>
/// <see cref="CloseFiscalYearCommand"/> handler.
/// <para>
/// Tüm alt dönemler Locked olmadan mali yıl kapatılamaz (ACC-204).
/// Kapatılan yılın IsCurrentYear bayrağı kaldırılır; bir sonraki
/// açık yıl varsa o aktif edilir.
/// </para>
/// </summary>
public sealed class CloseFiscalYearCommandHandler
    : IRequestHandler<CloseFiscalYearCommand, Result<bool>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CloseFiscalYearCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        CloseFiscalYearCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Mali yılı getir
        var fiscalYear = await _db.FiscalYears
            .Include(fy => fy.Periods.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(fy => fy.Id == command.FiscalYearId
                                    && fy.CompanyId == command.CompanyId
                                    && !fy.IsDeleted, cancellationToken);

        if (fiscalYear is null)
            return Result<bool>.Failure(
                Error.NotFound("ACC-003", "Mali yıl bulunamadı."));

        if (fiscalYear.Status == PeriodStatus.ClosedPermanent)
            return Result<bool>.Failure(
                Error.Failure("ACC-212", "Mali yıl zaten kalıcı olarak kapatılmış."));

        // 2. Tüm dönemler Locked olmalı (ACC-204)
        var hasUnlockedPeriods = fiscalYear.Periods
            .Any(p => p.Status != PeriodStatus.Locked);

        if (hasUnlockedPeriods)
            return Result<bool>.Failure(
                Error.Failure("ACC-204", "Mali yılı kapatmak için tüm dönemler kilitlenmelidir."));

        // 3. Mali yılı kalıcı kapat
        fiscalYear.Status = PeriodStatus.ClosedPermanent;
        fiscalYear.IsCurrentYear = false;

        // 4. Bir sonraki açık mali yılı cari yıl yap (varsa)
        var nextFiscalYear = await _db.FiscalYears
            .Where(fy => fy.CompanyId == command.CompanyId
                      && !fy.IsDeleted
                      && fy.Status == PeriodStatus.Open
                      && fy.Id != command.FiscalYearId)
            .OrderBy(fy => fy.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (nextFiscalYear is not null)
            nextFiscalYear.IsCurrentYear = true;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
