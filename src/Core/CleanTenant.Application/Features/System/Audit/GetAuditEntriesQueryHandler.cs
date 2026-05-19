using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.Audit;

/// <summary>Audit girişlerini filtreye ve sayfa numarasına göre getirir.</summary>
public sealed class GetAuditEntriesQueryHandler : IRequestHandler<GetAuditEntriesQuery, AuditPageResult>
{
    private const int MaxPageSize = 200;
    private const int DefaultPageSize = 50;

    private readonly IAuditDbContext _db;

    public GetAuditEntriesQueryHandler(IAuditDbContext db)
    {
        _db = db;
    }

    public async Task<AuditPageResult> Handle(GetAuditEntriesQuery request, CancellationToken cancellationToken)
    {
        var f = request.Filter;
        var pageSize = Math.Clamp(f.PageSize > 0 ? f.PageSize : DefaultPageSize, 1, MaxPageSize);
        var page = Math.Max(f.Page, 0);

        var query = _db.AuditEntries.AsNoTracking();

        if (f.DateFrom.HasValue)
            query = query.Where(e => e.Timestamp >= f.DateFrom.Value);
        if (f.DateTo.HasValue)
            query = query.Where(e => e.Timestamp <= f.DateTo.Value);
        if (!string.IsNullOrWhiteSpace(f.UserFullName))
            query = query.Where(e => e.UserFullName != null && e.UserFullName.Contains(f.UserFullName));
        if (!string.IsNullOrWhiteSpace(f.EntityType))
            query = query.Where(e => e.EntityType == f.EntityType);
        if (f.Action.HasValue)
            query = query.Where(e => e.Action == f.Action.Value);
        if (!string.IsNullOrWhiteSpace(f.TenantName))
            query = query.Where(e => e.TenantName != null && e.TenantName.Contains(f.TenantName));
        if (f.CompanyId.HasValue)
            query = query.Where(e => e.CompanyId == f.CompanyId.Value);

        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(e => new AuditListItem(
                e.Id,
                e.Timestamp,
                e.EntityType,
                e.EntityId,
                e.Action,
                e.UserFullName,
                e.UserEmail,
                e.TenantId,
                e.TenantName,
                e.CompanyId,
                e.ChangesJson,
                e.IpAddress,
                e.UserAgent,
                e.BrowserName,
                e.OperatingSystem,
                e.RequestPath,
                e.ScopeLevel))
            .ToListAsync(cancellationToken);

        return new AuditPageResult(rows, total, page, pageSize);
    }
}
