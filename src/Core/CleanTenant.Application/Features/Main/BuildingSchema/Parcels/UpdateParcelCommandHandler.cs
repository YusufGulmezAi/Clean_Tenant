using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Parcels;

/// <summary>
/// <see cref="UpdateParcelCommand"/> handler.
/// </summary>
public sealed class UpdateParcelCommandHandler : IRequestHandler<UpdateParcelCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateParcelCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateParcelCommand command, CancellationToken cancellationToken)
    {
        var parcel = await _db.Parcels.FirstOrDefaultAsync(p => p.Id == command.ParcelId, cancellationToken);
        if (parcel is null)
            return Result.Failure(Error.NotFound("PARCEL-NOT-FOUND", "Parsel bulunamadı."));

        parcel.Name = command.Name;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
