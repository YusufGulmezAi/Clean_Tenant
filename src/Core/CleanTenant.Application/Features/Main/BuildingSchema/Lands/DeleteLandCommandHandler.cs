using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Lands;

/// <summary>
/// <see cref="DeleteLandCommand"/> handler.
/// </summary>
public sealed class DeleteLandCommandHandler : IRequestHandler<DeleteLandCommand, Result>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteLandCommandHandler(IMainDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteLandCommand command, CancellationToken cancellationToken)
    {
        var land = await _db.Lands
            .FirstOrDefaultAsync(l => l.Id == command.LandId, cancellationToken);
        if (land is null)
            return Result.Failure(Error.NotFound("LAND-NOT-FOUND", "Ada bulunamadı."));

        var hasParcels = await _db.Parcels
            .AnyAsync(p => p.LandId == command.LandId, cancellationToken);
        if (hasParcels)
            return Result.Failure(Error.Conflict(
                "LAND-HAS-PARCELS",
                "Ada altında parsel bulunuyor. Önce parselleri silin."));

        land.IsDeleted = true;
        land.DeletedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
