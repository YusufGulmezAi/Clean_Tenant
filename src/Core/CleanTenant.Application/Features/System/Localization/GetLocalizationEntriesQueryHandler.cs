using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.Localization;

/// <summary>
/// Lokalizasyon kayıtlarını filtre + sayfa numarasına göre getirir. Culture
/// zorunlu; arama key veya value içerisinde contains; opsiyonel
/// "yalnız makine çevirisi" toggle filtresi.
/// </summary>
public sealed class GetLocalizationEntriesQueryHandler
    : IRequestHandler<GetLocalizationEntriesQuery, LocalizationPageResult>
{
    private const int MaxPageSize = 200;
    private const int DefaultPageSize = 50;

    private readonly ICatalogDbContext _db;

    public GetLocalizationEntriesQueryHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<LocalizationPageResult> Handle(
        GetLocalizationEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var f = request.Filter;
        var pageSize = Math.Clamp(f.PageSize > 0 ? f.PageSize : DefaultPageSize, 1, MaxPageSize);
        var page = Math.Max(f.Page, 0);

        var query = _db.LocalizedResources
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.Culture == f.Culture);

        if (!string.IsNullOrWhiteSpace(f.SearchTerm))
        {
            var lower = f.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.Key.ToLowerInvariant().Contains(lower) || r.Value.ToLowerInvariant().Contains(lower));
        }

        if (f.OnlyMachineTranslated)
        {
            query = query.Where(r => r.IsMachineTranslated);
        }

        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderBy(r => r.Key)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(r => new LocalizationEntryListItem(
                r.Id,
                r.Key,
                r.Culture,
                r.Value,
                r.IsMachineTranslated,
                r.UpdatedAt ?? r.CreatedAt,
                r.UpdatedBy ?? r.CreatedBy))
            .ToListAsync(cancellationToken);

        return new LocalizationPageResult(rows, total, page, pageSize);
    }
}
