using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Buildings;

/// <summary>
/// <see cref="UpdateBuildingCommand"/> handler.
/// </summary>
public sealed class UpdateBuildingCommandHandler : IRequestHandler<UpdateBuildingCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateBuildingCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateBuildingCommand command, CancellationToken cancellationToken)
    {
        var building = await _db.Buildings
            .FirstOrDefaultAsync(b => b.Id == command.BuildingId, cancellationToken);
        if (building is null)
            return Result.Failure(Error.NotFound("BUILDING-NOT-FOUND", "Yapı bulunamadı."));

        building.Name = command.Name;
        building.MunicipalNo = command.MunicipalNo;
        building.Type = command.Type;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
