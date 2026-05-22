using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.CostCenters;

/// <summary>
/// <see cref="DeactivateCostCenterCommand"/> handler.
/// </summary>
public sealed class DeactivateCostCenterCommandHandler
    : IRequestHandler<DeactivateCostCenterCommand, Result<bool>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeactivateCostCenterCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        DeactivateCostCenterCommand command,
        CancellationToken cancellationToken)
    {
        var cc = await _db.CostCenters
            .FirstOrDefaultAsync(x => x.Id == command.CostCenterId
                                   && x.CompanyId == command.CompanyId
                                   && !x.IsDeleted, cancellationToken);

        if (cc is null)
            return Result<bool>.Failure(
                Error.NotFound("ACC-002", "Maliyet merkezi bulunamadı."));

        cc.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
