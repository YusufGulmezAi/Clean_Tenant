using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// <see cref="InitializeChartOfAccountsCommand"/> handler.
/// <para>
/// TDHP şablonunu Catalog DB'den toplu okuyarak şirkete ait AccountCode
/// kayıtlarını oluşturur ve AccountingSettings.IsActivated = true yapar.
/// Tek bir SaveChangesAsync çağrısıyla atomik persist.
/// </para>
/// </summary>
public sealed class InitializeChartOfAccountsCommandHandler
    : IRequestHandler<InitializeChartOfAccountsCommand, Result<int>>
{
    private readonly IMainDbContext _db;
    private readonly ICatalogDbContext _catalog;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public InitializeChartOfAccountsCommandHandler(
        IMainDbContext db,
        ICatalogDbContext catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<Result<int>> Handle(
        InitializeChartOfAccountsCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Muhasebe ayarlarını getir — zaten aktifse idempotent hata
        var settings = await _db.AccountingSettings
            .FirstOrDefaultAsync(s => s.CompanyId == command.CompanyId && !s.IsDeleted, cancellationToken);

        if (settings is null)
            return Result<int>.Failure(
                Error.NotFound("ACC-005", "Muhasebe ayarları bulunamadı."));

        if (settings.IsActivated)
            return Result<int>.Failure(
                Error.Failure("ACC-206", "Hesap planı zaten başlatılmış."));

        // 2. Şablonu Catalog DB'den toplu çek
        var templates = await _catalog.ChartOfAccountsTemplates
            .AsNoTracking()
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken);

        if (templates.Count == 0)
            return Result<int>.Failure(
                Error.Failure("ACC-207", "TDHP şablonu bulunamadı."));

        // 3. Şablondan AccountCode kayıtları üret
        var accountCodes = templates.Select(t => new AccountCode
        {
            TenantId = command.TenantId,
            CompanyId = command.CompanyId,
            Code = t.Code,
            ParentCode = t.ParentCode,
            Name = t.Name,
            Level = t.Level,
            AccountClass = t.AccountClass,
            AccountType = t.AccountType,
            IsActive = true,
            IsDetail = t.IsDetail,
            IsMonetary = t.IsMonetary,
            IsRequired = t.IsRequired,
            Source = AccountCodeSource.Standard,
            TemplateCode = t.Code
        }).ToList();

        // 4. Toplu ekle
        _db.AccountCodes.AddRange(accountCodes);

        // 5. Muhasebe ayarlarını aktifleştir
        settings.IsActivated = true;

        // 6. Tek SaveChangesAsync
        await _db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(accountCodes.Count);
    }
}
