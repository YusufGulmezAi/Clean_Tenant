using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Units;

/// <summary>
/// <see cref="DeleteUnitCommand"/> handler.
/// </summary>
public sealed class DeleteUnitCommandHandler : IRequestHandler<DeleteUnitCommand, Result>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteUnitCommandHandler(IMainDbContext db, IClock clock) { _db = db; _clock = clock; }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteUnitCommand command, CancellationToken cancellationToken)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == command.UnitId, cancellationToken);
        if (unit is null)
            return Result.Failure(Error.NotFound("UNIT-NOT-FOUND", "Bağımsız bölüm bulunamadı."));

        unit.IsDeleted = true;
        unit.DeletedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
