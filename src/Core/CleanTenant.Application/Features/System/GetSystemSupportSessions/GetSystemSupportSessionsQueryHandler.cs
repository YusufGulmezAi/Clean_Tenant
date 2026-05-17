using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.GetSystemSupportSessions;

/// <summary>
/// Tüm tenant'lara ait destek oturumları (System operatörlerin denetimi için).
/// </summary>
public sealed class GetSystemSupportSessionsQueryHandler
{
    private const int MaxPageSize = 100;

    private readonly ICatalogDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetSystemSupportSessionsQueryHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    /// <summary>Sorguyu çalıştırır.</summary>
    public async Task<Result<IReadOnlyList<SystemSupportSessionDto>>> HandleAsync(
        GetSystemSupportSessionsQuery query,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var page = Math.Max(query.Page, 0);

        var baseQuery = _db.SupportSessions.AsNoTracking().AsQueryable();

        if (query.From.HasValue)
            baseQuery = baseQuery.Where(s => s.StartedAt >= query.From.Value);
        if (query.To.HasValue)
            baseQuery = baseQuery.Where(s => s.StartedAt <= query.To.Value);

        if (!string.IsNullOrWhiteSpace(query.OperatorUserUrlCode))
        {
            var op = await _db.Users
                .AsNoTracking()
                .Where(u => u.UrlCode == query.OperatorUserUrlCode)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);
            baseQuery = baseQuery.Where(s => s.OperatorUserId == op);
        }

        if (!string.IsNullOrWhiteSpace(query.TargetTenantUrlCode))
        {
            var tenant = await _db.Tenants
                .AsNoTracking()
                .Where(t => t.UrlCode == query.TargetTenantUrlCode)
                .Select(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);
            baseQuery = baseQuery.Where(s => s.TargetTenantId == tenant);
        }

        var rows = await baseQuery
            .OrderByDescending(s => s.StartedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Join(_db.Users,
                s => s.OperatorUserId,
                u => u.Id,
                (s, u) => new { Session = s, Operator = u })
            .Join(_db.Tenants,
                x => x.Session.TargetTenantId,
                t => t.Id,
                (x, t) => new SystemSupportSessionDto(
                    x.Session.UrlCode,
                    x.Operator.Email ?? string.Empty,
                    t.Name,
                    x.Session.Mode,
                    x.Session.Reason,
                    x.Session.StartedAt,
                    x.Session.EndedAt,
                    x.Session.WriteActionCount))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SystemSupportSessionDto>>.Success(rows);
    }
}
