using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Reorder;

/// <summary>
/// <see cref="ReorderParcelsCommand"/> handler.
/// </summary>
public sealed class ReorderParcelsCommandHandler : IRequestHandler<ReorderParcelsCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ReorderParcelsCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(ReorderParcelsCommand command, CancellationToken cancellationToken)
    {
        var parcels = await _db.Parcels
            .Where(p => p.BlockId == command.BlockId && command.OrderedIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        for (var i = 0; i < command.OrderedIds.Count; i++)
        {
            var parcel = parcels.FirstOrDefault(p => p.Id == command.OrderedIds[i]);
            if (parcel is not null)
                parcel.SortOrder = i + 1;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
