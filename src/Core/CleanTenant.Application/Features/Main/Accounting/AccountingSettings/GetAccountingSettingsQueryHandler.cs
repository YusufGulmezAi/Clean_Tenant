using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.AccountingSettings;

/// <summary>
/// <see cref="GetAccountingSettingsQuery"/> handler.
/// </summary>
public sealed class GetAccountingSettingsQueryHandler
    : IRequestHandler<GetAccountingSettingsQuery, Result<AccountingSettingsDto>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetAccountingSettingsQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<AccountingSettingsDto>> Handle(
        GetAccountingSettingsQuery query,
        CancellationToken cancellationToken)
    {
        var settings = await _db.AccountingSettings
            .Where(s => s.CompanyId == query.CompanyId && !s.IsDeleted)
            .Select(s => new AccountingSettingsDto(
                s.Id,
                s.CompanyId,
                s.IsActivated,
                s.RequireApproval,
                s.DefaultCurrency,
                s.VatPeriod,
                s.EDefterEnabled))
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
            return Result<AccountingSettingsDto>.Failure(
                Error.NotFound("ACC-005", "Muhasebe ayarları bulunamadı."));

        return Result<AccountingSettingsDto>.Success(settings);
    }
}
