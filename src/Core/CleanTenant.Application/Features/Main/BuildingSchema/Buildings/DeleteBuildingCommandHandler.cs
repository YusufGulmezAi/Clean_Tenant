using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Buildings;

/// <summary>
/// <see cref="DeleteBuildingCommand"/> handler.
/// </summary>
public sealed class DeleteBuildingCommandHandler : IRequestHandler<DeleteBuildingCommand, Result>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteBuildingCommandHandler(IMainDbContext db, IClock clock) { _db = db; _clock = clock; }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteBuildingCommand command, CancellationToken cancellationToken)
    {
        var building = await _db.Buildings
            .FirstOrDefaultAsync(b => b.Id == command.BuildingId, cancellationToken);
        if (building is null)
            return Result.Failure(Error.NotFound("BUILDING-NOT-FOUND", "Yapı bulunamadı."));

        var hasUnits = await _db.Units.AnyAsync(u => u.BuildingId == command.BuildingId, cancellationToken);
        if (hasUnits)
            return Result.Failure(Error.Conflict(
                "BUILDING-HAS-UNITS",
                "Yapı altında bağımsız bölüm bulunuyor. Önce bağımsız bölümleri silin."));

        building.IsDeleted = true;
        building.DeletedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
