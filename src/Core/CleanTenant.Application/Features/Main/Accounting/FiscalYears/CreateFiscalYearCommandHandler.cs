using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.Accounting.Provisioning;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Application.Features.Main.Accounting.FiscalYears;

/// <summary>
/// <see cref="CreateFiscalYearCommand"/> handler.
/// <para>
/// Çakışan dönem kontrolü yapar (ACC-205). Takvim dışı dönemler desteklenir
/// ancak log uyarısı verilir. 12 aylık dönemler otomatik oluşturulur.
/// </para>
/// <para>
/// <b>Hesap planı otomasyonu:</b> Şirketin ilk Mali Dönemi oluşturulduğunda
/// standart TDHP hesap planı <see cref="IChartOfAccountsProvisioner"/> ile
/// otomatik eklenir (idempotent; sonraki dönemlerde no-op). Best-effort:
/// hesap planı eklenemese de mali dönem oluşturulur.
/// </para>
/// </summary>
public sealed class CreateFiscalYearCommandHandler
    : IRequestHandler<CreateFiscalYearCommand, Result<FiscalYearDetail>>
{
    private readonly IMainDbContext _db;
    private readonly IChartOfAccountsProvisioner _chartProvisioner;
    private readonly ILogger<CreateFiscalYearCommandHandler> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateFiscalYearCommandHandler(
        IMainDbContext db,
        IChartOfAccountsProvisioner chartProvisioner,
        ILogger<CreateFiscalYearCommandHandler> logger)
    {
        _db = db;
        _chartProvisioner = chartProvisioner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<FiscalYearDetail>> Handle(
        CreateFiscalYearCommand command,
        CancellationToken cancellationToken)
    {
        // Çakışan dönem kontrolü (ACC-205)
        var hasOverlap = await _db.FiscalYears
            .AnyAsync(fy => fy.CompanyId == command.CompanyId
                         && !fy.IsDeleted
                         && fy.StartDate < command.EndDate
                         && fy.EndDate > command.StartDate, cancellationToken);

        if (hasOverlap)
            return Result<FiscalYearDetail>.Failure(
                Error.Failure("ACC-205", "Çakışan hesap dönemi mevcut."));

        // Takvim dışı dönem uyarısı (hata değil — sadece log)
        if (command.StartDate.Day != 1 || command.StartDate.Month != 1)
        {
            _logger.LogWarning(
                "Şirket {CompanyId} için takvim dışı mali yıl oluşturuluyor: {Start} - {End}",
                command.CompanyId, command.StartDate, command.EndDate);
        }

        // Mevcut cari yılı güncelle (SetAsCurrent = true ise)
        if (command.SetAsCurrent)
        {
            await _db.FiscalYears
                .Where(fy => fy.CompanyId == command.CompanyId && fy.IsCurrentYear && !fy.IsDeleted)
                .ExecuteUpdateAsync(s => s.SetProperty(fy => fy.IsCurrentYear, false), cancellationToken);
        }

        // Mali yılı oluştur
        var fiscalYear = new FiscalYear
        {
            TenantId = command.TenantId,
            CompanyId = command.CompanyId,
            Label = command.Label,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Status = PeriodStatus.Open,
            IsCurrentYear = command.SetAsCurrent
        };

        // Aylık dönemleri otomatik oluştur
        var periods = new List<AccountingPeriod>();
        var current = command.StartDate;
        while (current <= command.EndDate)
        {
            var periodEnd = new DateOnly(current.Year, current.Month,
                DateTime.DaysInMonth(current.Year, current.Month));

            // Son dönem mali yıl bitiş tarihini aşmasın
            if (periodEnd > command.EndDate)
                periodEnd = command.EndDate;

            periods.Add(new AccountingPeriod
            {
                TenantId = command.TenantId,
                CompanyId = command.CompanyId,
                FiscalYear = fiscalYear,
                Year = current.Year,
                Month = current.Month,
                StartDate = current,
                EndDate = periodEnd,
                Status = PeriodStatus.Open
            });

            // Sonraki ay
            current = periodEnd.AddDays(1);
        }

        fiscalYear.Periods = periods;
        _db.FiscalYears.Add(fiscalYear);
        await _db.SaveChangesAsync(cancellationToken);

        // Şirketin ilk Mali Dönemi açıldığında standart TDHP hesap planını otomatik
        // ekle (idempotent — hesap planı zaten varsa no-op). Best-effort: hesap planı
        // eklenemese bile mali dönem oluşturulmuş kalır.
        try
        {
            var addedCodes = await _chartProvisioner.EnsureStandardChartAsync(
                command.CompanyId, command.TenantId, cancellationToken);
            if (addedCodes > 0)
            {
                _logger.LogInformation(
                    "Mali dönem oluşturma ile şirket {CompanyId} için TDHP hesap planı eklendi ({Count} kod).",
                    command.CompanyId, addedCodes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Şirket {CompanyId} için TDHP hesap planı otomatik eklenemedi; mali dönem yine de oluşturuldu.",
                command.CompanyId);
        }

        var periodSummaries = periods
            .Select(p => new PeriodSummary(
                p.Id, p.Year, p.Month, p.StartDate, p.EndDate, p.Status))
            .ToList()
            .AsReadOnly();

        return Result<FiscalYearDetail>.Success(new FiscalYearDetail(
            fiscalYear.Id,
            fiscalYear.Label,
            fiscalYear.StartDate,
            fiscalYear.EndDate,
            fiscalYear.Status,
            fiscalYear.IsCurrentYear,
            periodSummaries));
    }
}
