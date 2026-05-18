using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TenantEntity = CleanTenant.Domain.Identity.Tenants.Tenant;
using TenantStatus = CleanTenant.Domain.Identity.Tenants.TenantStatus;

namespace CleanTenant.Application.Features.Auth.Tenants;

/// <summary>
/// <see cref="GetAccessibleContextsQuery"/> handler'ı. Tenant'ları Catalog DB'den,
/// her Tenant'ın altındaki Companies'i Main DB'den (System scope için
/// IgnoreQueryFilters ile) çeker. Main DB DI'da register edilmediyse Companies
/// boş döner — handler Catalog-only senaryolarda da çalışır (örn. integration
/// test fixture'ı Main DB taşımayabilir).
/// </summary>
public sealed class GetAccessibleContextsQueryHandler
    : IRequestHandler<GetAccessibleContextsQuery, Result<IReadOnlyList<AccessibleTenantContext>>>
{
    private readonly ICatalogDbContext _catalog;
    private readonly IMainDbContext? _main;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır (IMainDbContext opsiyonel).</summary>
    public GetAccessibleContextsQueryHandler(
        ICatalogDbContext catalog,
        ICurrentSessionAccessor sessionAccessor,
        IServiceProvider serviceProvider)
    {
        _catalog = catalog;
        _sessionAccessor = sessionAccessor;
        _main = serviceProvider.GetService<IMainDbContext>();
    }

    /// <summary>Hiyerarşik context listesini döner.</summary>
    public async Task<Result<IReadOnlyList<AccessibleTenantContext>>> Handle(
        GetAccessibleContextsQuery query,
        CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current;
        if (session is null)
        {
            return Result<IReadOnlyList<AccessibleTenantContext>>.Failure(
                Error.Unauthorized("AUTH-005", "Kimlik bilgisi gerekli."));
        }

        // v0.2.3.c — Support Mode v2 revizyonu:
        // Context Switcher dropdown'u **yalnız kullanıcının gerçek rol atamalarını**
        // gösterir (System için bile). Sistem operatörü "tüm Yönetim/Site listesi"ne
        // sol menü ("Yönetimler" / "Siteler" NavGroup'ları) üzerinden erişir —
        // burada gösterilenler değil. Bu ayrım: gerçek atama = tam yetki vs. support
        // erişimi = ReadOnly/WriteEnabled (mail link onayına bağlı).
        //
        // Yani: System scope da olsa, dropdown listesi her zaman UserRoleAssignments
        // tablosundan filtrelenir. "Sistem" item'ı NavMenu'de + dropdown'da ayrıca
        // gösterilir (kullanıcının System scope rol ataması varsa).
        var assignedTenantIds = _catalog.UserRoleAssignments.AsNoTracking()
            .Where(a => a.UserId == session.UserId && a.IsActive && a.TenantId != null)
            .Select(a => a.TenantId!.Value)
            .Distinct();

        var tenants = await _catalog.Tenants.AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active && assignedTenantIds.Contains(t.Id))
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.UrlCode, t.Name })
            .ToListAsync(cancellationToken);

        if (tenants.Count == 0)
        {
            return Result<IReadOnlyList<AccessibleTenantContext>>.Success([]);
        }

        // Companies: Main DB'de tenant_id ile filtreli. System scope için global
        // query filter ITenantContext.TenantId=null nedeniyle hep boş döner →
        // IgnoreQueryFilters() bypass eder. Alt scope kullanıcı zaten kendi
        // TenantId'sinde olduğu için filter doğru sonucu döner ama listede
        // erişebileceği tüm tenant'lar var, hepsinin company'leri lazım — bu
        // yüzden alt scope için de IgnoreQueryFilters ile çekip TenantId'leri
        // explicit `Contains` ile filtreliyoruz.
        var tenantIds = tenants.Select(t => t.Id).ToHashSet();

        Dictionary<Guid, IReadOnlyList<AccessibleCompany>> companiesByTenant = new();
        if (_main is not null)
        {
            var allCompanies = await _main.Companies
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => tenantIds.Contains(c.TenantId))
                .Select(c => new { c.Id, c.TenantId, c.UrlCode, c.Name })
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            companiesByTenant = allCompanies
                .GroupBy(c => c.TenantId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<AccessibleCompany>)g
                        .Select(c => new AccessibleCompany(c.Id, c.UrlCode, c.Name))
                        .ToList());
        }

        var result = tenants.Select(t => new AccessibleTenantContext(
            t.Id,
            t.UrlCode,
            t.Name,
            companiesByTenant.TryGetValue(t.Id, out var cs) ? cs : []))
            .ToList();

        return Result<IReadOnlyList<AccessibleTenantContext>>.Success(result);
    }
}
