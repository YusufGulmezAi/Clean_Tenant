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

        // Blok verildiyse: var olmalı ve BB'nin binasına ait olmalı. Null → bina-altı (blok yok).
        if (command.BlockId.HasValue)
        {
            var blockMatchesBuilding = await _db.Blocks
                .AnyAsync(b => b.Id == command.BlockId.Value
                            && b.BuildingId == unit.BuildingId, cancellationToken);
            if (!blockMatchesBuilding)
                return Result.Failure(Error.Validation(
                    "BLOCK-BUILDING-MISMATCH", "Seçilen blok bu binaya ait değil."));
        }

        // Numara benzersizliği (kendisi hariç; hedef kapsam = yeni BlockId / bina-altı).
        var numberTaken = command.BlockId.HasValue
            ? await _db.Units.AnyAsync(u => u.Id != command.UnitId && u.BlockId == command.BlockId.Value
                                         && u.Number == command.Number && !u.IsDeleted, cancellationToken)
            : await _db.Units.AnyAsync(u => u.Id != command.UnitId && u.BuildingId == unit.BuildingId && u.BlockId == null
                                         && u.Number == command.Number && !u.IsDeleted, cancellationToken);
        if (numberTaken)
            return Result.Failure(Error.Conflict(
                "UNIT-NUMBER-DUPLICATE", $"\"{command.Number}\" numarası bu kapsamda zaten kullanılıyor."));

        unit.BlockId = command.BlockId;
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
