using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Blocks;

/// <summary>
/// <see cref="UpdateBlockCommand"/> handler.
/// </summary>
public sealed class UpdateBlockCommandHandler : IRequestHandler<UpdateBlockCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateBlockCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateBlockCommand command, CancellationToken cancellationToken)
    {
        var block = await _db.Blocks
            .FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);
        if (block is null)
            return Result.Failure(Error.NotFound("BLOCK-NOT-FOUND", "Ada bulunamadı."));

        block.Name = command.Name;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
