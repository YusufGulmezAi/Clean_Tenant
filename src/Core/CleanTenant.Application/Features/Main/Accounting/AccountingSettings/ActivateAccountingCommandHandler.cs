using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.Accounting.AccountCodes;
using CleanTenant.Application.Features.Main.Accounting.FiscalYears;
using CleanTenant.Domain.Tenant.Accounting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.AccountingSettings;

/// <summary>
/// <see cref="ActivateAccountingCommand"/> handler.
/// <para>
/// AccountingSettings oluşturur (yoksa), hesap planını başlatır ve
/// cari takvim yılı için varsayılan FiscalYear açar.
/// </para>
/// </summary>
public sealed class ActivateAccountingCommandHandler
    : IRequestHandler<ActivateAccountingCommand, Result<AccountingSettingsDto>>
{
    private readonly IMainDbContext _db;
    private readonly IMediator _mediator;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ActivateAccountingCommandHandler(
        IMainDbContext db,
        IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    /// <inheritdoc />
    public async Task<Result<AccountingSettingsDto>> Handle(
        ActivateAccountingCommand command,
        CancellationToken cancellationToken)
    {
        // 1. AccountingSettings var mı kontrol et — yoksa oluştur
        var settings = await _db.AccountingSettings
            .FirstOrDefaultAsync(s => s.CompanyId == command.CompanyId && !s.IsDeleted, cancellationToken);

        if (settings is null)
        {
            settings = new Domain.Tenant.Accounting.AccountingSettings
            {
                TenantId = command.TenantId,
                CompanyId = command.CompanyId,
                IsActivated = false,
                RequireApproval = false,
                DefaultCurrency = "TRY",
                VatPeriod = Domain.Tenant.Accounting.Enums.VatPeriod.Monthly,
                EDefterEnabled = false
            };
            _db.AccountingSettings.Add(settings);
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (settings.IsActivated)
            return Result<AccountingSettingsDto>.Success(new AccountingSettingsDto(
                settings.Id,
                settings.CompanyId,
                settings.IsActivated,
                settings.RequireApproval,
                settings.DefaultCurrency,
                settings.VatPeriod,
                settings.EDefterEnabled));

        // 2. TDHP hesap planını başlat
        var initResult = await _mediator.Send(
            new InitializeChartOfAccountsCommand(command.CompanyId, command.TenantId),
            cancellationToken);

        if (initResult.IsFailure)
            return Result<AccountingSettingsDto>.Failure(initResult.FirstError);

        // 3. Varsayılan FiscalYear oluştur (cari takvim yılı)
        var currentYear = DateTime.Today.Year;
        var fiscalYearStartDate = new DateOnly(currentYear, 1, 1);
        var fiscalYearEndDate = new DateOnly(currentYear, 12, 31);

        var hasFiscalYear = await _db.FiscalYears
            .AnyAsync(fy => fy.CompanyId == command.CompanyId && !fy.IsDeleted, cancellationToken);

        if (!hasFiscalYear)
        {
            var fiscalYearResult = await _mediator.Send(
                new CreateFiscalYearCommand(
                    command.CompanyId,
                    command.TenantId,
                    $"{currentYear}",
                    fiscalYearStartDate,
                    fiscalYearEndDate,
                    SetAsCurrent: true),
                cancellationToken);



            if (fiscalYearResult.IsFailure)
                return Result<AccountingSettingsDto>.Failure(fiscalYearResult.FirstError);
        }

        // Güncel ayarları yeniden oku (InitializeChartOfAccountsCommand IsActivated=true yaptı)
        await _db.SaveChangesAsync(cancellationToken);

        var updatedSettings = await _db.AccountingSettings
            .FirstOrDefaultAsync(s => s.CompanyId == command.CompanyId && !s.IsDeleted, cancellationToken);

        return Result<AccountingSettingsDto>.Success(new AccountingSettingsDto(
            updatedSettings!.Id,
            updatedSettings.CompanyId,
            updatedSettings.IsActivated,
            updatedSettings.RequireApproval,
            updatedSettings.DefaultCurrency,
            updatedSettings.VatPeriod,
            updatedSettings.EDefterEnabled));
    }
}
