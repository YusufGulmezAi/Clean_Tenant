using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Blocks;

/// <summary>
/// <see cref="DeleteBlockCommand"/> handler.
/// </summary>
public sealed class DeleteBlockCommandHandler : IRequestHandler<DeleteBlockCommand, Result>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteBlockCommandHandler(IMainDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteBlockCommand command, CancellationToken cancellationToken)
    {
        var block = await _db.Blocks
            .FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);
        if (block is null)
            return Result.Failure(Error.NotFound("BLOCK-NOT-FOUND", "Ada bulunamadı."));

        var hasParcels = await _db.Parcels
            .AnyAsync(p => p.BlockId == command.BlockId, cancellationToken);
        if (hasParcels)
            return Result.Failure(Error.Conflict(
                "BLOCK-HAS-PARCELS",
                "Ada altında parsel bulunuyor. Önce parselleri silin."));

        block.IsDeleted = true;
        block.DeletedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
