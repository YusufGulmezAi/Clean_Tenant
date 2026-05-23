using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.Accruals;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Persistence.Main.Accruals;

/// <summary>
/// <see cref="IAccountCodeAllocator"/> implementasyonu. Catalog'dan base kodları
/// okur, Main DB'de parent zincirini garanti eder ve bir sonraki alt hesabı üretir.
/// </summary>
internal sealed class AccountCodeAllocator : IAccountCodeAllocator
{
    private readonly IMainDbContext _main;
    private readonly ICatalogDbContext _catalog;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AccountCodeAllocator(IMainDbContext main, ICatalogDbContext catalog)
    {
        _main = main;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<AccountCodePair> AllocateBudgetAccountCodesAsync(
        Guid tenantId, Guid companyId, BudgetType type, string budgetTitle, CancellationToken cancellationToken)
    {
        var meta = await _catalog.BudgetTypeMetadata
            .FirstOrDefaultAsync(m => m.Type == type, cancellationToken)
            ?? throw new InvalidOperationException($"BudgetTypeMetadata bulunamadı: {type}");

        var receivableId = await AllocateOneAsync(
            tenantId, companyId, meta.BaseReceivableCode,
            AccountClass.CurrentAsset, AccountType.Active,
            $"{meta.DisplayName} Alacakları", budgetTitle, cancellationToken);

        var incomeId = await AllocateOneAsync(
            tenantId, companyId, meta.BaseIncomeCode,
            AccountClass.IncomeStatement, AccountType.Passive,
            $"{meta.DisplayName} Gelirleri", budgetTitle, cancellationToken);

        return new AccountCodePair(receivableId, incomeId);
    }

    /// <summary>
    /// Bir base kod (örn. "120.01") altında bir sonraki detay hesabı (120.01.NNN)
    /// üretir. Parent zinciri (120, 120.01) eksikse oluşturur.
    /// </summary>
    private async Task<Guid> AllocateOneAsync(
        Guid tenantId, Guid companyId, string baseCode,
        AccountClass cls, AccountType type, string parentName, string detailName,
        CancellationToken cancellationToken)
    {
        // Parent zincirini garanti et: "120" (Main) + "120.01" (Sub)
        var mainCode = baseCode.Split('.')[0]; // "120"
        await EnsureAccountAsync(tenantId, companyId, mainCode, null, $"{parentName} (Ana)",
            AccountLevel.Main, cls, type, cancellationToken);
        await EnsureAccountAsync(tenantId, companyId, baseCode, mainCode, parentName,
            AccountLevel.Sub, cls, type, cancellationToken);

        // Bir sonraki seq: baseCode altındaki mevcut detay hesaplarından max suffix + 1
        var prefix = baseCode + ".";
        var existingCodes = await _main.AccountCodes
            .Where(a => a.CompanyId == companyId && a.ParentCode == baseCode)
            .Select(a => a.Code)
            .ToListAsync(cancellationToken);

        var maxSeq = 0;
        foreach (var code in existingCodes)
        {
            var suffix = code[prefix.Length..];
            if (int.TryParse(suffix, out var n) && n > maxSeq) maxSeq = n;
        }
        var nextSeq = maxSeq + 1;
        var detailCode = $"{baseCode}.{nextSeq:000}";

        var detail = new AccountCode
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = detailCode,
            ParentCode = baseCode,
            Name = $"{parentName} — {detailName}",
            Level = AccountLevel.Detail,
            AccountClass = cls,
            AccountType = type,
            Source = AccountCodeSource.CompanyDefined,
            IsActive = true,
            IsDetail = true,
            IsMonetary = true,
            IsRequired = false,
        };
        _main.AccountCodes.Add(detail);
        return detail.Id;
    }

    /// <summary>Verilen kod yoksa oluşturur (parent özet hesaplar için).</summary>
    private async Task EnsureAccountAsync(
        Guid tenantId, Guid companyId, string code, string? parentCode, string name,
        AccountLevel level, AccountClass cls, AccountType type, CancellationToken cancellationToken)
    {
        var exists = await _main.AccountCodes
            .AnyAsync(a => a.CompanyId == companyId && a.Code == code, cancellationToken);
        if (exists) return;

        // Bu metoddan aynı transaction'da iki kez çağrılırsa local ChangeTracker'a da bak.
        var tracked = _main.AccountCodes.Local
            .Any(a => a.CompanyId == companyId && a.Code == code);
        if (tracked) return;

        _main.AccountCodes.Add(new AccountCode
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = code,
            ParentCode = parentCode,
            Name = name,
            Level = level,
            AccountClass = cls,
            AccountType = type,
            Source = AccountCodeSource.CompanyDefined,
            IsActive = true,
            IsDetail = false,
            IsMonetary = true,
            IsRequired = false,
        });
    }
}
