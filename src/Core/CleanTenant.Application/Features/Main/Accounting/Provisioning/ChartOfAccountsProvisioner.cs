using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AccountingSettingsEntity = CleanTenant.Domain.Tenant.Accounting.AccountingSettings;
using AccountCodeEntity = CleanTenant.Domain.Tenant.Accounting.AccountCode;

namespace CleanTenant.Application.Features.Main.Accounting.Provisioning;

/// <summary>
/// Bir şirkete (Company) standart TDHP hesap planını <b>idempotent</b> olarak
/// ekleyen servis. Tek kaynak (single source of truth): hem manuel
/// "Hesap planını başlat" (<c>InitializeChartOfAccounts</c>) hem de şirketin ilk
/// Mali Dönemi oluşturulurken (<c>CreateFiscalYear</c>) bu servis çağrılır.
/// </summary>
public interface IChartOfAccountsProvisioner
{
    /// <summary>
    /// Şirkette hesap planı (AccountCode) yoksa: <see cref="AccountingSettingsEntity"/>'i
    /// hazırlar (yoksa oluşturur, <c>IsActivated=true</c>) ve TDHP şablonunu (Catalog DB)
    /// AccountCode kayıtlarına kopyalar. Hesap planı zaten varsa hiçbir şey yapmaz.
    /// </summary>
    /// <returns>Eklenen hesap kodu sayısı; <c>0</c> ise plan zaten mevcuttu (no-op).</returns>
    Task<int> EnsureStandardChartAsync(Guid companyId, Guid tenantId, CancellationToken cancellationToken);
}

/// <inheritdoc cref="IChartOfAccountsProvisioner" />
public sealed class ChartOfAccountsProvisioner : IChartOfAccountsProvisioner
{
    private readonly IMainDbContext _db;
    private readonly ICatalogDbContext _catalog;
    private readonly ILogger<ChartOfAccountsProvisioner> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ChartOfAccountsProvisioner(
        IMainDbContext db,
        ICatalogDbContext catalog,
        ILogger<ChartOfAccountsProvisioner> logger)
    {
        _db = db;
        _catalog = catalog;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> EnsureStandardChartAsync(
        Guid companyId, Guid tenantId, CancellationToken cancellationToken)
    {
        // İdempotanlık — hesap planı zaten varsa dokunma (tekrar mali dönem
        // açıldığında ya da manuel "başlat" çağrıldığında çift kayıt önlenir).
        var hasCodes = await _db.AccountCodes
            .AnyAsync(a => a.CompanyId == companyId && !a.IsDeleted, cancellationToken);
        if (hasCodes)
        {
            return 0;
        }

        // TDHP şablonunu Catalog DB'den toplu çek.
        var templates = await _catalog.ChartOfAccountsTemplates
            .AsNoTracking()
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken);
        if (templates.Count == 0)
        {
            _logger.LogWarning(
                "TDHP şablonu boş; şirket {CompanyId} için hesap planı otomatik eklenemedi.", companyId);
            return 0;
        }

        // Muhasebe ayarlarını hazırla (yoksa oluştur, varsa aktifleştir).
        var settings = await _db.AccountingSettings
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && !s.IsDeleted, cancellationToken);
        if (settings is null)
        {
            settings = new AccountingSettingsEntity
            {
                TenantId = tenantId,
                CompanyId = companyId,
                IsActivated = true,
                RequireApproval = false,
                DefaultCurrency = "TRY",
                VatPeriod = VatPeriod.Monthly,
                EDefterEnabled = false,
            };
            _db.AccountingSettings.Add(settings);
        }
        else
        {
            settings.IsActivated = true;
        }

        // Şablondan AccountCode kayıtları üret.
        var accountCodes = templates.Select(t => new AccountCodeEntity
        {
            TenantId = tenantId,
            CompanyId = companyId,
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
            TemplateCode = t.Code,
        }).ToList();

        _db.AccountCodes.AddRange(accountCodes);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Şirket {CompanyId} için TDHP hesap planı otomatik eklendi: {Count} kod.",
            companyId, accountCodes.Count);
        return accountCodes.Count;
    }
}
