using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Units;

/// <summary>
/// <see cref="UpdateUnitCommand"/> handler.
/// </summary>
public sealed class UpdateUnitCommandHandler : IRequestHandler<UpdateUnitCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateUnitCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateUnitCommand command, CancellationToken cancellationToken)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == command.UnitId, cancellationToken);
        if (unit is null)
            return Result.Failure(Error.NotFound("UNIT-NOT-FOUND", "Bağımsız bölüm bulunamadı."));

        unit.Number = command.Number;
        unit.NationalAddressCode = command.NationalAddressCode;
        unit.Type = command.Type;
        unit.SquareMeters = command.SquareMeters;
        unit.LandShare = command.LandShare;
        unit.AllocatedArea = command.AllocatedArea;
        unit.Floor = command.Floor;
        unit.Orientation = command.Orientation;
        unit.Layout = command.Layout;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
