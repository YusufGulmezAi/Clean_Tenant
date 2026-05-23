using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Blocks;

/// <summary>
/// <see cref="DeleteBlockCommand"/> handler.
/// </summary>
public sealed class DeleteBlockCommandHandler : IRequestHandler<DeleteBlockCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteBlockCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteBlockCommand command, CancellationToken cancellationToken)
    {
        var block = await _db.Blocks
            .FirstOrDefaultAsync(b => b.Id == command.BlockId && !b.IsDeleted, cancellationToken);

        if (block is null)
            return Result.Failure(Error.NotFound("BLOCK-NOT-FOUND", "Blok bulunamadı."));

        var hasUnits = await _db.Units
            .AnyAsync(u => u.BlockId == command.BlockId && !u.IsDeleted, cancellationToken);

        if (hasUnits)
            return Result.Failure(Error.Failure("BLOCK-HAS-UNITS", "Bağımsız bölüm içeren blok silinemez."));

        block.IsDeleted = true;
        block.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
