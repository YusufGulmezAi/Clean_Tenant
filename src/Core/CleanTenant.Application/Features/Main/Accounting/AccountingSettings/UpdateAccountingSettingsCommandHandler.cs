using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.AccountingSettings;

/// <summary>
/// <see cref="UpdateAccountingSettingsCommand"/> handler.
/// </summary>
public sealed class UpdateAccountingSettingsCommandHandler
    : IRequestHandler<UpdateAccountingSettingsCommand, Result<AccountingSettingsDto>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateAccountingSettingsCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<AccountingSettingsDto>> Handle(
        UpdateAccountingSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var settings = await _db.AccountingSettings
            .FirstOrDefaultAsync(s => s.CompanyId == command.CompanyId && !s.IsDeleted, cancellationToken);

        if (settings is null)
            return Result<AccountingSettingsDto>.Failure(
                Error.NotFound("ACC-005", "Muhasebe ayarları bulunamadı."));

        settings.RequireApproval = command.RequireApproval;
        settings.DefaultCurrency = command.DefaultCurrency;
        settings.VatPeriod = command.VatPeriod;
        settings.EDefterEnabled = command.EDefterEnabled;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<AccountingSettingsDto>.Success(new AccountingSettingsDto(
            settings.Id,
            settings.CompanyId,
            settings.IsActivated,
            settings.RequireApproval,
            settings.DefaultCurrency,
            settings.VatPeriod,
            settings.EDefterEnabled));
    }
}
