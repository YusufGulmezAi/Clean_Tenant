using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.BuildingSchema.Common;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Lands;

/// <summary>
/// <see cref="DeleteLandCommand"/> handler. Ada altındaki Bağımsız Bölümlerin hiçbiri
/// sistemde kullanılmıyorsa ada + parselleri + yapıları + blokları + tüm BB'leri kademeli
/// soft-delete eder; herhangi biri kullanılıyorsa engeller.
/// </summary>
public sealed class DeleteLandCommandHandler : IRequestHandler<DeleteLandCommand, Result>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;
    private readonly IUnitUsageChecker _usage;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteLandCommandHandler(IMainDbContext db, IClock clock, IUnitUsageChecker usage)
    {
        _db = db;
        _clock = clock;
        _usage = usage;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteLandCommand command, CancellationToken cancellationToken)
    {
        var land = await _db.Lands
            .FirstOrDefaultAsync(l => l.Id == command.LandId && !l.IsDeleted, cancellationToken);
        if (land is null)
            return Result.Failure(Error.NotFound("LAND-NOT-FOUND", "Ada bulunamadı."));

        var parcels = await _db.Parcels
            .Where(p => p.LandId == command.LandId && !p.IsDeleted)
            .ToListAsync(cancellationToken);
        var parcelIds = parcels.Select(p => p.Id).ToList();

        var buildings = await _db.Buildings
            .Where(b => parcelIds.Contains(b.ParcelId) && !b.IsDeleted)
            .ToListAsync(cancellationToken);
        var buildingIds = buildings.Select(b => b.Id).ToList();

        var units = await _db.Units
            .Where(u => buildingIds.Contains(u.BuildingId) && !u.IsDeleted)
            .ToListAsync(cancellationToken);

        var guard = await _usage.EnsureUnitsDeletableAsync(
            units.Select(u => u.Id).ToList(), cancellationToken);
        if (guard.IsFailure) return guard;

        var blocks = await _db.Blocks
            .Where(bk => buildingIds.Contains(bk.BuildingId) && !bk.IsDeleted)
            .ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
        foreach (var unit in units) unit.SoftDelete(now);
        foreach (var block in blocks) block.SoftDelete(now);
        foreach (var building in buildings) building.SoftDelete(now);
        foreach (var parcel in parcels) parcel.SoftDelete(now);
        land.SoftDelete(now);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
