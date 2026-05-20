using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.Audit;

/// <summary>
/// Audit DB'den distinct UserFullName / EntityType / TenantName listelerini
/// alfabetik sıralı döner. Sayfa ilk yüklendiğinde tek seferlik çağrılır;
/// filtre değişiklikleri arası yeniden çağrılmaz (autocomplete'ler sabit).
/// </summary>
public sealed class GetAuditFilterOptionsQueryHandler
    : IRequestHandler<GetAuditFilterOptionsQuery, AuditFilterOptions>
{
    private readonly IAuditDbContext _db;

    public GetAuditFilterOptionsQueryHandler(IAuditDbContext db)
    {
        _db = db;
    }

    public async Task<AuditFilterOptions> Handle(
        GetAuditFilterOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var userFullNames = await _db.AuditEntries
            .AsNoTracking()
            .Where(e => e.UserFullName != null && e.UserFullName != "")
            .Select(e => e.UserFullName!)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);

        var entityTypes = await _db.AuditEntries
            .AsNoTracking()
            .Select(e => e.EntityType)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);

        var tenantNames = await _db.AuditEntries
            .AsNoTracking()
            .Where(e => e.TenantName != null && e.TenantName != "")
            .Select(e => e.TenantName!)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);

        return new AuditFilterOptions(userFullNames, entityTypes, tenantNames);
    }
}
