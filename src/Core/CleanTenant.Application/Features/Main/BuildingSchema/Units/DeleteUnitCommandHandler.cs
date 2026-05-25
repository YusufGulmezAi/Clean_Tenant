using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.BuildingSchema.Common;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Units;

/// <summary>
/// <see cref="DeleteUnitCommand"/> handler. Bağımsız bölüm sistemde (tahakkuk,
/// malik/kiracı, katılım vb.) kullanılmıyorsa soft-delete eder; kullanılıyorsa engeller.
/// </summary>
public sealed class DeleteUnitCommandHandler : IRequestHandler<DeleteUnitCommand, Result>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;
    private readonly IUnitUsageChecker _usage;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteUnitCommandHandler(IMainDbContext db, IClock clock, IUnitUsageChecker usage)
    {
        _db = db;
        _clock = clock;
        _usage = usage;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteUnitCommand command, CancellationToken cancellationToken)
    {
        var unit = await _db.Units
            .FirstOrDefaultAsync(u => u.Id == command.UnitId && !u.IsDeleted, cancellationToken);
        if (unit is null)
            return Result.Failure(Error.NotFound("UNIT-NOT-FOUND", "Bağımsız bölüm bulunamadı."));

        var guard = await _usage.EnsureUnitsDeletableAsync(new[] { unit.Id }, cancellationToken);
        if (guard.IsFailure) return guard;

        unit.SoftDelete(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
