using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Parcels;

/// <summary>
/// <see cref="DeleteParcelCommand"/> handler.
/// </summary>
public sealed class DeleteParcelCommandHandler : IRequestHandler<DeleteParcelCommand, Result>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteParcelCommandHandler(IMainDbContext db, IClock clock) { _db = db; _clock = clock; }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteParcelCommand command, CancellationToken cancellationToken)
    {
        var parcel = await _db.Parcels.FirstOrDefaultAsync(p => p.Id == command.ParcelId, cancellationToken);
        if (parcel is null)
            return Result.Failure(Error.NotFound("PARCEL-NOT-FOUND", "Parsel bulunamadı."));

        var hasBuildings = await _db.Buildings.AnyAsync(b => b.ParcelId == command.ParcelId, cancellationToken);
        if (hasBuildings)
            return Result.Failure(Error.Conflict(
                "PARCEL-HAS-BUILDINGS",
                "Parsel altında yapı bulunuyor. Önce yapıları silin."));

        parcel.IsDeleted = true;
        parcel.DeletedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
