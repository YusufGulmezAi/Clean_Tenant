using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.BuildingSchema.Common;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Parcels;

/// <summary>
/// <see cref="DeleteParcelCommand"/> handler. Parsel altındaki Bağımsız Bölümlerin
/// hiçbiri sistemde kullanılmıyorsa parsel + yapıları + blokları + tüm BB'leri kademeli
/// soft-delete eder; herhangi biri kullanılıyorsa engeller.
/// </summary>
public sealed class DeleteParcelCommandHandler : IRequestHandler<DeleteParcelCommand, Result>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;
    private readonly IUnitUsageChecker _usage;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteParcelCommandHandler(IMainDbContext db, IClock clock, IUnitUsageChecker usage)
    {
        _db = db;
        _clock = clock;
        _usage = usage;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteParcelCommand command, CancellationToken cancellationToken)
    {
        var parcel = await _db.Parcels
            .FirstOrDefaultAsync(p => p.Id == command.ParcelId && !p.IsDeleted, cancellationToken);
        if (parcel is null)
            return Result.Failure(Error.NotFound("PARCEL-NOT-FOUND", "Parsel bulunamadı."));

        var buildings = await _db.Buildings
            .Where(b => b.ParcelId == command.ParcelId && !b.IsDeleted)
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
        parcel.SoftDelete(now);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
