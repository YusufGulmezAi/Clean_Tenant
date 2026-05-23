using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Reorder;

/// <summary>
/// <see cref="ReorderBlocksCommand"/> handler.
/// </summary>
public sealed class ReorderBlocksCommandHandler : IRequestHandler<ReorderBlocksCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ReorderBlocksCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(ReorderBlocksCommand command, CancellationToken cancellationToken)
    {
        var blocks = await _db.Blocks
            .Where(b => b.BuildingId == command.BuildingId
                     && command.OrderedIds.Contains(b.Id)
                     && !b.IsDeleted)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < command.OrderedIds.Count; i++)
        {
            var block = blocks.FirstOrDefault(b => b.Id == command.OrderedIds[i]);
            if (block is not null)
                block.SortOrder = i + 1;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
