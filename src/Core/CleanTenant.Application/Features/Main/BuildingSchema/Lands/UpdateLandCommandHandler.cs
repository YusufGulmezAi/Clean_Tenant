using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Lands;

/// <summary>
/// <see cref="UpdateLandCommand"/> handler.
/// </summary>
public sealed class UpdateLandCommandHandler : IRequestHandler<UpdateLandCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateLandCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateLandCommand command, CancellationToken cancellationToken)
    {
        var land = await _db.Lands
            .FirstOrDefaultAsync(l => l.Id == command.LandId, cancellationToken);
        if (land is null)
            return Result.Failure(Error.NotFound("LAND-NOT-FOUND", "Ada bulunamadı."));

        land.Name = command.Name;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
