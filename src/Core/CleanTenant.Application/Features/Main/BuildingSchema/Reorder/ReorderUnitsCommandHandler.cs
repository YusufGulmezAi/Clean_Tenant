using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Reorder;

/// <summary>
/// <see cref="ReorderUnitsCommand"/> handler.
/// </summary>
public sealed class ReorderUnitsCommandHandler : IRequestHandler<ReorderUnitsCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ReorderUnitsCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(ReorderUnitsCommand command, CancellationToken cancellationToken)
    {
        var units = await _db.Units
            .Where(u => u.BuildingId == command.BuildingId && command.OrderedIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        for (var i = 0; i < command.OrderedIds.Count; i++)
        {
            var unit = units.FirstOrDefault(u => u.Id == command.OrderedIds[i]);
            if (unit is not null)
                unit.SortOrder = i + 1;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
