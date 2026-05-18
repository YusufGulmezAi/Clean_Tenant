using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Reorder;

/// <summary>
/// <see cref="ReorderBuildingsCommand"/> handler.
/// </summary>
public sealed class ReorderBuildingsCommandHandler : IRequestHandler<ReorderBuildingsCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ReorderBuildingsCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(ReorderBuildingsCommand command, CancellationToken cancellationToken)
    {
        var buildings = await _db.Buildings
            .Where(b => b.ParcelId == command.ParcelId && command.OrderedIds.Contains(b.Id))
            .ToListAsync(cancellationToken);

        for (var i = 0; i < command.OrderedIds.Count; i++)
        {
            var building = buildings.FirstOrDefault(b => b.Id == command.OrderedIds[i]);
            if (building is not null)
                building.SortOrder = i + 1;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
