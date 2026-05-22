using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Reorder;

/// <summary>
/// <see cref="ReorderLandsCommand"/> handler.
/// </summary>
public sealed class ReorderLandsCommandHandler : IRequestHandler<ReorderLandsCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ReorderLandsCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(ReorderLandsCommand command, CancellationToken cancellationToken)
    {
        var lands = await _db.Lands
            .Where(l => l.CompanyId == command.CompanyId && command.OrderedIds.Contains(l.Id))
            .ToListAsync(cancellationToken);

        for (var i = 0; i < command.OrderedIds.Count; i++)
        {
            var land = lands.FirstOrDefault(l => l.Id == command.OrderedIds[i]);
            if (land is not null)
                land.SortOrder = i + 1;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
