using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.CostCenters;

/// <summary>
/// <see cref="UpdateCostCenterCommand"/> handler.
/// </summary>
public sealed class UpdateCostCenterCommandHandler
    : IRequestHandler<UpdateCostCenterCommand, Result<CostCenterListItem>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateCostCenterCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<CostCenterListItem>> Handle(
        UpdateCostCenterCommand command,
        CancellationToken cancellationToken)
    {
        var cc = await _db.CostCenters
            .FirstOrDefaultAsync(x => x.Id == command.CostCenterId
                                   && x.CompanyId == command.CompanyId
                                   && !x.IsDeleted, cancellationToken);

        if (cc is null)
            return Result<CostCenterListItem>.Failure(
                Error.NotFound("ACC-002", "Maliyet merkezi bulunamadı."));

        cc.Name = command.Name;
        cc.Description = command.Description;
        cc.IsActive = command.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<CostCenterListItem>.Success(new CostCenterListItem(
            cc.Id,
            cc.Code,
            cc.Name,
            cc.Description,
            cc.IsActive));
    }
}
