using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.BuildingSchema.Common;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Blocks;

/// <summary>
/// <see cref="DeleteBlockCommand"/> handler. Blok altındaki Bağımsız Bölümlerin
/// hiçbiri sistemde kullanılmıyorsa blok + tüm BB'leri kademeli soft-delete eder;
/// herhangi biri kullanılıyorsa engeller.
/// </summary>
public sealed class DeleteBlockCommandHandler : IRequestHandler<DeleteBlockCommand, Result>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;
    private readonly IUnitUsageChecker _usage;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteBlockCommandHandler(IMainDbContext db, IClock clock, IUnitUsageChecker usage)
    {
        _db = db;
        _clock = clock;
        _usage = usage;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteBlockCommand command, CancellationToken cancellationToken)
    {
        var block = await _db.Blocks
            .FirstOrDefaultAsync(b => b.Id == command.BlockId && !b.IsDeleted, cancellationToken);
        if (block is null)
            return Result.Failure(Error.NotFound("BLOCK-NOT-FOUND", "Blok bulunamadı."));

        var units = await _db.Units
            .Where(u => u.BlockId == command.BlockId && !u.IsDeleted)
            .ToListAsync(cancellationToken);

        var guard = await _usage.EnsureUnitsDeletableAsync(
            units.Select(u => u.Id).ToList(), cancellationToken);
        if (guard.IsFailure) return guard;

        var now = _clock.UtcNow;
        foreach (var unit in units) unit.SoftDelete(now);
        block.SoftDelete(now);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
