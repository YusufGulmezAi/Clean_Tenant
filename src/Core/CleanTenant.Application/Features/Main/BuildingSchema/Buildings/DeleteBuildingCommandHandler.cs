using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.BuildingSchema.Common;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Buildings;

/// <summary>
/// <see cref="DeleteBuildingCommand"/> handler. Yapı altındaki Bağımsız Bölümlerin
/// hiçbiri sistemde kullanılmıyorsa yapı + blokları + tüm BB'leri kademeli soft-delete
/// eder; herhangi biri kullanılıyorsa engeller.
/// </summary>
public sealed class DeleteBuildingCommandHandler : IRequestHandler<DeleteBuildingCommand, Result>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;
    private readonly IUnitUsageChecker _usage;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteBuildingCommandHandler(IMainDbContext db, IClock clock, IUnitUsageChecker usage)
    {
        _db = db;
        _clock = clock;
        _usage = usage;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteBuildingCommand command, CancellationToken cancellationToken)
    {
        var building = await _db.Buildings
            .FirstOrDefaultAsync(b => b.Id == command.BuildingId && !b.IsDeleted, cancellationToken);
        if (building is null)
            return Result.Failure(Error.NotFound("BUILDING-NOT-FOUND", "Yapı bulunamadı."));

        // Yapı altındaki tüm BB'ler (blok-altı + bina-altı; hepsinde BuildingId dolu).
        var units = await _db.Units
            .Where(u => u.BuildingId == command.BuildingId && !u.IsDeleted)
            .ToListAsync(cancellationToken);

        var guard = await _usage.EnsureUnitsDeletableAsync(
            units.Select(u => u.Id).ToList(), cancellationToken);
        if (guard.IsFailure) return guard;

        var blocks = await _db.Blocks
            .Where(bk => bk.BuildingId == command.BuildingId && !bk.IsDeleted)
            .ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
        foreach (var unit in units) unit.SoftDelete(now);
        foreach (var block in blocks) block.SoftDelete(now);
        building.SoftDelete(now);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
