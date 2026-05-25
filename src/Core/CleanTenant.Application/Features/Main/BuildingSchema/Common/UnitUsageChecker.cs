using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Common;

/// <inheritdoc cref="IUnitUsageChecker" />
public sealed class UnitUsageChecker : IUnitUsageChecker
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UnitUsageChecker(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> EnsureUnitsDeletableAsync(
        IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken)
    {
        if (unitIds.Count == 0)
            return Result.Success();

        var usedIds = await GetUsedUnitIdsAsync(unitIds, cancellationToken);
        if (usedIds.Count == 0)
            return Result.Success();

        // Hata mesajı için kullanılan BB'lerin numaralarından bir örnek al.
        var usedArray = usedIds.ToArray();
        var sample = await _db.Units
            .Where(u => usedArray.Contains(u.Id))
            .OrderBy(u => u.SortOrder)
            .Select(u => u.Number)
            .Take(5)
            .ToListAsync(cancellationToken);

        var more = usedIds.Count - sample.Count;
        const string reason = "(tahakkuk, malik/kiracı/iletişim, katılım grubu, muafiyet vb.)";
        var msg = usedIds.Count == 1
            ? $"'{sample.FirstOrDefault()}' numaralı bağımsız bölüm sistemde kullanıldığı için {reason} silinemiyor. Önce ilgili kayıtları kaldırın."
            : $"{usedIds.Count} bağımsız bölüm sistemde kullanıldığı için {reason} silinemiyor: " +
              $"{string.Join(", ", sample)}{(more > 0 ? $" (+{more} daha)" : "")}. Önce ilgili kayıtları kaldırın.";

        return Result.Failure(Error.Conflict("BUILDINGSCHEMA-UNITS-IN-USE", msg));
    }

    // Verilen BB kümesinden, herhangi bir kullanım tablosunda silinmemiş kayıtla
    // geçen BB Id'lerini toplar. Her tablo ayrı sorgu; sonuç birleşik küme.
    private async Task<HashSet<Guid>> GetUsedUnitIdsAsync(
        IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken)
    {
        var ids = unitIds as Guid[] ?? unitIds.ToArray();
        var used = new HashSet<Guid>();

        used.UnionWith(await _db.AccrualDetails
            .Where(x => !x.IsDeleted && ids.Contains(x.UnitId))
            .Select(x => x.UnitId).Distinct().ToListAsync(cancellationToken));
        used.UnionWith(await _db.UnitOwnerships
            .Where(x => !x.IsDeleted && ids.Contains(x.UnitId))
            .Select(x => x.UnitId).Distinct().ToListAsync(cancellationToken));
        used.UnionWith(await _db.UnitTenancies
            .Where(x => !x.IsDeleted && ids.Contains(x.UnitId))
            .Select(x => x.UnitId).Distinct().ToListAsync(cancellationToken));
        used.UnionWith(await _db.UnitContacts
            .Where(x => !x.IsDeleted && ids.Contains(x.UnitId))
            .Select(x => x.UnitId).Distinct().ToListAsync(cancellationToken));
        used.UnionWith(await _db.UnitParticipationGroups
            .Where(x => !x.IsDeleted && ids.Contains(x.UnitId))
            .Select(x => x.UnitId).Distinct().ToListAsync(cancellationToken));
        used.UnionWith(await _db.ExemptionRules
            .Where(x => !x.IsDeleted && ids.Contains(x.UnitId))
            .Select(x => x.UnitId).Distinct().ToListAsync(cancellationToken));

        return used;
    }
}
