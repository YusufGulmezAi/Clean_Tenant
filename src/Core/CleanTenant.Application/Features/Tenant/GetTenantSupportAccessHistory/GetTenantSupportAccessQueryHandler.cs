using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Tenant.GetTenantSupportAccessHistory;

/// <summary>
/// Mevcut Tenant Admin'in tenant'ına yapılan destek oturumlarının listesini döner.
/// Operatör bilgisi (e-posta + ad-soyad) DB join'iyle zenginleştirilir.
/// </summary>
public sealed class GetTenantSupportAccessQueryHandler
{
    private const int MaxPageSize = 100;

    private readonly ICatalogDbContext _db;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetTenantSupportAccessQueryHandler(
        ICatalogDbContext db,
        ICurrentSessionAccessor sessionAccessor)
    {
        _db = db;
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>Sorguyu çalıştırır.</summary>
    public async Task<Result<IReadOnlyList<TenantSupportAccessDto>>> HandleAsync(
        GetTenantSupportAccessQuery query,
        CancellationToken cancellationToken)
    {
        var current = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        if (current.TenantId is null)
        {
            return Result<IReadOnlyList<TenantSupportAccessDto>>.Failure(
                Error.Forbidden("SUP-013", "Aktif tenant scope'u yok."));
        }

        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var page = Math.Max(query.Page, 0);

        var baseQuery = _db.SupportSessions
            .AsNoTracking()
            .Where(s => s.TargetTenantId == current.TenantId.Value);

        if (query.From.HasValue)
            baseQuery = baseQuery.Where(s => s.StartedAt >= query.From.Value);
        if (query.To.HasValue)
            baseQuery = baseQuery.Where(s => s.StartedAt <= query.To.Value);

        var rows = await baseQuery
            .OrderByDescending(s => s.StartedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Join(_db.Users,
                s => s.OperatorUserId,
                u => u.Id,
                (s, u) => new TenantSupportAccessDto(
                    s.UrlCode,
                    u.Email ?? string.Empty,
                    (u.FirstName + " " + u.LastName).Trim(),
                    s.Mode,
                    s.Reason,
                    s.StartedAt,
                    s.EndedAt,
                    s.WriteActionCount))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<TenantSupportAccessDto>>.Success(rows);
    }
}
